using Refresher.Core.Patching.Steps.PS3;

namespace Refresher.Core.Patching.Pipelines.Lbp;

public class PatchworkPS3ConfigPipeline : PatchworkConfigPipeline
{
    protected override Type SetupAccessorStepType => typeof(SetupPS3AccessorStep);
    protected override string ConsoleName => "PS3";
}