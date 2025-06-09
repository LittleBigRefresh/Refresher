using Refresher.Core.Accessors;
using Refresher.Core.Pipelines.Steps;

namespace Refresher.Core.Pipelines.Lbp;

public class LbpRPCS3PatchPipeline : Pipeline
{
    public override string Id => "lbp-rpcs3-patch";
    public override string Name => "LBP RPCS3 Patch";

    protected override Type SetupAccessorStepType => typeof(SetupEmulatorAccessorStep);
    public override bool ReplacesEboot => false;

    public override string GuideLink => "https://docs.littlebigrefresh.com/rpcs3";

    public override IEnumerable<string> GameNameFilters => ["littlebigplanet", "lbp"];

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
        typeof(ApplySprxPatchToEbootStep),
        
        // Encryption and upload stage
        // FIXME: encrypting seems to break the elf in rpcs3?
        // typeof(EncryptGameEbootStep),
        // typeof(BackupGameEbootBeforeReplaceStep),
        typeof(UploadPatchworkSprxStep),
        typeof(UploadPatchworkConfigurationStep),
        typeof(UploadGameEbootElfStep), // upload EBOOT.elf for now
    ];
}