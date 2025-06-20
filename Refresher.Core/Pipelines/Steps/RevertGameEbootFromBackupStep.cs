using Refresher.Core.Accessors;

namespace Refresher.Core.Pipelines.Steps;

public class RevertGameEbootFromBackupStep : Step
{
    public RevertGameEbootFromBackupStep(Pipeline pipeline) : base(pipeline)
    {}

    public override float Progress { get; protected set; }
    public override Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        PatchAccessor.Try(this, () =>
        {
            string titleId = this.Game.TitleId;
            string usrDir = $"game/{titleId}/USRDIR";
        
            string eboot = Path.Combine(usrDir, "EBOOT.BIN");
            string backup = Path.Combine(usrDir, "EBOOT.BIN.ORIG");

            if (!this.Pipeline.Accessor!.FileExists(backup))
            {
                this.Fail("The original backup couldn't be found. Is your game unpatched?");
                return;
            }
            
            this.Progress = 0.25f;

            if (this.Pipeline.Accessor!.FileExists(eboot))
            {
                this.Progress = 0.50f;
                this.Pipeline.Accessor.RemoveFile(eboot);
            }

            this.Progress = 0.75f;
            this.Pipeline.Accessor.CopyFile(backup, eboot);
        });

        this.Progress = 1.0f;
        return Task.CompletedTask;
    }
}