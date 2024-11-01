using static SCEToolSharp.LibSceToolSharp;

namespace Refresher.Core.Pipelines.Steps;

public class PrepareSceToolStep : Step
{
    public PrepareSceToolStep(Pipeline pipeline) : base(pipeline)
    {
    }

    public override float Progress { get; protected set; }
    public override Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        Init();

        SetRapDirectory(this.Encryption.LicenseDirectory!);
        SetRifPath(this.Encryption.LicenseDirectory!);
        
        if(this.Encryption.DownloadedActDatPath != null)
            SetActDatFilePath(this.Encryption.DownloadedActDatPath);
        
        if(this.Encryption.ConsoleIdps != null)
            SetIdpsKey(this.Encryption.ConsoleIdps);

        return Task.CompletedTask;
    }
}