using Refresher.Core.Accessors;
using Refresher.Core.Patching.Pipelines;

namespace Refresher.Core.Patching.Steps.PS3;

public class SetupPS3AccessorStep : Step
{
    public SetupPS3AccessorStep(Pipeline pipeline) : base(pipeline)
    {}

    public override string Name { get; } = "Preparing to access files on your PS3";
    public override float Progress { get; protected set; }

    public override List<StepInput> Inputs { get; } = [
        CommonStepInputs.ConsoleIP,
    ];

    public override Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        string remoteIp = CommonStepInputs.ConsoleIP.GetValueFromPipeline(this.Pipeline);
        State.Logger.LogDebug(LogType.PS3, $"Using PS3 IP {remoteIp}");
        
        PatchAccessor.Try(this, () =>
        {
            this.Pipeline.Accessor = new ConsolePatchAccessor(remoteIp);
        });

        return Task.CompletedTask;
    }
}