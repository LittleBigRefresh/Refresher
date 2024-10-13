using Refresher.Core.Patching;

namespace Refresher.Core.Pipelines;

public abstract class Step
{
    protected Pipeline Pipeline { get; }
    public abstract float Progress { get; protected set; }

    public virtual List<StepInput> Inputs { get; } = [];

    protected GameInformation Game => this.Pipeline.GameInformation!;
    protected EncryptionDetails Encryption => this.Pipeline.EncryptionDetails!;

    protected Step(Pipeline pipeline)
    {
        this.Pipeline = pipeline;
    }

    public abstract Task ExecuteAsync(CancellationToken cancellationToken = default);
}