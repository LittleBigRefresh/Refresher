using SCEToolSharp;

namespace Refresher.Core.Pipelines.Steps;

public class PrintInfoForEncryptedGameEbootStep : Step
{
    public PrintInfoForEncryptedGameEbootStep(Pipeline pipeline) : base(pipeline)
    {}

    public override float Progress { get; protected set; }
    public override Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        LibSceToolSharp.PrintInfos(this.Game.EncryptedEbootPath);
        return Task.CompletedTask;
    }
}