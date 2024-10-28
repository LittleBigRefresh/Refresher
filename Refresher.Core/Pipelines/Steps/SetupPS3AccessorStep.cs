using Refresher.Core.Accessors;

namespace Refresher.Core.Pipelines.Steps;

public class SetupPS3AccessorStep : Step
{
    public SetupPS3AccessorStep(Pipeline pipeline) : base(pipeline)
    {}

    public override float Progress { get; protected set; }

    public override List<StepInput> Inputs { get; } = [
        CommonStepInputs.ConsoleIP,
    ];

    public override Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        string remoteIp = CommonStepInputs.ConsoleIP.GetValueFromPipeline(this.Pipeline);
        State.Logger.LogDebug(PS3, $"Using PS3 IP {remoteIp}");
        this.Pipeline.Accessor = new ConsolePatchAccessor(remoteIp);

        return Task.CompletedTask;
    }
}