namespace Refresher.Core.Patching;

public class EncryptionDetails
{
    public string? LicenseDirectory { get; internal set; }
    public string? DownloadedActDatPath { get; internal set; }
    public byte[]? ConsoleIdps { get; internal set; }
}