using _Microsoft.Android.Resource.Designer;
using Android.OS;
using Android.Text;
using Android.Views;
using Refresher.Core;
using Refresher.Core.Logging;
using Refresher.Core.Pipelines;
using static Android.Views.ViewGroup.LayoutParams;

namespace Refresher.AndroidApp;

[Activity]
public class PipelineActivity : RefresherActivity
{
    internal static PipelineActivity Instance { get; private set; }
    
    private Pipeline? _pipeline;
    private CancellationTokenSource? _cts;
    
    private TextView _pipelineState = null!;
    private LinearLayout _pipelineInputs = null!;
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
        
        this._button = this.FindViewById<Button>(ResourceConstant.Id.ExecutePipelineButton)!;
        this._button.Click += this.ExecutePipeline;
        
        this._pipelineState = this.FindViewById<TextView>(ResourceConstant.Id.PipelineState)!;
        
        this._progressBar = this.FindViewById<ProgressBar>(ResourceConstant.Id.ProgressBar)!;
        this._currentProgressBar = this.FindViewById<ProgressBar>(ResourceConstant.Id.CurrentProgressBar)!;
        
        this._logScroll = this.FindViewById<ScrollView>(ResourceConstant.Id.GlobalLogScroll)!;
        this._log = this.FindViewById<TextView>(ResourceConstant.Id.GlobalLog)!;
        State.Log += this.OnLog;
        
        this._pipelineInputs = this.FindViewById<LinearLayout>(ResourceConstant.Id.PipelineInputs)!;

        this.InitializePipeline();
        this.UpdateFormStateLoop();
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

        this._log.Text = string.Empty;

        ViewGroup.LayoutParams layoutParams = new(MatchParent, WrapContent);
        foreach (StepInput input in pipeline.RequiredInputs)
        {
            EditText view = new(this);
            view.Hint = $"{input.Name} (e.g. {input.Placeholder})";
            view.Tag = input.Id;
            view.LayoutParameters = layoutParams;

            if (input.Type is StepInputType.Url or StepInputType.ConsoleIp)
                view.InputType = InputTypes.ClassText | InputTypes.TextVariationUri;
            else
                view.InputType = InputTypes.ClassText | InputTypes.TextVariationNormal;
            
            this._pipelineInputs.AddView(view);
        }
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
            this._pipeline.Reset();
        }
        
        this.UpdateFormState();

        int inputCount = this._pipelineInputs.ChildCount;
        for (int i = 0; i < inputCount; i++)
        {
            EditText child = (EditText)this._pipelineInputs.GetChildAt(i)!;
            string id = (string)child.Tag!;
            string value = child.Text!;
            
            this._pipeline.Inputs.Add(id, value);
        }

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

        this._progressBar.Progress = (int)((this._pipeline?.Progress ?? 0) * 100);
        this._currentProgressBar.Progress = (int)((this._pipeline?.CurrentProgress ?? 0) * 100);
    }

    private void UpdateFormStateLoop()
    {
        this.UpdateFormState();
        this._handler.PostDelayed(this.UpdateFormStateLoop, this._pipeline?.State == PipelineState.Running ? 16 : 1000);
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