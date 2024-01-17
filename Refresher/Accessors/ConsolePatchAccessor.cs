using System.Net;
using FluentFTP;

namespace Refresher.Accessors;

public class ConsolePatchAccessor : PatchAccessor, IDisposable
{
    private readonly FtpClient _client;
    private const string BasePath = "/dev_hdd0/";

    public ConsolePatchAccessor(string remoteIp)
    {
        this._client = new FtpClient(remoteIp, "anonymous", "");
        this._client.Config.LogToConsole = true;
        this._client.Config.ConnectTimeout = 5000;
        this._client.AutoConnect();
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