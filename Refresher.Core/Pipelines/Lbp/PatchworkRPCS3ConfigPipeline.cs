using Refresher.Core.Pipelines.Steps;

namespace Refresher.Core.Pipelines.Lbp;

public class PatchworkRPCS3ConfigPipeline : PatchworkConfigPipeline
{
    protected override string ConsoleName => "RPCS3";
    protected override Type? SetupAccessorStepType => typeof(SetupEmulatorAccessorStep);
}