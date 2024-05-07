using FluentFTP;
using Refresher.Exceptions;

namespace Refresher.Accessors;

public class ConsolePatchAccessor : PatchAccessor, IDisposable
{
    private readonly FtpClient _client;
    private const string BasePath = "/dev_hdd0/";
    private readonly string _remoteIp;

    public readonly Lazy<byte[]?> IdpsFile;

    public ConsolePatchAccessor(string remoteIp)
    {
        this._remoteIp = remoteIp;
        
        this._client = new FtpClient(remoteIp, "anonymous", "");
        this._client.Config.LogToConsole = true;
        this._client.Config.ConnectTimeout = 5000;
        
        FtpProfile? profile = this._client.AutoConnect();
        if (profile == null) throw new FTPConnectionFailureException();
        
        this.IdpsFile = new Lazy<byte[]?>(this.GetIdps);
    }
    
    private byte[]? GetIdps()
    {
        Program.Log("Getting IDPS...", "IDPS");
        UriBuilder idpsPs3 = new("http", this._remoteIp, 80, "idps.ps3");
        UriBuilder idpsHex = new("http", this._remoteIp, 80, "dev_hdd0/idps.hex");
        UriBuilder idpsHexUsb = new("http", this._remoteIp, 80, "dev_usb000/idps.hex");
        
        HttpClient httpClient = new();
        
        HttpResponseMessage? response = this.IdpsRequestStep("Triggering generation of IDPS file", httpClient, idpsPs3.Uri);
        if (response == null) return null;

        response = this.IdpsRequestStep("Downloading IDPS hex (HDD)", httpClient, idpsHex.Uri);
        response ??= this.IdpsRequestStep("Downloading IDPS hex (USB)", httpClient, idpsHexUsb.Uri);
        
        //Return the IDPS key
        return response?.Content.ReadAsByteArrayAsync().Result;
    }
    
    private HttpResponseMessage? IdpsRequestStep(ReadOnlySpan<char> stepName, HttpClient client, Uri uri)
    {
        HttpResponseMessage response;
        
        Program.Log($"  {stepName} ({uri.AbsolutePath})", "IDPS");
        try
        {
            response = client.GetAsync(uri).Result;
        }
        catch (HttpRequestException e)
        {
            Program.Log($"Couldn't fetch the IDPS from the PS3 because we couldn't make the request: {e.Message}", "IDPS", BreadcrumbLevel.Error);
            return null;
        }
        catch (Exception e)
        {
            Program.Log($"Couldn't fetch the IDPS from the PS3 because of an unknown error: {e}", "IDPS", BreadcrumbLevel.Error);
            SentrySdk.CaptureException(e);
            return null;
        }
        Program.Log($"    {(int)response.StatusCode} {response.StatusCode} (success: {response.IsSuccessStatusCode})", "IDPS");
        
        if (!response.IsSuccessStatusCode)
        {
            Program.Log("Couldn't fetch the IDPS from the PS3 because of a bad status code.", "IDPS", BreadcrumbLevel.Error);
            Program.Log(response.Content.ReadAsStringAsync().Result, "IDPS", BreadcrumbLevel.Debug);
            return null;
        }
        
        return response;
    }

    private static string GetPath(string path)
    {
        if (Path.IsPathRooted(path)) return path;
        return BasePath + path;
    }

    public override bool DirectoryExists(string path) => this._client.DirectoryExists(GetPath(path));

    public override bool FileExists(string path) => this._client.FileExists(GetPath(path));

    public override IEnumerable<string> GetDirectoriesInDirectory(string path) =>
        this._client.GetListing(GetPath(path))
            .Where(l => l.Type == FtpObjectType.Directory)
            .Select(l => l.FullName);

    public override IEnumerable<string> GetFilesInDirectory(string path) =>
        this._client.GetListing(GetPath(path))
            .Where(l => l.Type == FtpObjectType.File)
            .Select(l => l.FullName);

    public override Stream OpenRead(string path)
    {
        MemoryStream ms = new();
        this._client.DownloadStream(ms, GetPath(path));
        ms.Seek(0, SeekOrigin.Begin);
        return ms;
        
        // technically we can use a stream directly but this uses a lot more requests
        // and webman doesnt like that, it tends to just slow down to a crawl after a bunch of them :(
        // return this._client.OpenRead(GetPath(path));
    }

    public override Stream OpenWrite(string path) => this._client.OpenWrite(GetPath(path), FtpDataType.Binary, false);

    public override void RemoveFile(string path) => this._client.DeleteFile(GetPath(path));

    public void Dispose()
    {
        this._client.Dispose();
        GC.SuppressFinalize(this);
    }
}