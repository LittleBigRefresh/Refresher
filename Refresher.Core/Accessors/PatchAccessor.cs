using System.Reflection;

namespace Refresher.Core.Accessors;

public abstract class PatchAccessor
{
    public abstract bool Available { get; }
    
    public abstract bool DirectoryExists(string path);
    public abstract bool FileExists(string path);
    public abstract IEnumerable<string> GetDirectoriesInDirectory(string path);
    public abstract IEnumerable<string> GetFilesInDirectory(string path);
    public abstract Stream OpenRead(string path);
    public abstract Stream OpenWrite(string path);
    public abstract void RemoveFile(string path);

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

    public virtual void CopyFile(string inPath, string outPath)
    {
        using Stream inStream = this.OpenRead(inPath);
        using Stream outStream = this.OpenWrite(outPath);
        
        inStream.CopyTo(outStream);
    }

    public static void Try(Action action)
    {
        try
        {
            action();
        }
        catch (TargetInvocationException targetInvocationException)
        {
            CatchAccessorException(targetInvocationException.InnerException!);
            throw;
        }
        catch (Exception ex)
        {
            CatchAccessorException(ex);
            throw;
        }
    }
    
    public static async Task TryAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (TargetInvocationException targetInvocationException)
        {
            CatchAccessorException(targetInvocationException.InnerException!);
            throw;
        }
        catch (Exception ex)
        {
            CatchAccessorException(ex);
            throw;
        }
    }

    private static void CatchAccessorException(Exception ex)
    {
        State.Logger.LogError(Accessor, $"Something went wrong while accessing the filesystem: {ex.GetType().Name}: {ex.Message}");
    }
}