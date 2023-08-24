using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Eto.Drawing;
using Eto.Forms;
using Newtonsoft.Json;
using Refresher.Patching;
using Refresher.Verification;
using Refresher.Verification.Autodiscover;
using Task = System.Threading.Tasks.Task;

namespace Refresher.UI;

public abstract class PatchForm<TPatcher> : RefresherForm where TPatcher : Patcher
{
    protected abstract TableLayout FormPanel { get; }
    
    private readonly Button _patchButton;
    private readonly ListBox _messages;
    
    protected TextBox  UrlField = null!;
    protected CheckBox PatchDigest;
    protected TPatcher? Patcher;
    
    private CancellationToken? _latestToken;
    private CancellationTokenSource? _latestTokenSource;
    private Task? _latestTask;

    public PatchForm(string subtitle) : base(subtitle, new Size(700, -1), false)
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
        
        this.Content = new Splitter
        {
            Orientation = Orientation.Vertical,
            Panel1 = formPanel,

            // ReSharper disable once RedundantExplicitParamsArrayCreation
            Panel2 = new StackLayout(new StackLayoutItem[]
            {
                this._messages,
                new Button(this.Guide) { Text = "View guide" },
                new Button(this.Autodiscover) { Text = "Autodiscover" },
                this._patchButton,
            })
            {
                Padding = new Padding(0, 10, 0, 0),
                Spacing = 5,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
            },
        };
        
        this.UrlField.TextChanged += this.Reverify;
        this.PatchDigest.CheckedChanged += this.Reverify;
        this.UrlField.PlaceholderText = "http://localhost:10061/lbp";
    }

    protected static TableRow AddField<TControl>(string labelText, out TControl control) where TControl : Control, new()
    {
        Label label = new()
        {
            Text = labelText + ':',
            VerticalAlignment = VerticalAlignment.Center,
        };

        return new TableRow(label, control = new TControl());
    }

    public virtual void CompletePatch(object? sender, EventArgs e)
    {
        // Not necessary for some patchers maybe
    }

    public virtual void Guide(object? sender, EventArgs e)
    {
        MessageBox.Show("No guide exists for this patch method yet, so stay tuned!", MessageBoxType.Warning);
    }

    protected void OpenUrl(string url)
    {
        // based off of https://stackoverflow.com/a/43232486
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            Process.Start(url);
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            Process.Start("xdg-open", url);
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            Process.Start("open", url);
        else
            throw new PlatformNotSupportedException("Cannot open a URL on this platform.");
    }

    private void Autodiscover(object? sender, EventArgs arg)
    {
        try
        {
            using HttpClient client = new()
            {
                BaseAddress = new Uri(this.UrlField.Text),
            };

            HttpResponseMessage response = client.GetAsync("/autodiscover").Result;
            response.EnsureSuccessStatusCode();

            Stream stream = response.Content.ReadAsStream();

            JsonSerializer serializer = new();
            using StreamReader streamReader = new(stream);
            using JsonTextReader jsonReader = new(streamReader);

            AutodiscoverResponse? autodiscover = serializer.Deserialize<AutodiscoverResponse>(jsonReader);
            if (autodiscover == null) throw new InvalidOperationException("autoresponse was null");

            string text = $"Successfully found a '{autodiscover.ServerBrand}' server at the given URL!\n\n" +
                          $"Server's recommended patch URL: {autodiscover.Url}\n" +
                          $"Custom digest key?: {(autodiscover.UsesCustomDigestKey.GetValueOrDefault() ? "Yes" : "No")}\n\n" +
                          $"Use this server's configuration?";

            DialogResult result = MessageBox.Show(text, MessageBoxButtons.YesNo);
            if (result != DialogResult.Yes) return;

            this.UrlField.Text       = autodiscover.Url;
            this.PatchDigest.Checked = autodiscover.UsesCustomDigestKey;
        }
        catch (HttpRequestException e)
        {
            if (e.StatusCode == null) throw;
            MessageBox.Show($"Autodiscover failed, because the server responded with {(int)e.StatusCode} {e.StatusCode}.");
        }
        catch(Exception e)
        {
            MessageBox.Show($"Autodiscover failed: {e}", MessageBoxType.Error);
        }
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

        this.Patcher.Patch(this.UrlField.Text, this.PatchDigest.Checked ?? false);
        
        this.CompletePatch(sender, e);
    }

    protected void FailVerify(string reason, Exception? e = null, bool clear = true)
    {
        this.FailVerify($"{reason}\n{e}", clear);
    }

    private void FailVerify(string reason, bool clear = true)
    {
        if(clear) this._messages.Items.Clear();
        this._messages.Items.Add(reason);
        
        this.Reset();
        
        this.Patcher = null;
        this._patchButton.Enabled = false;
    }

    protected void LogMessage(string message)
    {
        this._messages.Items.Add(message);
    }

    protected void Reverify(object? sender, EventArgs e) 
    {
        if (this.Patcher == null) return;

        // Cancel the current task, and wait for it to complete
        this.CancelAndWaitForTask();
        
        // Disable the patch button
        this._patchButton.Enabled = false;
        this._patchButton.Text = "Verifying...";

        // Create a new token and token source
        this._latestTokenSource = new CancellationTokenSource();
        this._latestToken = this._latestTokenSource.Token;

        // Create a local copy of the URL (accessing it *inside* the task will cause the thread to immediately close)
        string url = this.UrlField.Text;
        bool patchDigest = this.PatchDigest.Checked ?? false;
        
        // Start a new task to verify the URL
        this._latestTask = Task.Factory.StartNew(delegate 
        {
            this._latestToken.Value.ThrowIfCancellationRequested();
            
            // Verify the URL
            List<Message> messages = this.Patcher.Verify(url, patchDigest);
            
            this._latestToken.Value.ThrowIfCancellationRequested();
            Program.App.AsyncInvoke(() => 
            {
                this._messages.Items.Clear();
                foreach (Message message in messages) this._messages.Items.Add(message.ToString());
            
                this._patchButton.Enabled = messages.All(m => m.Level != MessageLevel.Error);
                this._patchButton.Text = "Patch!";
            });
        }, this._latestToken.Value);
    }
    
    protected override void OnClosing(CancelEventArgs e)
    {
        Environment.Exit(0);
        
        base.OnClosing(e);
    }
}