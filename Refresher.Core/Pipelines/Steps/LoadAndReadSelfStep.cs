using LibSceSharp;

namespace Refresher.Core.Pipelines.Steps;

public class LoadAndReadSelfStep : Step
{
    public LoadAndReadSelfStep(Pipeline pipeline) : base(pipeline)
    {
    }

    public override float Progress { get; protected set; }
    public override Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        Self self = new(this.Encryption.Sce!, File.ReadAllBytes(this.Game.DownloadedEbootPath!), true);
        this.Encryption.Self = self;
        
        string? contentId = this.Encryption.Self!.ContentId;

        if(contentId != null)
            State.Logger.LogDebug(InfoRetrieval, "Got content ID from the game's EBOOT: {0}", contentId);
        else
            State.Logger.LogDebug(InfoRetrieval, "Unable to find content ID in the game's EBOOT.");
        
        this.Game.ContentId = contentId;
        this.Game.ShouldUseNpdrmEncryption = self.NeedsNpdrmLicense;
        
        return Task.CompletedTask;
    }
}