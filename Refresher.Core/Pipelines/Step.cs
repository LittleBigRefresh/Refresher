namespace Refresher.Core.Pipelines;

public abstract class Step
{
    protected Pipeline Pipeline { get; }
    public abstract float Progress { get; protected set; }

    public virtual List<StepInput> Inputs { get; } = [];

    protected Step(Pipeline pipeline)
    {
        this.Pipeline = pipeline;
    }

    public abstract Task ExecuteAsync(CancellationToken cancellationToken = default);
}