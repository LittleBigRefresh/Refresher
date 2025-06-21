using Refresher.Core.Platform;

namespace Refresher.Core.Pipelines.Steps;

public class ExampleInputStep : Step
{
    public ExampleInputStep(Pipeline pipeline) : base(pipeline)
    {
    }

    public override float Progress { get; protected set; }

    public override List<StepInput> Inputs { get; } =
    [
        new("example-input", "Input"),
    ];

    public override Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        this.Platform.InfoPrompt("Info");
        this.Platform.WarnPrompt("Warn");
        this.Platform.ErrorPrompt("Error");
        QuestionResult result = this.Platform.Ask("Question");
        this.Platform.InfoPrompt(result.ToString());
        
        State.Logger.LogInfo(LogType.Pipeline, $"Input was set to '{this.Inputs[0].GetValueFromPipeline(this.Pipeline)}'");
        return Task.CompletedTask;
    }
}