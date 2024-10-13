using Eto;
using Eto.Drawing;
using Eto.Forms;
using Refresher.Core;
using Refresher.Core.Extensions;
using Refresher.Core.Logging;
using Refresher.Core.Pipelines;

namespace Refresher.UI;

public class PipelineForm<TPipeline> : RefresherForm where TPipeline : Pipeline, new()
{
    private TPipeline? _pipeline;

    private readonly Button _button;
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
            Panel1 = this._formLayout = new TableLayout
            {
                Spacing = new Size(5, 5),
                Padding = new Padding(0, 0, 0, 10),
            },
            Panel2 = new StackLayout([
                this._messages = new ListBox { Height = 200 },
                this._button = new Button(this.OnButtonClick) { Text = "Execute" },
                this._currentProgressBar = new ProgressBar(),
                this._progressBar = new ProgressBar(),
            ])
            {
                Padding = new Padding(0, 10, 0, 0),
                Spacing = 5,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Bottom,
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
        this._currentProgressBar.Enabled = this._progressBar.Enabled = this._pipeline?.State == PipelineState.Running;
        this._progressBar.ToolTip = this._pipeline?.State.ToString() ?? "Uninitialized";

        this._button.Text = this._pipeline?.State switch
        {
            PipelineState.NotStarted => "Execute",
            PipelineState.Running => "Cancel",
            PipelineState.Finished => "Retry",
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
                default:
                    throw new ArgumentOutOfRangeException();
            }

            this._formLayout.Rows.Add(row);
        }

        this.UpdateSubtitle(this._pipeline.Name);
        this.UpdateFormState();
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
            this.InitializePipeline();
        }
        
        foreach (TableRow row in this._formLayout.Rows)
        {
            string id = row.Cells[0].Control.ToolTip;
            string value = row.Cells[1].Control.GetUserInput();
            
            this._pipeline.Inputs.Add(id, value);
        }
        
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
    
    private static TableRow AddField<TControl>(StepInput input) where TControl : Control, new()
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

        return new TableRow(label, control);
    }
    
    private void OnLog(RefresherLog log)
    {
        this._messages.Items.Add($"[{log.Level}] [{log.Category}] {log.Content}");
    }
}