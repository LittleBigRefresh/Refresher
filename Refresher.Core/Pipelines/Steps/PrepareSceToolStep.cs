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

        SetRapDirectory(this.Pipeline.LicenseDirectory!);
        SetRifPath(this.Pipeline.LicenseDirectory!);
        
        if(this.Pipeline.DownloadedActDatPath != null)
            SetActDatFilePath(this.Pipeline.DownloadedActDatPath);

        return Task.CompletedTask;
    }
}