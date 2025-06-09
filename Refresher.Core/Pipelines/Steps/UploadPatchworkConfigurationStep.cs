using System.Text;
using Refresher.Core.Accessors;

namespace Refresher.Core.Pipelines.Steps;

public class UploadPatchworkConfigurationStep : Step
{
    public UploadPatchworkConfigurationStep(Pipeline pipeline) : base(pipeline)
    {}
    
    public override List<StepInput> Inputs =>
    [
        CommonStepInputs.ServerUrl,
        CommonStepInputs.LobbyPassword,
    ];

    public override float Progress { get; protected set; }
    public override async Task ExecuteAsync(CancellationToken ct = default)
    {
        this.Pipeline.Accessor!.CreateDirectoryIfNotExists("tmp/");
        
        string? lobbyPassword = this.Pipeline.Inputs["lobby-password"];
        if (string.IsNullOrWhiteSpace(lobbyPassword))
            lobbyPassword = null;
        
        await this.UploadConfigValue("patchwork_lobby_password.txt", lobbyPassword, ct);
        this.Progress = 0.33f;
        
        await this.UploadConfigValue("patchwork_url.txt", this.Pipeline.Inputs["url"], ct);
        this.Progress = 0.66f;
        
        string? customDigest = this.Pipeline.AutoDiscover?.UsesCustomDigestKey ?? false
            ? "CustomServerDigest"
            : null;
        
        await this.UploadConfigValue("patchwork_digest.txt", customDigest, ct);
        
        this.Progress = 1f;
    }

    private async Task UploadConfigValue(string filename, string? value, CancellationToken ct = default)
    {
        await PatchAccessor.TryAsync(async () =>
        {
            string configPath = "tmp/" + filename;
            
            if (this.Pipeline.Accessor!.FileExists(configPath))
                this.Pipeline.Accessor.RemoveFile(configPath);

            if (value == null)
                return;

            await using Stream writeStream = this.Pipeline.Accessor.OpenWrite(configPath);
            await using Stream readStream = new MemoryStream(Encoding.UTF8.GetBytes(value));

            await readStream.CopyToAsync(writeStream, ct);
            await writeStream.FlushAsync(ct);
        });
    }
}