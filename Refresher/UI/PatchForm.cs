using System.ComponentModel;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text.Json;
using Eto.Drawing;
using Eto.Forms;
using Refresher.Core;
using Refresher.Core.Patching;
using Refresher.Core.Verification;
using Refresher.Core.Verification.Autodiscover;
using Refresher.Core.Patching;
using Sentry;
using Task = System.Threading.Tasks.Task;

namespace Refresher.UI;

public abstract class PatchForm<TPatcher> : RefresherForm where TPatcher : class, IPatcher
{
    protected abstract TableLayout FormPanel { get; }
    
    private readonly Button _patchButton;
    private readonly ListBox _messages;

    protected bool PatchDigest;
    protected TextBox UrlField = null!;
    protected TPatcher? Patcher;
    
    private CancellationToken? _latestToken;
    private CancellationTokenSource? _latestTokenSource;
    private Task? _latestTask;

    private bool _usedAutoDiscover = false;

    protected PatchForm(string subtitle) : base(subtitle, new Size(700, -1), false)
    {
        this._messages = new ListBox { Height = 200 };
        this._patchButton = new Button(this.Patch) { Text = "Patch!", Enabled = false };

        this.Content = new Label { Text = $"This patcher is uninitialized. Call {nameof(this.InitializePatcher)}() at the end of the patcher's constructor.\n" +
                                          $"If you're not a developer, then this is a broken build. Try downloading a newer version." };
    }

    protected void InitializePatcher()
    {
        TableLayout formPanel = this.FormPanel;
        formPanel.Spacing = new Size(5, 5);
        formPanel.Padding = new Padding(0, 0, 0, 10);

        StackLayout layout;
        
        this.Content = new Splitter
        {
            Orientation = Orientation.Vertical,
            Panel1 = formPanel,

            // ReSharper disable once RedundantExplicitParamsArrayCreation
            Panel2 = layout = new StackLayout(new StackLayoutItem[]
            {
                this._messages,
                new Button(this.Guide) { Text = "View guide" },
                new Button(this.InvokeAutoDiscover) { Text = "AutoDiscover" },
                this._patchButton,
            })
            {
                Padding = new Padding(0, 10, 0, 0),
                Spacing = 5,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Bottom,
            },
        };
        
        foreach (Button button in this.AddExtraButtons())
            layout.Items.Add(button);

        this.UrlField.TextChanged += this.Reverify;
        this.UrlField.PlaceholderText = "http://localhost:10061/lbp";
    }

    protected static TableRow AddField<TControl>(string labelText, out TControl control, Button? button = null, int forceHeight = -1) where TControl : Control, new()
    {
        if (!string.IsNullOrWhiteSpace(labelText)) labelText += ':';
        
        Label label = new()
        {
            Text = labelText,
            VerticalAlignment = VerticalAlignment.Center,
        };

        control = new TControl();
        if (forceHeight != -1) control.Height = forceHeight;

        if (button != null)
        {
            DynamicLayout buttonLayout = new();
            buttonLayout.AddRow(button, control);
            buttonLayout.Spacing = new Size(5, 0);
            
            return new TableRow(label, buttonLayout);
        }
        
        return new TableRow(label, control);
    }

    public virtual void CompletePatch(object? sender, EventArgs e)
    {
        // Not necessary for some patchers maybe
    }

    public virtual IEnumerable<Button> AddExtraButtons()
    {
        return Array.Empty<Button>();
    }

    public virtual void Guide(object? sender, EventArgs e)
    {
        MessageBox.Show("No guide exists for this patch method yet, so stay tuned!", MessageBoxType.Warning);
    }

    protected void OpenUrl(string url)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                Process.Start("xdg-open", url);
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                Process.Start("open", url);
            else
                throw new PlatformNotSupportedException("Cannot open a URL on this platform.");
        }
        catch (Exception e)
        {
            State.Logger.LogError(OSIntegration, e.ToString());
            MessageBox.Show("We couldn't open your browser due to an error.\n" +
                            $"You can use this link instead: {url}\n\n" +
                            $"Exception details: {e.GetType().Name} {e.Message}",
                MessageBoxType.Error);
        }
        // based off of https://stackoverflow.com/a/43232486
    }

    private void InvokeAutoDiscover(object? sender, EventArgs arg)
    {
        string url = this.UrlField.Text;
        if(!url.StartsWith("http"))
            url = "https://" + url; // prefer HTTPS by default if there's no scheme set.
        
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? autodiscoverUri))
        {
            State.Logger.LogWarning(AutoDiscover, $"Invalid URL for autodiscover: {url}");
            MessageBox.Show("Server URL could not be parsed correctly. AutoDiscover cannot continue.", "Error", MessageBoxType.Error);
            return;
        }
        
        Debug.Assert(autodiscoverUri != null);
        
        State.Logger.LogInfo(AutoDiscover, $"Invoking autodiscover on URL '{url}'");
        try
        {
            using HttpClient client = new();
            client.BaseAddress = autodiscoverUri;
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Refresher/3");
            
            HttpResponseMessage response = client.GetAsync("/autodiscover").Result;
            response.EnsureSuccessStatusCode();

            AutodiscoverResponse? autodiscover = response.Content.ReadFromJsonAsync<AutodiscoverResponse>().Result;
            if (autodiscover == null) throw new InvalidOperationException("autoresponse was null");
            
            string text = $"Successfully found a '{autodiscover.ServerBrand}' server at the given URL!\n\n" +
                          $"Server's recommended patch URL: {autodiscover.Url}\n" +
                          $"Custom digest key?: {(autodiscover.UsesCustomDigestKey.GetValueOrDefault() ? "Yes" : "No")}\n\n" +
                          $"Use this server's configuration?";
            
            DialogResult result = MessageBox.Show(text, MessageBoxButtons.YesNo);
            if (result != DialogResult.Yes) return;
            
            this.UrlField.Text = autodiscover.Url;
            this.PatchDigest = autodiscover.UsesCustomDigestKey ?? false;
            this._usedAutoDiscover = true;
        }
        catch (AggregateException aggregate)
        {
            aggregate.Handle(HandleAutoDiscoverError);
        }
        catch(Exception e)
        {
            if (!HandleAutoDiscoverError(e))
            {
                SentrySdk.CaptureException(e);
                MessageBox.Show($"AutoDiscover failed: {e}", MessageBoxType.Error);
            }
        }
    }
    
    private static bool HandleAutoDiscoverError(Exception inner)
    {
        if (inner is HttpRequestException httpException)
        {
            if (httpException.StatusCode == null)
            {
                MessageBox.Show($"AutoDiscover failed, because we couldn't communicate with the server: {inner.Message}");
                return true;
            }
            
            MessageBox.Show($"AutoDiscover failed, because the server responded with {(int)httpException.StatusCode} {httpException.StatusCode}.");
            return true;
        }
        
        if (inner is SocketException)
        {
            MessageBox.Show($"AutoDiscover failed, because we couldn't communicate with the server: {inner.Message}");
            return true;
        }

        if (inner is JsonException)
        {
            MessageBox.Show("AutoDiscover failed, because the server sent invalid data. There might be an outage; please try again in a few moments.");
            return true;
        }

        if (inner is NotSupportedException)
        {
            MessageBox.Show($"AutoDiscover failed due to something we couldn't support: {inner.Message}");
            return true;
        }
        
        return false;
    }

    /// <summary>
    /// Called when the state of the patcher should be entirely reset, e.g. when verification has failed.
    /// </summary>
    protected virtual void Reset()
    { }

    /// <summary>
    /// Cancel the current task, and wait for it to complete
    /// </summary>
    protected void CancelAndWaitForTask()
    {
        this._latestTokenSource?.Cancel();
    }

    private void WaitForTask(int timeout = 1000)
    {
        if(this._latestTask is { IsCanceled: false })
            this._latestTask?.Wait(timeout);
    }
    
    private void Patch(object? sender, EventArgs e)
    {
        // Wait for the patch task to finish
        this.WaitForTask();
        
        if (!this._patchButton.Enabled) return; // shouldn't happen ever but just in-case
        if (this.Patcher == null) return;

        this.BeforePatch(sender, e);

        if (!this._usedAutoDiscover)
        {
            DialogResult result = MessageBox.Show("You didn't use AutoDiscover. Would you like to try to run it now?", MessageBoxButtons.YesNoCancel, MessageBoxType.Question);
            
            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (result)
            {
                case DialogResult.Yes:
                    this.InvokeAutoDiscover(this, EventArgs.Empty);
                    break;
                case DialogResult.No:
                    MessageBox.Show("Okay, AutoDiscover won't be used. If you have issues with this patch, try using it next time.\n" +
                                    "You can also try clicking 'Revert EBOOT' before patching to undo any server-specific patches that may cause issues.\n\n" +
                                    "Click OK to proceed with patching.");
                    break;
                case DialogResult.Cancel:
                    return;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        this.Patcher.Patch(this.UrlField.Text, this.PatchDigest);
        
        this.CompletePatch(sender, e);
    }
    
    protected virtual void BeforePatch(object? sender, EventArgs e) {}

    protected void FailVerify(string reason, Exception? e = null, bool clear = true)
    {
        this.FailVerify($"{reason}\n{e}", clear);
    }

    private void FailVerify(string reason, bool clear = true)
    {
        if(clear) this._messages.Items.Clear();
        this._messages.Items.Add(reason);

        State.Logger.LogError(Verify, reason);
        MessageBox.Show(reason, "Verification Failed", MessageBoxType.Error);
        this.Reset();
        
        this.Patcher = null;
        this._patchButton.Enabled = false;
    }

    protected void LogMessage(string message)
    {
        State.Logger.LogInfo(PatchForm, message);
        this._messages.Items.Add(message);
    }

    protected void Reverify(object? sender, EventArgs args) 
    {
        State.Logger.LogDebug(PatchForm, $"Reverify triggered for patcher {this.Patcher?.GetType().Name}");
        if (this.Patcher == null) return;

        // Cancel the current task, and wait for it to complete
        this.CancelAndWaitForTask();
        
        // Disable the patch button
        this._patchButton.Enabled = false;
        this._patchButton.Text = "Verifying...";

        // Create a new token and token source
        this._latestTokenSource = new CancellationTokenSource();
        this._latestToken = this._latestTokenSource.Token;

        // Create a local copy of the URL and flag for patching digest (accessing it *inside* the task will cause the thread to immediately close and would be a race condition)
        string url = this.UrlField.Text;
        bool patchDigest = this.PatchDigest;
        
        // Start a new task to verify the URL
        this._latestTask = Task.Factory.StartNew(delegate 
        {
            try
            {
                this._latestToken.Value.ThrowIfCancellationRequested();

                // Verify the URL
                List<Message> messages = this.Patcher.Verify(url, patchDigest);
                this.ResetAfterPatch(messages);

                this._latestToken.Value.ThrowIfCancellationRequested();
            }
            catch (OperationCanceledException)
            {
                // ignored
            }
            catch (Exception e)
            {
                SentrySdk.CaptureException(e);
                SentrySdk.Flush();
                Program.App.AsyncInvoke(() =>
                {
                    MessageBox.Show("An exception occured while verifying.\n" + e, "Verification Failed");
                });
                List<Message> messages =
                [
                    new Message(MessageLevel.Error, "An exception occured while verifying."),
                ];
                this.ResetAfterPatch(messages);
            }
        }, this._latestToken.Value);
    }
    
    private void ResetAfterPatch(List<Message> messages)
    {
        Program.App.AsyncInvoke(() =>
        {
            this._messages.Items.Clear();
            foreach (Message message in messages) this.LogMessage(message.ToString());
            
            this._patchButton.Enabled = messages.All(m => m.Level != MessageLevel.Error);
            this._patchButton.Text = "Patch!";
        });
    }
    
    protected override void OnClosing(CancelEventArgs e)
    {
        Environment.Exit(0);
        
        base.OnClosing(e);
    }
}