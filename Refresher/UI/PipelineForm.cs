using Eto.Drawing;
using Eto.Forms;
using Refresher.Core.Pipelines;

namespace Refresher.UI;

public class PipelineForm<TPipeline> : RefresherForm where TPipeline : Pipeline, new()
{
    private TPipeline? _pipeline;
    
    private readonly Label _stateLabel;
    private readonly ProgressBar _progressBar;
    
    public PipelineForm() : base(typeof(TPipeline).Name, new Size(700, 1), false)
    {
        this.Content = new StackLayout([
            this._stateLabel = new Label(),
            new Button(this.ExecutePipeline) { Text = "Execute" },
            this._progressBar = new ProgressBar(),
        ])
        {
            Padding = new Padding(0, 10, 0, 0),
            Spacing = 5,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Bottom,
        };
        
        this.InitializePipeline();

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

    private void UpdateFormState()
    {
        this._progressBar.Value = (int)((this._pipeline?.Progress ?? 0) * 100);
        this._progressBar.Enabled = this._pipeline?.State == PipelineState.Running;
        this._stateLabel.Text = this._pipeline?.State.ToString() ?? "Uninitialized";
    }

    private void InitializePipeline()
    {
        this._pipeline = new TPipeline();
        this._pipeline.Initialize();
        
        this.UpdateFormState();
    }

    private void ExecutePipeline(object? sender, EventArgs e)
    {
        if (this._pipeline == null)
            return;
        
        Task.Run(async () =>
        {
            await this._pipeline.ExecuteAsync();
        });
        
        this.UpdateFormState();
    }
}