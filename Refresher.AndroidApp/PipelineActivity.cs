using _Microsoft.Android.Resource.Designer;
using Android.OS;
using Android.Views;
using Refresher.Core;
using Refresher.Core.Logging;
using Refresher.Core.Pipelines;

namespace Refresher.AndroidApp;

[Activity]
public class PipelineActivity : RefresherActivity
{
    internal static PipelineActivity Instance { get; private set; }
    
    private Pipeline? _pipeline;
    private CancellationTokenSource? _cts;
    
    private TextView _pipelineState = null!;
    private ScrollView _logScroll = null!;
    private TextView _log = null!;
    private Button _button = null!;
    private ProgressBar _progressBar = null!;
    private ProgressBar _currentProgressBar = null!;
    
    private readonly Handler _handler = new(Looper.MainLooper!);

    public PipelineActivity()
    {
        Instance = this;
    }
    
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        this.SetContentView(ResourceConstant.Layout.activity_pipeline);

        this.InitializePipeline();

        this._button = this.FindViewById<Button>(ResourceConstant.Id.ExecutePipelineButton)!;
        this._button.Click += this.ExecutePipeline;
        
        this._pipelineState = this.FindViewById<TextView>(ResourceConstant.Id.PipelineState)!;
        
        this._progressBar = this.FindViewById<ProgressBar>(ResourceConstant.Id.ProgressBar)!;
        this._currentProgressBar = this.FindViewById<ProgressBar>(ResourceConstant.Id.CurrentProgressBar)!;

        this.UpdateFormStateLoop();

        this._logScroll = this.FindViewById<ScrollView>(ResourceConstant.Id.GlobalLogScroll)!;
        this._log = this.FindViewById<TextView>(ResourceConstant.Id.GlobalLog)!;
        State.Log += this.OnLog;
    }

    private void InitializePipeline()
    {
        this._cts = new CancellationTokenSource();
        
        string? pipelineTypeName = this.Intent?.GetStringExtra("PipelineType");
        if(pipelineTypeName == null)
            throw new Exception("Pipeline type not specified");

        Type? pipelineType = typeof(Pipeline).Assembly.GetType(pipelineTypeName);
        if(pipelineType == null)
            throw new Exception("Pipeline was not found");

        Pipeline pipeline = (Pipeline)Activator.CreateInstance(pipelineType)!;
        pipeline.Initialize();
        this._pipeline = pipeline;
        
        if (this.ActionBar != null)
        {
            this.ActionBar.Subtitle = pipeline.Name;
        }
        else
        {
            this.Title = "Refresher - " + pipeline.Name;
        }
        
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if(this._log != null)
            this._handler.Post(() =>
            {
                this._log.Text = string.Empty;
            });
    }
    
    private void ExecutePipeline(object? sender, EventArgs e)
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
        
        this.UpdateFormState();
        
        // State.Logger.LogInfo(LogType.Pipeline, "Starting pipeline task...");
        Task.Run(async () =>
        {
            try
            {
                State.Logger.LogInfo(LogType.Pipeline, "Executing Pipeline...");
                await this._pipeline.ExecuteAsync(this._cts?.Token ?? default);
                this.UpdateFormState();
            }
            catch (Exception ex)
            {
                State.Logger.LogError(LogType.Pipeline, $"Error while running pipeline {this._pipeline.Name}: {ex.Message}");
            }
        }, this._cts?.Token ?? default);
    }

    private void UpdateFormState()
    {
        this._pipelineState.Text = this._pipeline?.State.ToString();
        // this._pipelineState.Text = DateTimeOffset.Now.Ticks.ToString();
        
        this._progressBar.Progress = (int)(this._pipeline?.Progress ?? 0 * 100);
        this._currentProgressBar.Progress = (int)(this._pipeline?.CurrentProgress ?? 0 * 100);
    }

    private void UpdateFormStateLoop()
    {
        this.UpdateFormState();
        this._handler.PostDelayed(this.UpdateFormStateLoop, this._pipeline?.State == PipelineState.Running ? 250 : 1000);
    }
    
    private void OnLog(RefresherLog log)
    {
        this._handler.Post(() =>
        {
            this._log.Text += $"[{log.Level}] [{log.Category}] {log.Content}\n";

            this._logScroll.Post(() =>
            {
                this._logScroll.FullScroll(FocusSearchDirection.Down);
            });
        });
    }
}