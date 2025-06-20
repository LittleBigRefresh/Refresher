using SCEToolSharp;

namespace Refresher.Core.Pipelines.Steps;

public class DecryptGameEbootStep : Step
{
    public DecryptGameEbootStep(Pipeline pipeline) : base(pipeline)
    {}

    public override float Progress { get; protected set; }
    public override Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        LibSceToolSharp.PrintInfos(this.Game.DownloadedEbootPath!);
        
        string tempFile = this.Game.DecryptedEbootPath = Path.GetTempFileName();
        LibSceToolSharp.Decrypt(this.Game.DownloadedEbootPath!, tempFile);
        
        // HACK: scetool doesn't give us result codes, check if the file has been written to instead
        if (new FileInfo(tempFile).Length == 0)
        {
            return this.Fail($"Decryption of the EBOOT failed. Support info: game='{this.Game}' npdrm='{this.Game.ShouldUseNpdrmEncryption}'");
        }
        
        return Task.CompletedTask;
    }
}