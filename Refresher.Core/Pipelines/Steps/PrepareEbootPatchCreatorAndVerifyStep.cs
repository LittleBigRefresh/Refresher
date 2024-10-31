using Refresher.Core.Patching;
using Refresher.Core.Verification;

namespace Refresher.Core.Pipelines.Steps;

public class PrepareEbootPatchCreatorAndVerifyStep : Step
{
    public PrepareEbootPatchCreatorAndVerifyStep(Pipeline pipeline) : base(pipeline)
    {}

    public override List<StepInput> Inputs =>
    [
        CommonStepInputs.RPCS3Folder,
        CommonStepInputs.ServerUrl,
    ];

    public override float Progress { get; protected set; }
    public override Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        string url = this.Pipeline.Inputs["url"];

        EbootPatcher patcher = new(File.Open(this.Game.DecryptedEbootPath!, FileMode.Open, FileAccess.ReadWrite));
        patcher.GenerateRpcs3Patch = true;
        patcher.Rpcs3PatchFolder = Path.GetFullPath(Path.Combine(this.Pipeline.Inputs["hdd0-path"], "..", "patches"));
        patcher.TitleId = this.Game.TitleId;
        patcher.GameName = this.Game.Name;
        patcher.GameVersion = this.Game.Version;
        
        State.Logger.LogDebug(RPCS3, $"RPCS3 patches folder: {patcher.Rpcs3PatchFolder}");

        this.Pipeline.Patcher = patcher;

        List<Message> messages = patcher.Verify(url, this.AutoDiscover?.UsesCustomDigestKey ?? false);
        foreach (Message message in messages)
        {
            State.Logger.LogInfo(Patcher, message.ToString());
        }

        if (messages.Any(m => m.Level == MessageLevel.Error))
        {
            throw new Exception("There were errors while verifying the patch details against the EBOOT. Check the log for more information.");
        }
        
        return Task.CompletedTask;
    }
}