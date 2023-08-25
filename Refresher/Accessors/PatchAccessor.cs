namespace Refresher.Accessors;

public abstract class PatchAccessor
{
    public abstract bool DirectoryExists(string path);
    public abstract bool FileExists(string path);
    public abstract IEnumerable<string> GetDirectoriesInDirectory(string path);
    public abstract IEnumerable<string> GetFilesInDirectory(string path);
    public abstract Stream OpenRead(string path);
    public abstract Stream OpenWrite(string path);

    public string DownloadFile(string path)
    {
        string outFile = Path.GetTempFileName();
        
        using FileStream outStream = File.OpenWrite(outFile);
        using Stream inStream = this.OpenRead(path);
        inStream.CopyTo(outStream);

        return outFile;
    }

    public void UploadFile(string inPath, string outPath)
    {
        using FileStream inStream = File.OpenRead(inPath);
        using Stream outStream = this.OpenWrite(outPath);
        
        inStream.CopyTo(outStream);
    }
}