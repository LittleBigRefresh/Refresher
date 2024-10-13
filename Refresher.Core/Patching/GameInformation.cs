namespace Refresher.Core.Patching;

public class GameInformation
{
    public string TitleId { get; set; } = null!;

    public string? Name { get; set; }
    public string? ContentId { get; set; }
    public string? Version { get; set; }
    
    public string? DownloadedEbootPath { get; set; }
    public string? DecryptedEbootPath { get; internal set; }

    public override string ToString()
    {
        return $"[{this.TitleId}] name: {this.Name}, contentId: {this.ContentId}, version: {this.Version}";
    }
}