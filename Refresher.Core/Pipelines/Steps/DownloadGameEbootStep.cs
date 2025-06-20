using Refresher.Core.Accessors;

namespace Refresher.Core.Pipelines.Steps;

public class DownloadGameEbootStep : Step
{
    public DownloadGameEbootStep(Pipeline pipeline) : base(pipeline)
    {}

    public override float Progress { get; protected set; }
    public override Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        string titleId = this.Game.TitleId;
        string usrDir = $"game/{titleId}/USRDIR";
        
        string ebootPath = Path.Combine(usrDir, "EBOOT.BIN.ORIG"); // Prefer original backup over active copy
        PatchAccessor.Try(this, () =>
        {
            if (this.Pipeline.Accessor!.FileExists(ebootPath)) return;
            // If the backup doesn't exist, use the EBOOT.BIN
            
            State.Logger.LogInfo(Accessor, "Couldn't find an original backup of the EBOOT, using active copy. This is not an error.");
            ebootPath = Path.Combine(usrDir, "EBOOT.BIN");
            
            this.Progress = 0.25f;
                
            if (this.Pipeline.Accessor.FileExists(ebootPath)) return;

            // If we land here, then we have no valid patch target without any way to recover.
            // This is very inconvenient for us and the user.
            this.Fail("The EBOOT.BIN file does not exist, nor does the original backup exist." +
                             "This usually means you haven't installed any updates for your game.");
        });
        
        if(this.Failed)
            return Task.CompletedTask;

        this.Progress = 0.5f;

        string downloadedFile = null!;
        PatchAccessor.Try(this, () =>
        { 
            downloadedFile = this.Pipeline.Accessor!.DownloadFile(ebootPath);
            this.Game.DownloadedEbootPath = downloadedFile;
        });
        
        if(this.Failed)
            return Task.CompletedTask;
        
        State.Logger.LogDebug(Accessor, $"Downloaded EBOOT Path: {downloadedFile}");
        if (!File.Exists(downloadedFile))
        {
            return this.Fail("Could not find the EBOOT we downloaded. This is likely a bug. Patching cannot continue.");
        }
        
        return Task.CompletedTask;
    }
}