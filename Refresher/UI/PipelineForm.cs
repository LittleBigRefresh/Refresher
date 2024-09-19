using Eto.Drawing;
using Eto.Forms;
using Refresher.Core.Pipelines;

namespace Refresher.UI;

public class PipelineForm<TPipeline> : RefresherForm where TPipeline : Pipeline, new()
{
    private TPipeline? _pipeline;
    
    private ProgressBar _progressBar;
    
    public PipelineForm() : base(typeof(TPipeline).Name, new Size(700, 1), false)
    {
        this.InitializePipeline();
        
        this.Content = new StackLayout([
            new Button(this.ExecutePipeline),
            this._progressBar = new ProgressBar(),
        ]);

        Thread progressThread = new(() =>
        {
            while (!this.IsDisposed && !Application.Instance.IsDisposed)
            {
                Application.Instance.Invoke(() =>
                {
                    this._progressBar.Value = (int)((this._pipeline?.Progress ?? 0) * 100);
                    this._progressBar.Enabled = this._pipeline?.State == PipelineState.Running;
                });
                Thread.Sleep(100);
            }
        });
        
        progressThread.Start();
    }

    private void InitializePipeline()
    {
        this._pipeline = new TPipeline();
        this._pipeline.Initialize();
    }

    private void ExecutePipeline(object? sender, EventArgs e)
    {
        Task.Run(async () =>
        {
            await this._pipeline.ExecuteAsync();
        });
    }
}