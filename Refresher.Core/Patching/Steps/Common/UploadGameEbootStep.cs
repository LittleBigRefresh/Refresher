using Refresher.Core.Accessors;
using Refresher.Core.Patching.Pipelines;

namespace Refresher.Core.Patching.Steps.Common;

public class UploadGameEbootStep : Step
{
    public UploadGameEbootStep(Pipeline pipeline) : base(pipeline)
    {
    }

    public override string Name { get; } = "Uploading patched EBOOT";
    public override float Progress { get; protected set; }
    public override Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        PatchAccessor.Try(this, () =>
        {
            string titleId = this.Game.TitleId;
            string usrDir = $"game/{titleId}/USRDIR";
        
            string eboot = Path.Combine(usrDir, "EBOOT.BIN");
            
            if (this.Pipeline.Accessor!.FileExists(eboot))
                this.Pipeline.Accessor.RemoveFile(eboot);

            this.Progress = 0.5f;
            
            this.Pipeline.Accessor.UploadFile(this.Game.EncryptedEbootPath!, eboot);
        });

        return Task.CompletedTask;
    }
}