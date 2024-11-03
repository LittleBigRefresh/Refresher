using System.Net.Mime;
using Refresher.Core.Verification.AutoDiscover;

namespace Refresher.Core.Pipelines;

/// <summary>
/// A set of common utilities to assist in controlling a pipeline from a UI.
/// </summary>
public sealed class PipelineController
{
    private readonly Pipeline _pipeline;
    private readonly Action<Action> _uiThread;
    
    private CancellationTokenSource? _cts;
    private CancellationTokenSource? _autoDiscoverCts;

    public PipelineController(Pipeline pipeline, Action<Action> uiThread)
    {
        this._pipeline = pipeline;
        this._uiThread = uiThread;
    }

    public bool PipelineRunning => this._cts != null;
    public bool AutoDiscoverRunning => this._autoDiscoverCts != null;

    public bool EnableControls => this._pipeline.State != PipelineState.Running;
    
    public string MainButtonText => this._pipeline.State switch
    {
        PipelineState.NotStarted => "Patch!",
        PipelineState.Running => "Patching... (click to cancel)",
        PipelineState.Finished => "Complete!",
        PipelineState.Cancelled => "Patch!",
        PipelineState.Error => "Retry",
        _ => throw new ArgumentOutOfRangeException(),
    };

    private string? _autoDiscoverButtonText;
    public string AutoDiscoverButtonText
    {
        get
        {
            if(this._autoDiscoverButtonText != null)
                return this._autoDiscoverButtonText;

            if (this.AutoDiscoverRunning)
                return "Running AutoDiscover... (click to cancel)";
            
            return "AutoDiscover";
        }
    }

    public void CancelPipeline()
    {
        this._cts?.Cancel();
    }
    
    public void CancelAutoDiscover()
    {
        this._autoDiscoverCts?.Cancel();
    }

    public void MainButtonClick()
    {
        if (this._pipeline.State == PipelineState.Running)
        {
            this.CancelPipeline();
            return;
        }

        this._cts = new CancellationTokenSource();
        
        if (this._pipeline.State is PipelineState.Cancelled or PipelineState.Error or PipelineState.Finished)
        {
            this._pipeline.Reset();
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
    }

    public void AutoDiscoverButtonClick(string url, Action<AutoDiscoverResponse> onSuccess)
    {
        if (this._autoDiscoverCts != null)
        {
            this.CancelAutoDiscover();
            return;
        }

        this._autoDiscoverCts = new CancellationTokenSource();
        
        Task.Run(async () =>
        {
            try
            {
                AutoDiscoverResponse? autoDiscover =
                    await this._pipeline.InvokeAutoDiscoverAsync(url, this._autoDiscoverCts.Token);

                if (autoDiscover != null)
                {
                    this._autoDiscoverButtonText = $"AutoDiscover [locked to {autoDiscover.ServerBrand}]";
                    this._uiThread.Invoke(() => onSuccess.Invoke(autoDiscover));
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
}