namespace Refresher.Core.Pipelines.Steps;

public class ExampleStep : Step
{
    public ExampleStep(Pipeline pipeline) : base(pipeline)
    {
    }

    public override float Progress { get; protected set; }

    public override async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(1000, cancellationToken);
    }
}