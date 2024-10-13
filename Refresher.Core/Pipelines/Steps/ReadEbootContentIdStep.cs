using SCEToolSharp;

namespace Refresher.Core.Pipelines.Steps;

public class ReadEbootContentIdStep : Step
{
    public ReadEbootContentIdStep(Pipeline pipeline) : base(pipeline)
    {}

    public override float Progress { get; protected set; }
    public override Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        string ebootPath = this.Pipeline.DownloadedEbootPath!;
        
        LibSceToolSharp.Init();
        this.Progress = 0.5f;
        
        string? contentId = LibSceToolSharp.GetContentId(ebootPath)?.TrimEnd('\0');
        if (contentId == null)
            throw new Exception("Unable to retrieve the content ID from the game's EBOOT.");
        this.Progress = 1f;

        this.Game.ContentId = contentId;
        State.Logger.LogDebug(InfoRetrieval, "Got content ID from the game's EBOOT: {0}", contentId);
        return Task.CompletedTask;
    }
}