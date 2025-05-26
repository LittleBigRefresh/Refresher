using Refresher.Core.Pipelines.Steps;

namespace Refresher.Core.Pipelines.Lbp;

public class PatchworkPs3ConfigPipeline : Pipeline
{
    public override string Id => "patchwork-config-ps3";
    public override string Name => "Patchwork PS3 Config";

    protected override Type SetupAccessorStepType => typeof(SetupPS3AccessorStep);

    protected override List<Type> StepTypes =>
    [
        typeof(UploadPatchworkSprxStep),
        typeof(UploadPatchworkConfigurationStep),
    ];
}