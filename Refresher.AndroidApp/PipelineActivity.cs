using _Microsoft.Android.Resource.Designer;
using Android.Graphics;
using Refresher.Core;
using Refresher.Core.Pipelines;

namespace Refresher.AndroidApp;

[Activity]
public class PipelineActivity : RefresherActivity
{
    private Pipeline? _pipeline;
    private CancellationTokenSource? _cts;
    
    private Button _button = null!;
    private TextView _pipelineState = null!;
    
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        this.SetContentView(ResourceConstant.Layout.activity_pipeline);

        this.InitializePipeline();

        this._button = this.FindViewById<Button>(ResourceConstant.Id.ExecutePipelineButton)!;
        this._button.Click += this.ExecutePipeline;
        
        this._pipelineState = this.FindViewById<TextView>(ResourceConstant.Id.PipelineState)!;
    }

    private void InitializePipeline()
    {
        string? pipelineTypeName = this.Intent?.GetStringExtra("PipelineType");
        if(pipelineTypeName == null)
            throw new Exception("Pipeline type not specified");

        Type? pipelineType = typeof(Pipeline).Assembly.GetType(pipelineTypeName);
        if(pipelineType == null)
            throw new Exception("Pipeline was not found");

        Pipeline pipeline = (Pipeline)Activator.CreateInstance(pipelineType)!;
        this._pipeline = pipeline;
        
        if (this.ActionBar != null)
        {
            this.ActionBar.Subtitle = pipeline.Name;
        }
        else
        {
            this.Title = "Refresher - " + pipeline.Name;
        }
    }
    
    private void ExecutePipeline(object? sender, EventArgs e)
    {
        this._pipelineState.Text = "Null";

        if (this._pipeline == null)
            return;
        
        this._pipelineState.Text = this._pipeline.State.ToString();
        
        if (this._pipeline.State == PipelineState.Running)
        {
            this._cts?.Cancel();
            return;
        }
        
        if (this._pipeline.State is PipelineState.Cancelled or PipelineState.Error or PipelineState.Finished)
        {
            this.InitializePipeline();
        }
        
        // State.Logger.LogInfo(LogType.Pipeline, "Starting pipeline task...");
        Task.Run(async () =>
        {
            try
            {
                State.Logger.LogInfo(LogType.Pipeline, "Executing Pipeline...");
                await this._pipeline.ExecuteAsync(this._cts?.Token ?? default);
            }
            catch (Exception ex)
            {
                State.Logger.LogError(LogType.Pipeline, $"Error while running pipeline {this._pipeline.Name}: {ex.Message}");
            }
        }, this._cts?.Token ?? default);
    }
}