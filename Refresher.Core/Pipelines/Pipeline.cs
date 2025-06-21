using System.Collections.Frozen;
using System.Diagnostics;
using Refresher.Core.Accessors;
using Refresher.Core.Patching;
using Refresher.Core.Pipelines.Steps;
using Refresher.Core.Platform;
using Refresher.Core.Storage;
using Refresher.Core.Verification.AutoDiscover;
using GlobalState = Refresher.Core.State;

namespace Refresher.Core.Pipelines;

public abstract class Pipeline : IAccessesPlatform
{
    public abstract string Id { get; }
    public abstract string Name { get; }
    
    public readonly Dictionary<string, string> Inputs = [];
    public FrozenSet<StepInput> RequiredInputs { get; private set; } = null!;

    public IPlatformInterface Platform { get; private set; } = null!;

    internal List<GameInformation>? GameList { get; set; } = null;
    
    public IPatcher? Patcher { get; internal set; }
    public PatchAccessor? Accessor { get; internal set; }
    public GameInformation? GameInformation { get; internal set; }
    public EncryptionDetails? EncryptionDetails { get; internal set; }
    public AutoDiscoverResponse? AutoDiscover { get; internal set; }

    public virtual string? GuideLink => null;

    public virtual IEnumerable<string> GameNameFilters => [];
    
    public PipelineState State { get; private set; } = PipelineState.NotStarted;
    
    public float Progress
    {
        get
        {
            if (this.State == PipelineState.Finished)
                return 1;
            
            float completed = (this._currentStepIndex - 1) / (float)this._stepCount;
            float currentStep = this._currentStep?.Progress ?? 0f;
            float stepWeight = 1f / this._stepCount;
            
            return completed + currentStep * stepWeight;
        }
    }

    public float CurrentProgress => this.State == PipelineState.Finished ? 1 : this._currentStep?.Progress ?? 0;

    protected virtual Type? SetupAccessorStepType => null;
    public virtual bool ReplacesEboot => false;

    protected abstract List<Type> StepTypes { get; }
    private List<Step> _steps = [];
    
    private int _stepCount;
    private byte _currentStepIndex;
    private Step? _currentStep;

    public void Initialize(IPlatformInterface platform)
    {
        this.Platform = platform;

        List<StepInput> requiredInputs = [];
        
        this._steps = new List<Step>(this.StepTypes.Count + 1);
        
        if(this.SetupAccessorStepType != null)
            this.AddStep(requiredInputs, this.SetupAccessorStepType);

        foreach (Type type in this.StepTypes)
            this.AddStep(requiredInputs, type);
        
        this.RequiredInputs = requiredInputs.DistinctBy(i => i.Id).ToFrozenSet();
    }

    public void Reset()
    {
        this.Inputs.Clear();

        this.State = PipelineState.NotStarted;

        this._stepCount = 0;
        this._currentStepIndex = 0;
        this._currentStep = null;

        this.Patcher = null;
        if(this.Accessor is IDisposable disposable)
            disposable.Dispose();
        this.Accessor = null;
        this.GameInformation = null;
        this.EncryptionDetails = null;
    }

    private void AddStep(List<StepInput> requiredInputs, Type type)
    {
        Debug.Assert(type.IsAssignableTo(typeof(Step)));

        Step step = (Step)Activator.CreateInstance(type, this)!;

        this._steps.Add(step);
        requiredInputs.AddRange(step.Inputs);
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
            if (!this.Inputs.TryGetValue(input.Id, out string? data))
            {
                this.State = PipelineState.Error;
                throw new InvalidOperationException($"Input {input.Id} was not provided to the pipeline before execution.");
            }

            if (input.Required && string.IsNullOrWhiteSpace(data))
            {
                this.State = PipelineState.Error;
                this.Platform.WarnPrompt($"Please fill out the required '{input.Name}' input before proceeding, as it is a required field.");
                return;
            }
        }
        
        if(this.Inputs.Count != 0)
            GlobalState.Logger.LogTrace(LogType.Pipeline, "Pipeline inputs:");
        foreach ((string? key, string? value) in this.Inputs)
        {
            GlobalState.Logger.LogTrace(LogType.Pipeline, $"  '{key}' = '{value}'");
        }

        GlobalState.Logger.LogInfo(LogType.Pipeline, $"Pipeline {this.GetType().Name} started.");
        this.State = PipelineState.Running;

        bool success = await this.RunListOfSteps(this._steps, cancellationToken);

        if (success)
        {
            GlobalState.Logger.LogInfo(LogType.Pipeline, $"Pipeline {this.GetType().Name} finished!");
            this.State = PipelineState.Finished;
        
            PreviousInputStorage.ApplyFromPipeline(this);
            PreviousInputStorage.Write();

            if (!this.Inputs.TryGetValue("url", out string? url))
            {
                this.Platform.InfoPrompt("Patch successful!");
                return;
            }
            
            QuestionResult result = this.Platform.Ask(
                "Patch successful!\r\n\r\n" +
                        "Some servers like Bonsai require registering an account to play. " +
                        "Would you like to open the server's website?");

            if (result == QuestionResult.Yes)
            {
                try
                {
                    Uri oldUri = new(url);
                    UriBuilder builder = new()
                    {
                        Host = oldUri.Host,
                        Scheme = "https",
                        Path = "/register",
                        Port = 443,
                    };

                    this.Platform.OpenUrl(builder.Uri);
                }
                catch (Exception e)
                {
                    GlobalState.Logger.LogWarning(LogType.Platform, "Failed to open server URL: " + e);
                    this.Platform.WarnPrompt("Sorry, we couldn't open the URL. You can usually reach it by using the same link as the one you gave to Refresher.");
                }
            }
        }
    }

    private async Task<bool> RunListOfSteps(List<Step> steps, CancellationToken cancellationToken = default)
    {
        this._stepCount = steps.Count;
        byte i = 1;
        foreach (Step step in steps)
        {
            GlobalState.Logger.LogInfo(LogType.Pipeline, $"Executing {step.GetType().Name}... ({i}/{steps.Count})");
            this._currentStepIndex = i;
            this._currentStep = step;

            try
            {
                await step.ExecuteAsync(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
            }
            catch (OperationCanceledException)
            {
                this.State = PipelineState.Cancelled;
                return false;
            }
            catch (Exception)
            {
                this.State = PipelineState.Error;
                throw;
            }

            if (this.State == PipelineState.Error)
            {
                return false;
            }

            i++;
        }

        return true;
    }

    public async Task<AutoDiscoverResponse?> InvokeAutoDiscoverAsync(string url, CancellationToken cancellationToken = default)
    {
        AutoDiscoverResponse? autoDiscover = await AutoDiscoverClient.InvokeAutoDiscoverAsync(url, this.Platform, cancellationToken);
        if(autoDiscover != null)
           this.AutoDiscover = autoDiscover;

        return autoDiscover;
    }

    public async Task<List<GameInformation>?> DownloadGameListAsync(CancellationToken cancellationToken = default)
    {
        if (this.State != PipelineState.NotStarted)
        {
            this.State = PipelineState.Error;
            throw new InvalidOperationException("Pipeline must be in a clean state before downloading games.");
        }
        
        if(this.SetupAccessorStepType == null)
            throw new InvalidOperationException("This pipeline doesn't have accessors configured.");

        List<Step> stepTypes = [
            (Step)Activator.CreateInstance(this.SetupAccessorStepType, this)!,
            new DownloadGameListStep(this),
        ];
        
        this.State = PipelineState.Running;
        
        await this.RunListOfSteps(stepTypes, cancellationToken);
        
        this.Reset();

        // ReSharper disable once InvertIf
        if (this.GameList == null)
        {
            if (this.State != PipelineState.Error)
                this.Fail(null, "Could not download the list of games.");

            return null;
        }

        return this.GameList;
    }
    
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (this.State != PipelineState.NotStarted)
        {
            this.State = PipelineState.Error;
            throw new InvalidOperationException("Pipeline must be in a clean state before downloading games.");
        }
        
        if(this.SetupAccessorStepType == null)
            throw new InvalidOperationException("This pipeline doesn't have accessors configured.");

        List<Step> stepTypes = [
            (Step)Activator.CreateInstance(this.SetupAccessorStepType, this)!,
        ];
        
        this.State = PipelineState.Running;
        
        await this.RunListOfSteps(stepTypes, cancellationToken);
        this.Reset();
    }
    
    public async Task RevertGameEbootAsync(CancellationToken cancellationToken = default)
    {
        if (this.State != PipelineState.NotStarted)
        {
            this.State = PipelineState.Error;
            throw new InvalidOperationException("Pipeline must be in a clean state before reverting the EBOOT.");
        }
        
        if(this.SetupAccessorStepType == null)
            throw new InvalidOperationException("This pipeline doesn't have accessors configured.");

        if (!this.ReplacesEboot)
            throw new InvalidOperationException("This pipeline doesn't replace the EBOOT, so this operation is unnecessary.");

        List<Step> stepTypes = [
            (Step)Activator.CreateInstance(this.SetupAccessorStepType, this)!,
            new ValidateGameStep(this),
            new RevertGameEbootFromBackupStep(this),
        ];
        
        this.State = PipelineState.Running;
        
        await this.RunListOfSteps(stepTypes, cancellationToken);
        
        this.Reset();
    }

    public void Fail(Step? step, string reason)
    {
        this.Platform.ErrorPrompt($"{step?.GetType().Name ?? "Pipeline"} failed: {reason}\n\nPatching cannot continue.");
        this.State = PipelineState.Error;
    }
}