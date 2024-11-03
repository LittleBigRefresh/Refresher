using LibSceSharp;
using Refresher.Core.Patching;

namespace Refresher.Core.Pipelines.Steps;

public class PrepareSceToolStep : Step
{
    public PrepareSceToolStep(Pipeline pipeline) : base(pipeline)
    {
    }

    public override float Progress { get; protected set; }
    public override Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        this.Pipeline.EncryptionDetails = new EncryptionDetails();
        
        LibSce sce = new();
        this.Encryption.Sce = sce;

        return Task.CompletedTask;
    }
}