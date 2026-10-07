using Refresher.Core.Patching.Pipelines;

namespace Refresher.Core.Patching.Steps.Legacy;

public class ApplyPatchToEbootStep : Step
{
    public ApplyPatchToEbootStep(Pipeline pipeline) : base(pipeline)
    {}
    
    public override List<StepInput> Inputs =>
    [
        CommonStepInputs.ServerUrl,
    ];

    public override string Name { get; } = "Writing server data into your EBOOT";
    public override float Progress { get; protected set; }
    public override Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        string url = this.Pipeline.Inputs["url"];
        this.Pipeline.Patcher!.Patch(url, this.AutoDiscover?.UsesCustomDigestKey ?? false);
        return Task.CompletedTask;
    }
}