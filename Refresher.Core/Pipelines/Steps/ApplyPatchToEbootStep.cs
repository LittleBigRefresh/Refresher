namespace Refresher.Core.Pipelines.Steps;

public class ApplyPatchToEbootStep : Step
{
    public ApplyPatchToEbootStep(Pipeline pipeline) : base(pipeline)
    {}
    
    public override List<StepInput> Inputs =>
    [
        CommonStepInputs.ServerUrl,
    ];

    public override float Progress { get; protected set; }
    public override Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        string url = this.Pipeline.Inputs["url"];
        this.Pipeline.Patcher!.Patch(url, true); // TODO: handle autodiscover in pipelines
        return Task.CompletedTask;
    }
}