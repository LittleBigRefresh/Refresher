using Refresher.Core.Patching.Steps.RPCS3;

namespace Refresher.Core.Patching.Pipelines.Lbp;

public class PatchworkRPCS3ConfigPipeline : PatchworkConfigPipeline
{
    protected override string ConsoleName => "RPCS3";
    protected override Type? SetupAccessorStepType => typeof(SetupEmulatorAccessorStep);
}