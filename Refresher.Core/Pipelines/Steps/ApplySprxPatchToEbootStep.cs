using SPRXPatcher.Elf;

namespace Refresher.Core.Pipelines.Steps;

public class ApplySprxPatchToEbootStep : Step
{
    public ApplySprxPatchToEbootStep(Pipeline pipeline) : base(pipeline)
    {}

    public override float Progress { get; protected set; }
    public override async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        string? decryptedEbootPath = this.Game?.DecryptedEbootPath;
        if (decryptedEbootPath == null)
            throw new InvalidOperationException("We haven't decrypted the eboot yet");

        FileStream stream = File.OpenRead(decryptedEbootPath);
        ElfFile elf = new(stream);
        // dispose early to avoid errors while writing in-place
        // above ctor consumes whole stream without reading again
        await stream.DisposeAsync();
        
        this.Progress = 0.5f;
        
        elf.SprxPath = "/dev_hdd0/plugins/patchwork.sprx";

        await using FileStream writeStream = File.OpenWrite(decryptedEbootPath);
        elf.Write(writeStream);
    }
}