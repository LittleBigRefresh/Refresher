using Refresher.Core.Pipelines.Steps;

namespace Refresher.Core.Pipelines;

public class ExamplePipeline : Pipeline
{
    public override string Id => "example";
    public override string Name => "Example Pipeline";
    protected override List<Type> StepTypes { get; } =
    [
        typeof(ExampleStep),
        typeof(ExampleStep),
        typeof(ExampleStep),
        typeof(ExampleStep),
        typeof(ExampleStep),
    ];
}