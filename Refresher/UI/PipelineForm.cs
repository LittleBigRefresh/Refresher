using System.Diagnostics;
using System.Runtime.InteropServices;
using Eto;
using Eto.Drawing;
using Eto.Forms;
using NotEnoughLogs;
using Refresher.Core;
using Refresher.Core.Accessors;
using Refresher.Core.Extensions;
using Refresher.Core.Logging;
using Refresher.Core.Patching;
using Refresher.Core.Pipelines;
using Refresher.Core.Verification.AutoDiscover;
using Refresher.UI.Items;
using Pipeline = Refresher.Core.Pipelines.Pipeline;

namespace Refresher.UI;

public class PipelineForm<TPipeline> : RefresherForm where TPipeline : Pipeline, new()
{
    private TPipeline? _pipeline;

    private readonly Button _button;
    private readonly Button _revertButton;
    private readonly Button _autoDiscoverButton;
    private readonly ProgressBar _currentProgressBar;
    private readonly ProgressBar _progressBar;
    private readonly ListBox _messages;
    
    private readonly TableLayout _formLayout;
    private Button? _connectButton;
    private DropDown? _gamesDropDown;

    private CancellationTokenSource? _cts;
    private CancellationTokenSource? _autoDiscoverCts;
    
    public PipelineForm() : base(typeof(TPipeline).Name, new Size(700, -1), false)
    {
        this.Content = new Splitter
        {
            Orientation = Orientation.Vertical,
            FixedPanel = SplitterFixedPanel.Panel1,
            Panel1 = this._formLayout = new TableLayout
            {
                Spacing = new Size(5, 5),
                Padding = new Padding(0, 0, 0, 10),
            },
            Panel2 = new StackLayout([
                new StackLayoutItem(this._messages = new ListBox
                {
                    Height = -1,
                }, VerticalAlignment.Top, true),
                this._button = new Button(this.OnButtonClick) { Text = "Patch!" },
                this._revertButton = new Button(this.OnRevertEbootClick) { Text = "Revert EBOOT" },
                this._autoDiscoverButton = new Button(this.OnAutoDiscoverClick) { Text = "AutoDiscover" },
                new Button(this.OnViewGuideClick) { Text = "View Guide" },
                this._currentProgressBar = new ProgressBar(),
                this._progressBar = new ProgressBar(),
            ])
            {
                Padding = new Padding(0, 10, 0, 10),
                Spacing = 5,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch,
                Size = new Size(-1, -1),
            },
        };
        
        this.InitializePipeline();
        this.InitializeFormStateUpdater();
        
        State.Log += this.OnLog;
    }

    private void UpdateFormState()
    {
        // adjust progress bars
        this._progressBar.Value = (int)((this._pipeline?.Progress ?? 0) * 100);
        this._currentProgressBar.Value = (int)(this._pipeline?.CurrentProgress * 100 ?? 0);
        this._progressBar.ToolTip = this._pipeline?.State.ToString() ?? "Uninitialized";

        bool enableControls = this._pipeline?.State != PipelineState.Running;
        
        // highlight progress bars while patching
        this._currentProgressBar.Enabled = !enableControls;
        this._progressBar.Enabled = !enableControls;
        
        // disable other things
        this._formLayout.Enabled = enableControls;
        this._autoDiscoverButton.Enabled = enableControls && this._pipeline?.AutoDiscover == null;
        this._revertButton.Enabled = enableControls;

        // set text of autodiscover button
        if (this._autoDiscoverCts != null)
            this._autoDiscoverButton.Text = "Running AutoDiscover... (click to cancel)";
        else if (this._pipeline?.AutoDiscover == null)
            this._autoDiscoverButton.Text = "AutoDiscover";

        this._button.Text = this._pipeline?.State switch
        {
            PipelineState.NotStarted => "Patch!",
            PipelineState.Running => "Patching... (click to cancel)",
            PipelineState.Finished => "Complete!",
            PipelineState.Cancelled => "Patch!",
            PipelineState.Error => "Retry",
            _ => throw new ArgumentOutOfRangeException(),
        };
    }

    private void InitializePipeline()
    {
        this._cts = new CancellationTokenSource();

        this._pipeline = new TPipeline();
        this._pipeline.Initialize();
        
        this._formLayout.Rows.Clear();
        foreach (StepInput input in this._pipeline.RequiredInputs)
        {
            TableRow row;
            switch (input.Type)
            {
                case StepInputType.Game:
                    row = AddField<DropDown>(input);
                    this._gamesDropDown = row.Cells[1].Control as DropDown;
                    this._gamesDropDown!.Enabled = false;
                    this._gamesDropDown.Height = 56;
                    break;
                case StepInputType.Text:
                    row = AddField<TextBox>(input);
                    break;
                case StepInputType.Directory:
                    row = AddField<FilePicker>(input);
                    if (input.ShouldCauseGameDownloadWhenChanged)
                        (row.Cells[1].Control as FilePicker)!.FilePathChanged += this.OnDownloadGameList;
                    break;
                case StepInputType.ConsoleIp:
                    row = AddField<TextBox>(input, this._connectButton = new Button(this.OnDownloadGameList) { Text = "Connect" });
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            this._formLayout.Rows.Add(row);
        }

        this.UpdateSubtitle(this._pipeline.Name);
        this.UpdateFormState();
    }

    private void AddFormInputsToPipeline()
    {
        foreach (TableRow row in this._formLayout.Rows)
        {
            string id = row.Cells[0].Control.ToolTip;
            Control? valueControl = row.Cells[1].Control;
            if (valueControl is DynamicLayout layout)
                valueControl = ((DynamicControl)layout.Rows.First().Last()).Control;
            
            string value = valueControl.GetUserInput();
            
            this._pipeline!.Inputs.Add(id, value);
        }
    }

    private void InitializeFormStateUpdater()
    {
        Thread progressThread = new(() =>
        {
            while (!this.IsDisposed && !Application.Instance.IsDisposed)
            {
                Application.Instance.Invoke(this.UpdateFormState);
                Thread.Sleep(this._pipeline?.State == PipelineState.Running ? 50 : 250);
            }
        });
        
        progressThread.Start();
    }

    private void OnButtonClick(object? sender, EventArgs e)
    {
        if (this._pipeline == null)
            return;

        if (this._pipeline.State == PipelineState.Running)
        {
            this._cts?.Cancel();
            return;
        }
        
        if (this._pipeline.State is PipelineState.Cancelled or PipelineState.Error or PipelineState.Finished)
        {
            this._pipeline.Reset();
        }

        this.AddFormInputsToPipeline();
        
        Task.Run(async () =>
        {
            try
            {
                await this._pipeline.ExecuteAsync(this._cts?.Token ?? default);
            }
            catch (Exception ex)
            {
                State.Logger.LogError(LogType.Pipeline, $"Error while running pipeline {this._pipeline.Name}: {ex}");
            }
        }, this._cts?.Token ?? default);
        
        this.UpdateFormState();
    }
    
    private void OnDownloadGameList(object? sender, EventArgs e)
    {
        if (this._pipeline == null)
            return;
        
        if (this._pipeline.State == PipelineState.Running)
            return;
        
        this.AddFormInputsToPipeline();
        
        Task.Run(async () =>
        {
            try
            {
                List<GameInformation> games = await this._pipeline!.DownloadGameListAsync();
                await Application.Instance.InvokeAsync(() =>
                {
                    this.HandleGameList(games);
                });
            }
            catch (Exception ex)
            {
                State.Logger.LogError(LogType.Pipeline, $"Error while downloading games list: {ex}");
            }
        });
    }

    private void HandleGameList(List<GameInformation> games)
    {
        if (this._connectButton != null)
        {
            this._connectButton.Enabled = false;
            this._connectButton.Text = "Connected!";
        }
        
        foreach (GameInformation game in games)
        {
            GameItem item = new()
            {
                Text = $"{game.Name} [{game.TitleId} {game.Version}]",
                Version = game.Version ?? "00.00",
                TitleId = game.TitleId,
            };

            if (GameCacheAccessor.IconExistsInCache(game.TitleId))
            {
                using Stream iconStream = GameCacheAccessor.GetIconFromCache(game.TitleId);
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
                    State.Logger.LogWarning(InfoRetrieval, $"Failed to set image for {game}: {e}");
                    SentrySdk.CaptureException(e);
                }
            }

            this._gamesDropDown!.Items.Add(item);
        }

        this._gamesDropDown!.Enabled = true;
    }

    private void OnAutoDiscoverClick(object? sender, EventArgs e)
    {
        if (this._pipeline == null)
            return;
        
        if (this._pipeline.State == PipelineState.Running)
            return;

        if (this._autoDiscoverCts != null)
        {
            this._autoDiscoverCts.Cancel();
            return;
        }

        this._autoDiscoverCts = new CancellationTokenSource();
        
        TextControl control = (TextControl)this._formLayout.Rows
            .Where(r => r.Cells[0].Control.ToolTip == CommonStepInputs.ServerUrl.Id)
            .Select(r => r.Cells[1].Control)
            .First();

        string url = control.Text;

        Task.Run(async () =>
        {
            try
            {
                AutoDiscoverResponse? autoDiscover =
                    await this._pipeline.InvokeAutoDiscoverAsync(url, this._autoDiscoverCts.Token);

                if (autoDiscover != null)
                {
                    await Application.Instance.InvokeAsync(() =>
                    {
                        this._autoDiscoverButton.Enabled = false;
                        this._autoDiscoverButton.Text = $"AutoDiscover [locked to {autoDiscover.ServerBrand}]";

                        control.Text = autoDiscover.Url;
                        control.Enabled = false;
                    });
                }
            }
            catch (Exception ex)
            {
                State.Logger.LogError(LogType.Pipeline, $"Error while invoking autodiscover: {ex}");
                SentrySdk.CaptureException(ex);
            }
            finally
            {
                this._autoDiscoverCts = null;
            }
        }, this._autoDiscoverCts.Token);
    }
    
    private void OnViewGuideClick(object? sender, EventArgs _)
    {
        if (this._pipeline == null)
            return;

        if (this._pipeline.GuideLink == null)
        {
            MessageBox.Show("No guide exists for this patch method yet, so stay tuned!", MessageBoxType.Warning);
            return;
        }

        string url = this._pipeline.GuideLink;
        
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
                            $"Exception details: {e.GetType().Name} {e.Message}", MessageBoxType.Error);
            SentrySdk.CaptureException(e);
        }
        // based off of https://stackoverflow.com/a/43232486
    }
    
    private void OnRevertEbootClick(object? sender, EventArgs e)
    {
        if (this._pipeline == null)
            return;
        
        if (this._pipeline.State == PipelineState.Running)
            return;
        
        this.AddFormInputsToPipeline();
        
        Task.Run(async () =>
        {
            try
            {
                await this._pipeline!.RevertGameEbootAsync();
                await Application.Instance.InvokeAsync(() =>
                {
                    MessageBox.Show("The EBOOT was successfully reverted to the original copy.", "Success!");
                });
            }
            catch (Exception ex)
            {
                State.Logger.LogError(LogType.Pipeline, $"Error while reverting the game's EBOOT: {ex}");
            }
        });
    }
    
    private static TableRow AddField<TControl>(StepInput input, Button? button = null) where TControl : Control, new()
    {
        Label label = new()
        {
            Text = input.Name + ':',
            VerticalAlignment = VerticalAlignment.Center,
            ToolTip = input.Id,
        };

        Control control = new TControl();
        TextBox? textBox = control as TextBox;

        string? newValue = input.DetermineDefaultValue?.Invoke();

        if (textBox != null)
        {
            textBox.Text = newValue;
            textBox.PlaceholderText = input.Placeholder;
        }
        else if (control is FilePicker filePicker)
        {
            filePicker.FilePath = newValue;

            // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
            filePicker.FileAction = input.Type switch
            {
                StepInputType.Directory => FileAction.SelectFolder,
                StepInputType.OpenFile => FileAction.OpenFile,
                StepInputType.SaveFile => FileAction.SaveFile,
                _ => throw new ArgumentOutOfRangeException(),
            };
        }

        if (button != null)
        {
            DynamicLayout buttonLayout = new();
            buttonLayout.AddRow(button, control);
            buttonLayout.Spacing = new Size(5, 0);
            
            return new TableRow(label, buttonLayout);
        }

        return new TableRow(label, control);
    }
    
    private void OnLog(RefresherLog log)
    {
        Application.Instance.Invoke(() =>
        {
            this._messages.Items.Add($"[{log.Level}] [{log.Category}] {log.Content}");
        
            // automatically scroll to the bottom by highlighting the last item temporarily
            this._messages.SelectedIndex = this._messages.Items.Count - 1;
            this._messages.SelectedIndex = -1;

            if (log.Level <= LogLevel.Error)
            {
                MessageBox.Show(log.Content, $"{log.Category} {log.Level.ToString()}", MessageBoxType.Error);
            }
        });
    }
}