using Eto;
using Eto.Drawing;
using Eto.Forms;
using NotEnoughLogs;
using Refresher.Core;
using Refresher.Core.Extensions;
using Refresher.Core.Logging;
using Refresher.Core.Patching;
using Refresher.Core.Pipelines;
using Refresher.Core.Verification.AutoDiscover;
using Pipeline = Refresher.Core.Pipelines.Pipeline;

namespace Refresher.UI;

public class PipelineForm<TPipeline> : RefresherForm where TPipeline : Pipeline, new()
{
    private TPipeline? _pipeline;

    private readonly Button _button;
    private readonly Button _autoDiscoverButton;
    private Button? _connectButton;
    private readonly ProgressBar _currentProgressBar;
    private readonly ProgressBar _progressBar;
    private readonly ListBox _messages;
    
    private readonly TableLayout _formLayout;

    private CancellationTokenSource? _cts;
    
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
                this._button = new Button(this.OnButtonClick) { Text = "Execute" },
                this._autoDiscoverButton = new Button(this.OnAutoDiscoverClick) { Text = "AutoDiscover" },
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
        this._progressBar.Value = (int)((this._pipeline?.Progress ?? 0) * 100);
        this._currentProgressBar.Value = (int)(this._pipeline?.CurrentProgress * 100 ?? 0);
        this._progressBar.ToolTip = this._pipeline?.State.ToString() ?? "Uninitialized";

        bool enableControls = this._pipeline?.State != PipelineState.Running;
        this._currentProgressBar.Enabled = !enableControls;
        this._progressBar.Enabled = !enableControls;
        this._autoDiscoverButton.Enabled = enableControls && this._pipeline?.AutoDiscover == null;

        this._button.Text = this._pipeline?.State switch
        {
            PipelineState.NotStarted => "Execute",
            PipelineState.Running => "Cancel",
            PipelineState.Finished => "Complete!",
            PipelineState.Cancelled => "Execute",
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
                case StepInputType.Text:
                    row = AddField<TextBox>(input);
                    break;
                case StepInputType.Directory:
                    row = AddField<FilePicker>(input);
                    break;
                case StepInputType.ConsoleIp:
                    row = AddField<TextBox>(input, this._connectButton = new Button(this.OnConnectToConsoleClick) { Text = "Connect" });
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
    
    private void OnConnectToConsoleClick(object? sender, EventArgs e)
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
        MessageBox.Show($"Found {games.Count} game(s).", "Success!");
        if (this._connectButton != null)
        {
            this._connectButton.Enabled = false;
            this._connectButton.Text = "Connected!";
        }
    }

    private void OnAutoDiscoverClick(object? sender, EventArgs e)
    {
        if (this._pipeline == null)
            return;
        
        if (this._pipeline.State == PipelineState.Running)
            return;
        
        TextControl control = (TextControl)this._formLayout.Rows
            .Where(r => r.Cells[0].Control.ToolTip == CommonStepInputs.ServerUrl.Id)
            .Select(r => r.Cells[1].Control)
            .First();

        string url = control.Text;

        Task.Run(async () =>
        {
            try
            {
                AutoDiscoverResponse? autoDiscover = await this._pipeline.InvokeAutoDiscoverAsync(url);
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
        }, this._cts?.Token ?? default);
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