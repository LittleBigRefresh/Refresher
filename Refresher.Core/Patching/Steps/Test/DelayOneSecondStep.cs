using System.Diagnostics;
using Refresher.Core.Patching.Pipelines;

namespace Refresher.Core.Patching.Steps.Test;

public class DelayOneSecondStep : Step
{
    public DelayOneSecondStep(Pipeline pipeline) : base(pipeline)
    {
    }

    public override string Name { get; } = "Delaying one second";
    public override float Progress { get; protected set; }

    public override async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        while (stopwatch.ElapsedMilliseconds <= 1000)
        {
            this.Progress = stopwatch.ElapsedMilliseconds / 1000.0f;
            await Task.Delay(10, cancellationToken);
        }
    }
}