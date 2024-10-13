using System.Collections.Frozen;
using System.Diagnostics;
using Refresher.Core.Accessors;
using Refresher.Core.Patching;
using GlobalState = Refresher.Core.State;

namespace Refresher.Core.Pipelines;

public abstract class Pipeline
{
    public abstract string Id { get; }
    public abstract string Name { get; }
    
    public readonly Dictionary<string, string> Inputs = [];
    public FrozenSet<StepInput> RequiredInputs { get; private set; }
    
    public IPatcher? Patcher { get; set; }
    public PatchAccessor? Accessor { get; internal set; }
    public GameInformation? GameInformation { get; internal set; }
    public EncryptionDetails? EncryptionDetails { get; internal set; }
    
    public PipelineState State { get; private set; } = PipelineState.NotStarted;
    
    public float Progress
    {
        get
        {
            float completed = (this._currentStepIndex - 1) / (float)this._steps.Count;
            float currentStep = this._currentStep?.Progress ?? 0f;
            float stepWeight = 1f / this._steps.Count;
            
            return completed + currentStep * stepWeight;
        }
    }

    public float CurrentProgress => this._currentStep?.Progress ?? 0;

    protected abstract List<Type> StepTypes { get; }
    private List<Step> _steps = [];
    
    private byte _currentStepIndex;
    private Step? _currentStep;

    public void Initialize()
    {
        List<StepInput> requiredInputs = [];
        
        this._steps = new List<Step>(this.StepTypes.Count);
        foreach (Type type in this.StepTypes)
        {
            Debug.Assert(type.IsAssignableTo(typeof(Step)));

            Step step = (Step)Activator.CreateInstance(type, this)!;

            this._steps.Add(step);
            requiredInputs.AddRange(step.Inputs);
        }
        
        this.RequiredInputs = requiredInputs.DistinctBy(i => i.Id).ToFrozenSet();
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (this.State != PipelineState.NotStarted)
        {
            this.State = PipelineState.Error;
            throw new InvalidOperationException("Pipeline must be restarted before it can be executed again.");
        }
        
        foreach (StepInput input in this.RequiredInputs)
        {
            if(!this.Inputs.ContainsKey(input.Id))
                throw new InvalidOperationException($"Input {input.Id} was not provided to the pipeline before execution.");
        }

        GlobalState.Logger.LogInfo(LogType.Pipeline, $"Pipeline {this.GetType().Name} started.");
        this.State = PipelineState.Running;
        
        byte i = 1;
        foreach (Step step in this._steps)
        {
            GlobalState.Logger.LogInfo(LogType.Pipeline, $"Executing {step.GetType().Name}... ({i}/{this._steps.Count})");
            this._currentStepIndex = i;
            this._currentStep = step;

            try
            {
                await step.ExecuteAsync(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
            }
            catch (TaskCanceledException)
            {
                this.State = PipelineState.Cancelled;
                return;
            }
            catch (Exception)
            {
                this.State = PipelineState.Error;
                throw;
            }

            i++;
        }

        GlobalState.Logger.LogInfo(LogType.Pipeline, $"Pipeline {this.GetType().Name} finished!");
        this.State = PipelineState.Finished;
    }
}