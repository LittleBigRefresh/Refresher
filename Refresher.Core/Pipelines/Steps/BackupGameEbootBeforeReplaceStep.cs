using Refresher.Core.Accessors;

namespace Refresher.Core.Pipelines.Steps;

public class BackupGameEbootBeforeReplaceStep : Step
{
    public BackupGameEbootBeforeReplaceStep(Pipeline pipeline) : base(pipeline)
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
                this.Progress = 0.5f;
                this.Pipeline.Accessor.CopyFile(eboot, backup);
            }
        });

        return Task.CompletedTask;
    }
}