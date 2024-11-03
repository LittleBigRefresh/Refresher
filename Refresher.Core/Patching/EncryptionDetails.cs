using LibSceSharp;

namespace Refresher.Core.Patching;

public class EncryptionDetails
{
    public LibSce? Sce { get; internal set; }
    public Self? Self { get; internal set; }
    
    public string? DownloadedActDatPath { get; internal set; }
    public string? DownloadedLicensePath { get; internal set; }
    public byte[]? ConsoleIdps { get; internal set; }
}