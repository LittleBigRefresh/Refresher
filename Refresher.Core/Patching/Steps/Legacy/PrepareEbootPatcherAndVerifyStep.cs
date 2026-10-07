using Refresher.Core.Patching;
using Refresher.Core.Patching.Pipelines;
using Refresher.Core.Verification;

namespace Refresher.Core.Patching.Steps.Legacy;

public class PrepareEbootPatcherAndVerifyStep : Step
{
    public PrepareEbootPatcherAndVerifyStep(Pipeline pipeline) : base(pipeline)
    {}

    public override List<StepInput> Inputs =>
    [
        CommonStepInputs.ServerUrl,
    ];

    public override string Name { get; } = "Preparing PS3 EBOOT patcher";
    public override float Progress { get; protected set; }
    public override Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        string url = this.Pipeline.Inputs["url"];

        EbootPatcher patcher = new(File.Open(this.Game.DecryptedEbootPath!, FileMode.Open, FileAccess.ReadWrite));

        this.Pipeline.Patcher = patcher;

        List<Message> messages = patcher.Verify(url, true); // TODO: handle autodiscover in pipelines
        foreach (Message message in messages)
        {
            State.Logger.LogInfo(LogType.Patcher, message.ToString());
        }

        if (messages.Any(m => m.Level == MessageLevel.Error))
        {
            return this.Fail("There were errors while verifying the patch details against the EBOOT. Check the log for more information.");
        }
        
        return Task.CompletedTask;
    }
}