namespace Refresher.Core.Accessors;

public class EmulatorPatchAccessor : PatchAccessor
{
    public EmulatorPatchAccessor(string basePath)
    {
        this.BasePath = basePath;
    }

    private string BasePath { get; }

    private string GetPath(string path)
    {
        if (Path.IsPathRooted(path)) return path;
        return Path.Join(this.BasePath, path);
    }

    public override bool Available => true;
    public override bool DirectoryExists(string path) => Directory.Exists(this.GetPath(path));
    public override bool FileExists(string path) => File.Exists(this.GetPath(path));
    
    public override IEnumerable<string> GetDirectoriesInDirectory(string path) => Directory.GetDirectories(this.GetPath(path));
    public override IEnumerable<string> GetFilesInDirectory(string path) => Directory.GetFiles(this.GetPath(path));
    
    public override Stream OpenRead(string path) => File.OpenRead(this.GetPath(path));
    public override Stream OpenWrite(string path) => File.OpenWrite(this.GetPath(path));
    public override void RemoveFile(string path) => File.Delete(this.GetPath(path));
    public override void CreateDirectory(string path) => Directory.CreateDirectory(this.GetPath(path));

    public override void CopyFile(string inPath, string outPath)
        => File.Copy(this.GetPath(inPath), this.GetPath(outPath));
}