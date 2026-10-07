using Refresher.Core.Patching.Steps.Common;
using Refresher.Core.Patching.Steps.Legacy;
using Refresher.Core.Patching.Steps.PS3;

namespace Refresher.Core.Patching.Pipelines.Legacy;

public class PS3PatchPipeline : Pipeline
{
    public override string Name => "PS3 Patch (any game)";

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
        typeof(PrepareEbootPatcherAndVerifyStep),
        typeof(ApplyPatchToEbootStep),
        
        // Encryption and upload stage
        typeof(EncryptGameEbootStep),
        typeof(BackupGameEbootBeforeReplaceStep),
        typeof(UploadGameEbootStep),
    ];
}