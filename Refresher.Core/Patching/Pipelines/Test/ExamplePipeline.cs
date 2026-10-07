using Refresher.Core.Patching.Steps.Test;

namespace Refresher.Core.Patching.Pipelines.Test;

public class ExamplePipeline : Pipeline
{
    public override string Name => "Example Pipeline";
    protected override List<Type> StepTypes { get; } =
    [
        typeof(ExampleInputStep),
        typeof(DelayOneSecondStep),
        typeof(DelayOneSecondStep),
        typeof(DelayOneSecondStep),
        typeof(DelayOneSecondStep),
        typeof(DelayOneSecondStep),
    ];
}