using System.Diagnostics;

namespace Refresher.Core.Pipelines.Steps;

public class DelayOneSecondStep : Step
{
    public DelayOneSecondStep(Pipeline pipeline) : base(pipeline)
    {
    }

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