using Refresher.Core.Accessors;

namespace Refresher.Core.Pipelines.Steps;

public class UploadGameEbootElfStep : Step
{
    public UploadGameEbootElfStep(Pipeline pipeline) : base(pipeline)
    {
    }

    public override float Progress { get; protected set; }
    public override Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        PatchAccessor.Try(this, () =>
        {
            string titleId = this.Game.TitleId;
            string usrDir = $"game/{titleId}/USRDIR";
        
            string eboot = Path.Combine(usrDir, "EBOOT.elf");
            
            if (this.Pipeline.Accessor!.FileExists(eboot))
                this.Pipeline.Accessor.RemoveFile(eboot);

            this.Progress = 0.5f;
            
            this.Pipeline.Accessor.UploadFile(this.Game.DecryptedEbootPath!, eboot);
        });

        return Task.CompletedTask;
    }
}