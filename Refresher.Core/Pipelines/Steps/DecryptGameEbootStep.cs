using LibSceSharp;

namespace Refresher.Core.Pipelines.Steps;

public class DecryptGameEbootStep : Step
{
    public DecryptGameEbootStep(Pipeline pipeline) : base(pipeline)
    {}

    public override float Progress { get; protected set; }
    public override Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        this.Encryption.Self?.Dispose();

        LibSce sce = this.Encryption.Sce!;
        byte[] selfData = File.ReadAllBytes(this.Game.DownloadedEbootPath!);

        // if we don't need to decrypt, just don't. ez
        if (!this.Game.ShouldUseNpdrmEncryption.GetValueOrDefault())
        {
            this.Encryption.Self = new Self(sce, selfData);
        }
        // if we downloaded an act.dat file, we downloaded a rif. use full console decryption.
        if (this.Encryption.DownloadedActDatPath != null)
        {
            byte[] rifData = File.ReadAllBytes(this.Encryption.DownloadedLicensePath!);
            byte[] actData = File.ReadAllBytes(this.Encryption.DownloadedActDatPath!);
            byte[] idps = this.Encryption.ConsoleIdps!;
            this.Encryption.Self = new Self(sce, selfData, rifData, actData, idps);
        }
        // if we still downloaded a license, we downloaded a rap file. use rap decryption.
        else if(this.Encryption.DownloadedLicensePath != null)
        {
            byte[] rapData = File.ReadAllBytes(this.Encryption.DownloadedLicensePath!);
            this.Encryption.Self = new Self(sce, selfData, rapData);
        }
        // otherwise, we're fucked
        else
        {
            throw new InvalidOperationException("No encryption method could be determined.");
        }
        
        string tempFile = this.Game.DecryptedEbootPath = Path.GetTempFileName();
        Span<byte> elfBytes = this.Encryption.Self!.ExtractToElf();
        File.WriteAllBytes(tempFile, elfBytes.ToArray());
        
        sce.FreeMemory(elfBytes);
        
        return Task.CompletedTask;
    }
}