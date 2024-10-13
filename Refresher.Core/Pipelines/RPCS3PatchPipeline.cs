using Refresher.Core.Pipelines.Steps;

namespace Refresher.Core.Pipelines;

public class RPCS3PatchPipeline : Pipeline
{
    public override string Id => "rpcs3-patch";
    public override string Name => "RPCS3 Patch";
    protected override List<Type> StepTypes =>
    [
        typeof(SetupEmulatorAccessorStep),
        typeof(ValidateGameStep),
        typeof(DownloadParamSfoStep),
        typeof(DownloadGameEbootStep),
        typeof(ReadEbootContentIdStep),
        typeof(DownloadGameLicenseStep),
    ];
}