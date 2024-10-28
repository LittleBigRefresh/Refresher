using Refresher.Core.Pipelines.Steps;

namespace Refresher.Core.Pipelines;

public class RPCS3PatchPipeline : Pipeline
{
    public override string Id => "rpcs3-patch";
    public override string Name => "RPCS3 Patch";

    protected override Type SetupAccessorStepType => typeof(SetupEmulatorAccessorStep);

    protected override List<Type> StepTypes =>
    [
        // Info gathering stage
        typeof(ValidateGameStep),
        typeof(DownloadParamSfoStep),
        typeof(DownloadGameEbootStep),
        typeof(ReadEbootContentIdStep),
        typeof(DownloadGameLicenseStep),
        
        // Decryption and patch stage
        typeof(PrepareSceToolStep),
        typeof(DecryptGameEbootStep),
        typeof(PrepareEbootPatchCreatorAndVerifyStep),
        typeof(ApplyPatchToEbootStep),
        // The patch creator will automatically write to the patch file. No upload steps are required.
    ];
}