using Eto.Drawing;
using Eto.Forms;
using Refresher.Core;
using Refresher.Core.Logging;
using Refresher.Core.Pipelines;

namespace Refresher.UI;

public class PipelineForm<TPipeline> : RefresherForm where TPipeline : Pipeline, new()
{
    private TPipeline? _pipeline;

    private readonly Button _button;
    private readonly ProgressBar _progressBar;
    private readonly ListBox _messages;

    private CancellationTokenSource? _cts;
    
    public PipelineForm() : base(typeof(TPipeline).Name, new Size(700, -1), false)
    {
        this.Content = new Splitter
        {
            Orientation = Orientation.Vertical,
            Panel1 = new Label { Text = "test" },
            Panel2 = new StackLayout([
                this._messages = new ListBox { Height = 200 },
                this._button = new Button(this.OnButtonClick) { Text = "Execute" },
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
        this._progressBar.Enabled = this._pipeline?.State == PipelineState.Running;
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
    
    private void OnLog(RefresherLog log)
    {
        this._messages.Items.Add($"[{log.Level}] [{log.Category}] {log.Content}");
    }
}