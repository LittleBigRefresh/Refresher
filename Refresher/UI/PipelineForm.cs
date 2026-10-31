using System.Diagnostics;
using System.Text;
using Eto.Drawing;
using Eto.Forms;
using NotEnoughLogs;
using Refresher.Core;
using Refresher.Core.Logging;
using Refresher.Core.Patching;
using Refresher.Core.Patching.Pipelines;
using Refresher.Core.Platform;
using Refresher.Core.Storage;
using Refresher.UI.Items;

namespace Refresher.UI;

public abstract class PipelineForm : RefresherForm, IAccessesPlatform
{
    // TODO replace with PipelineController once we move the hardcoded steps from the base Pipeline class (e.g. AutoDiscover, eboot revert),
    // and once we make PipelineController (and superclasses of it) manage all the pipelines we need per form
    protected abstract Type PipelineType { get; }
    protected Pipeline Pipeline { get; }
    protected PipelineController? Controller { get; }

    protected Button? PatchButton { get; set; }
    protected Button? RevertPatchButton { get; set; }
    protected Button? AutoDiscoverButton => this.ServerUrlInput?.AutoDiscoverButton;
    protected PatchProgressBars ProgressBars { get; set; } = new();
    protected PatchLogDisplay LogDisplay { get; set; } = new();
    
    protected TableLayout FormLayout { get; set; } = new();
    protected virtual Button? ConnectButton => null;
    protected GameListDropdown? GamesDropDown { get; set; }

    // We need references to these so we can easily access their values when filling them into the pipeline,
    // or incase we need to change the value (e.g. AutoDiscover)
    protected ServerUrlInput? ServerUrlInput { get; set; }
    protected JoinKeyInput? JoinKeyInput { get; set; }

    protected AutoApplyInformation? AutoApply { get; set; }
    public new IPlatformInterface Platform { get; set; }
    
    protected bool UsedAutoDiscover { get; set; } = false;
    protected bool IsConnectedToTarget { get; set; } = false;

    public PipelineForm() : this(null)
    {
        
    }
    
    public PipelineForm(AutoApplyInformation? autoApply) : base("Patch", new Size(700, 700), false)
    {
        this.AutoApply = autoApply;
        this.Platform = new EtoPlatformInterface(this);

        this.Pipeline = (Pipeline)Activator.CreateInstance(this.PipelineType)!;
        this.Pipeline.Initialize(this.Platform);

        this.Controller = new(this.Pipeline, Application.Instance.Invoke);
        this.UpdateSubtitle(this.Pipeline.Name);
        
        PreviousInputStorage.Read();

        Thread progressThread = new(() =>
        {
            while (!this.IsDisposed && Application.Instance != null && !Application.Instance.IsDisposed)
            {
                try
                {
                    Application.Instance.Invoke(this.UpdateFormState);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (NullReferenceException)
                {
                    break;
                }
                Thread.Sleep(this.Pipeline?.State == PipelineState.Running ? 10 : 250);
            }
        });
        
        progressThread.Start();
        
        RefresherState.Log += this.OnLog;
        this.UpdateFormState();

        if(this._shouldTriggerOnConnect || (this.AutoApply?.AutomaticallyApply ?? false))
            this.OnConnect(this, EventArgs.Empty);

        if (this.AutoApply?.AutomaticallyDiscover ?? false)
            this.OnAutoDiscoverClick(null, EventArgs.Empty);

        if (this.AutoApply?.AutomaticallyApply ?? false)
        {
            new Thread(() =>
            {
                while (!this.UsedAutoDiscover || !this.IsConnectedToTarget)
                {
                    Thread.Sleep(20);
                }

                Application.Instance.Invoke(() =>
                {
                    this.OnPatchButtonClick(null, EventArgs.Empty);
                });
            }).Start();
        }
    }

    protected virtual void UpdateFormState()
    {
        if (this.Controller == null)
        {
            RefresherState.Logger.LogWarning(LogType.PatchForm, $"Controller is null, not updating form state");
            return;
        }
        
        bool enableControls = this.Controller.EnableControls;

        if (this.ProgressBars != null)
        {
            // adjust progress bars
            this.ProgressBars.PipelineProgressBar.Value = (int)((this.Pipeline?.Progress ?? 0) * 100);
            this.ProgressBars.StepProgressBar.Value = (int)(this.Pipeline?.CurrentProgress * 100 ?? 0);
            this.ProgressBars.PipelineProgressBar.ToolTip = this.Pipeline?.State.ToString() ?? "Uninitialized";

            // highlight progress bars while patching
            this.ProgressBars.StepProgressBar.Enabled = !enableControls;
            this.ProgressBars.PipelineProgressBar.Enabled = !enableControls;
        }

        // disable other things
        this.FormLayout.Enabled = enableControls;

        if(this.AutoDiscoverButton != null)
        {
            this.AutoDiscoverButton.Enabled = enableControls && this.Pipeline?.AutoDiscover == null;
            this.AutoDiscoverButton.Text = this.Controller.AutoDiscoverButtonText;
        }
            
        if(this.RevertPatchButton != null)
        {
            this.RevertPatchButton.Enabled = enableControls;
        }
        
        if(this.PatchButton != null)
        {
            this.PatchButton.Text = this.Controller.MainButtonText;
        }
    }

    protected bool _shouldTriggerOnConnect = false;

    private void AddFormInputsToPipelineInternal()
    {
        if (this.Pipeline == null || this.Pipeline.State == PipelineState.Running)
        {
            RefresherState.Logger.LogWarning(LogType.PatchForm, $"Pipeline is null or currently running, cancelling adding inputs to pipeline");
            return;
        }

        this.Pipeline.Reset();
        this.AddFormInputsToPipeline();
    }

    protected abstract void AddFormInputsToPipeline();
    
    protected virtual void OnConnect(object? sender, EventArgs e)
    {
        if (this.Pipeline == null)
        {
            RefresherState.Logger.LogWarning(LogType.PatchForm, $"Pipeline is null, cancelling platform connection");
            return;
        }
        
        if (this.Pipeline.State == PipelineState.Running)
        {
            RefresherState.Logger.LogWarning(LogType.PatchForm, $"Pipeline is currently running, cancelling platform connection");
            return;
        }
        
        this.AddFormInputsToPipelineInternal();
        
        Task.Run(async () =>
        {
            try
            {
                if (this.GamesDropDown != null)
                {
                    List<GameInformation>? games = await this.Pipeline!.DownloadGameListAsync();
                    if (games != null)
                    {
                        await Application.Instance.InvokeAsync(() =>
                        {
                            this.HandleConnection();
                            this.HandleGameList(games);
                        });
                    }
                }
                else
                {
                    await this.Pipeline.ConnectAsync();
                    await Application.Instance.InvokeAsync(this.HandleConnection);
                }
            }
            // TODO these exceptions should be caught and shown as warning by whatever step is responsible for this
            catch (DirectoryNotFoundException)
            {
                RefresherState.Logger.LogError(LogType.Accessor, "The games folder doesn't exist at that path. Please ensure you entered the right path/IP.");
            }
            catch (Exception ex)
            {
                RefresherState.Logger.LogError(LogType.Pipeline, $"Unhandled error while downloading games list: {ex}");
                SentrySdk.CaptureException(ex);
            }
        });
    }

    protected void HandleConnection()
    {
        this.IsConnectedToTarget = true;
        if (this.ConnectButton == null) return;

        this.ConnectButton.Enabled = false;
        this.ConnectButton.Text = "Connected!";
    }

    protected void HandleGameList(List<GameInformation> games)
    {
        Debug.Assert(this.GamesDropDown != null);

        this.GamesDropDown.Dropdown.Items.Clear();
        
        foreach (GameInformation game in games)
        {
            GameItem item = new()
            {
                Text = $"{game.Name} [{game.TitleId} {game.Version}]",
                Version = game.Version ?? "00.00",
                TitleId = game.TitleId,
                Key = game.TitleId,
            };

            if (GameCacheStorage.IconExistsInCache(game.TitleId))
            {
                using Stream iconStream = GameCacheStorage.GetIconFromCache(game.TitleId);
                try
                {
                    item.Image = new Bitmap(iconStream).WithSize(new Size(64, 64));
                }
                catch (NotSupportedException)
                {
                    // Failed to set image for NPEB01899: System.NotSupportedException: No imaging component suitable to complete this operation was found.
                    // ignore for now
                }
                catch (FormatException)
                {
                    // Failed to set image for BLUS31426: System.IO.FileFormatException: The image format is unrecognized.
                    // also ignore for now

                    // FileFormatException seems to not exist, but this page mentions it extending FormatException:
                    // https://learn.microsoft.com/en-us/dotnet/api/system.io.fileformatexception
                }
                catch(Exception e)
                {
                    RefresherState.Logger.LogWarning(LogType.InfoRetrieval, $"Failed to set image for {game}: {e}");
                }
            }

            this.GamesDropDown.Dropdown.Items.Add(item);
        }

        this.GamesDropDown.Dropdown.Enabled = true;
    }

    protected virtual void OnPatchButtonClick(object? sender, EventArgs e)
    {
        if (this.Pipeline == null || this.ServerUrlInput == null)
        {
            RefresherState.Logger.LogWarning(LogType.PatchForm, $"Pipeline or URL input is null, cancelling patch execution");
            return;
        }
        
        if (this.Pipeline.State == PipelineState.Running)
        {
            RefresherState.Logger.LogWarning(LogType.PatchForm, $"Pipeline is currently running, cancelling patch execution");
            return;
        }
        
        if (this.Pipeline.State is PipelineState.Cancelled or PipelineState.Error or PipelineState.Finished)
        {
            RefresherState.Logger.LogDebug(LogType.PatchForm, $"Resetting pipeline before execution because state is {this.Pipeline.State}");
            this.Pipeline.Reset();
        }
        
        if (!this.UsedAutoDiscover && this.AutoDiscoverButton != null)
        {
            DialogResult result = MessageBox.Show("You haven't used AutoDiscover. Would you like to try to run it now?", "AutoDiscover",
                MessageBoxButtons.YesNoCancel, MessageBoxType.Question);
            
            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (result)
            {
                case DialogResult.Yes:
                    this.OnAutoDiscoverClick(this, EventArgs.Empty);
                    return;
                case DialogResult.No:
                    MessageBox.Show("Okay, AutoDiscover won't be used. If you have issues with this patch, try using it next time.\n" +
                                    "You can also try clicking 'Revert EBOOT' before patching to undo any server-specific patches that may cause issues.\n\n" +
                                    "Click OK to proceed with patching.", "AutoDiscover", MessageBoxType.Warning);
                    break;
                case DialogResult.Cancel:
                    return;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        this.AddFormInputsToPipelineInternal();

        this.Controller?.MainButtonClick();
        this.UpdateFormState();
    }
    
    protected virtual void OnRevertPatchClick(object? sender, EventArgs e)
    {
        if (this.Pipeline == null || this.ServerUrlInput == null)
        {
            RefresherState.Logger.LogWarning(LogType.PatchForm, $"Pipeline or URL input is null, cancelling patch revert");
            return;
        }
        
        if (this.Pipeline.State == PipelineState.Running)
        {
            RefresherState.Logger.LogWarning(LogType.PatchForm, $"Pipeline is currently running, cancelling patch revert");
            return;
        }
        
        this.AddFormInputsToPipelineInternal();
        
        Task.Run(async () =>
        {
            try
            {
                await this.Pipeline!.RevertGameEbootAsync();
            }
            catch (Exception ex)
            {
                RefresherState.Logger.LogError(LogType.Pipeline, $"Unhandled error while reverting the game's EBOOT: {ex}");
            }
        });
    }

    protected void OnAutoDiscoverClick(object? sender, EventArgs e)
    {
        if (this.Pipeline == null || this.ServerUrlInput == null)
        {
            RefresherState.Logger.LogWarning(LogType.PatchForm, $"Pipeline or URL input is null, cancelling AutoDiscover");
            return;
        }
        
        if (this.Pipeline.State == PipelineState.Running)
        {
            RefresherState.Logger.LogWarning(LogType.PatchForm, $"Pipeline is currently running, cancelling AutoDiscover");
            return;
        }
        
        TextControl control = this.ServerUrlInput.Input;

        string url = control.Text;
        this.Controller?.AutoDiscoverButtonClick(url, autoDiscover =>
        {
            this.UsedAutoDiscover = true;

            Debug.Assert(this.AutoDiscoverButton != null);
            this.AutoDiscoverButton.Enabled = false;

            control.Text = autoDiscover.Url;
            control.Enabled = false; // TODO allow to re-enable
        });
    }
    
    protected void OnCopyJoinKeyClick(object? sender, EventArgs e)
    {
        if (this.Pipeline == null || this.JoinKeyInput == null)
        {
            RefresherState.Logger.LogWarning(LogType.PatchForm, $"Pipeline or join key input is null, cancelling join key URL copy");
            return;
        }
        
        if (this.Pipeline.State == PipelineState.Running)
        {
            RefresherState.Logger.LogWarning(LogType.PatchForm, $"Pipeline is currently running, cancelling join key URL copy");
            return;
        }
        
        Debug.Assert(this.Pipeline.ShorthandUrlId != null);
        
        TextControl control = this.JoinKeyInput.Input;
        string key = control.Text;
        
        if((Keyboard.Modifiers & Keys.Shift) != 0 || (Keyboard.Modifiers & Keys.Control) != 0) 
            Clipboard.Instance.Text = $"refresher://join/{this.Pipeline.ShorthandUrlId}?{key}";
        else
            Clipboard.Instance.Text = $"https://go.lbpbonsai.com/join/{this.Pipeline.ShorthandUrlId}?{key}";

        MessageBox.Show("Copied!", "Success");
    }
    
    protected void OnViewGuideClick(object? sender, EventArgs _)
    {
        if (this.Pipeline == null)
        {
            RefresherState.Logger.LogWarning(LogType.PatchForm, $"Pipeline is null, cancelling guide open");
            return;
        }

        if (this.Pipeline.GuideLink == null)
        {
            MessageBox.Show("No guide exists for this patch method yet, so stay tuned!", MessageBoxType.Warning);
            return;
        }

        string url = this.Pipeline.GuideLink;
        this.Platform.OpenUrl(new Uri(url));
    }

    protected void OnCopyLogClick(object? sender, EventArgs e)
    {
        StringBuilder log = new();
        foreach (IListItem item in this.LogDisplay.Display.Items)
        {
            log.AppendLine(item.Text);
        }
        
        Clipboard.Instance.Text = log.ToString();
        MessageBox.Show("The log was successfully copied to the clipboard.", "Success");
    }
    
    protected void OnLog(RefresherLog log)
    {
        Application.Instance.Invoke(() =>
        {
            // somehow getting hit on sentry?
            // id REFRESHER-88
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (this.LogDisplay == null)
                return;
            
            this.LogDisplay.Display.Items.Add($"[{log.Level}] [{log.Category}] {log.Content}");
        
            // automatically scroll to the bottom by highlighting the last item temporarily
            this.LogDisplay.Display.SelectedIndex = this.LogDisplay.Display.Items.Count - 1;
            this.LogDisplay.Display.SelectedIndex = -1;
            
            if (log.Level <= LogLevel.Error && log.Category != nameof(LogType.Platform))
            {
                MessageBox.Show(log.Content, $"{log.Category} {log.Level.ToString()}", MessageBoxType.Error);
            }
        });
    }
}