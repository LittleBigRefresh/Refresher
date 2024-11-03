namespace Refresher.Core.Pipelines.Steps;

public class EncryptGameEbootStep : Step
{
    public EncryptGameEbootStep(Pipeline pipeline) : base(pipeline)
    {}

    public override float Progress { get; protected set; }
    public override Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (this.Game.ShouldUseNpdrmEncryption ?? this.Game.TitleId.StartsWith('N'))
        {
            State.Logger.LogDebug(Crypto, "Will encrypt using Npdrm");
            // TODO: LibSceToolSharp.SetNpdrmEncryptOptions();
            // TODO: LibSceToolSharp.SetNpdrmContentId(this.Game.ContentId!);
        }
        else
        {
            State.Logger.LogDebug(Crypto, "Will encrypt using Disc");
            // TODO: LibSceToolSharp.SetDiscEncryptOptions();
        }
        
        string tempFile = this.Game.EncryptedEbootPath = Path.GetTempFileName();
        // TODO: LibSceToolSharp.Encrypt(this.Game.DecryptedEbootPath!, tempFile);
        
        // HACK: scetool doesn't give us result codes, check if the file has been written to instead
        if (new FileInfo(tempFile).Length == 0)
        {
            throw new Exception("Encryption failed.");
        }
        
        return Task.CompletedTask;
    }
}