using Refresher.Core.Pipelines.Steps;

namespace Refresher.Core.Pipelines.Lbp;

public class LbpPS3PatchPipeline : Pipeline
{
    public override string Id => "lbp-ps3-patch";
    public override string Name => "LBP PS3 Patch";

    protected override Type SetupAccessorStepType => typeof(SetupPS3AccessorStep);
    public override bool ReplacesEboot => true;

    public override string GuideLink => "https://docs.littlebigrefresh.com/ps3";

    protected override List<Type> StepTypes =>
    [
        // Info gathering stage
        typeof(ValidateGameStep),
        typeof(DownloadParamSfoStep),
        typeof(DownloadGameEbootStep),
        typeof(ReadEbootContentIdStep),
        typeof(DownloadGameLicenseStep),
        typeof(GetConsoleIdpsStep),
        
        // Decryption and patch stage
        typeof(PrepareSceToolStep),
        typeof(DecryptGameEbootStep),
        typeof(ApplySprxPatchToEbootStep),
        
        // Encryption and upload stage
        typeof(EncryptGameEbootStep),
        typeof(BackupGameEbootBeforeReplaceStep),
        typeof(UploadPatchworkSprxStep),
        typeof(UploadPatchworkConfigurationStep),
        typeof(UploadGameEbootStep),
    ];
}