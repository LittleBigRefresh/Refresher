using SCEToolSharp;

namespace Refresher.Core.Pipelines.Steps;

public class DecryptGameEbootStep : Step
{
    public DecryptGameEbootStep(Pipeline pipeline) : base(pipeline)
    {}

    public override float Progress { get; protected set; }
    public override Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        string tempFile = this.Pipeline.DecryptedEbootPath = Path.GetTempFileName();
        LibSceToolSharp.Decrypt(this.Pipeline.DownloadedEbootPath!, tempFile);
        
        // HACK: scetool doesn't give us result codes, check if the file has been written to instead
        if (new FileInfo(tempFile).Length == 0)
        {
            throw new Exception("Decryption failed.");
        }
        
        return Task.CompletedTask;
    }
}