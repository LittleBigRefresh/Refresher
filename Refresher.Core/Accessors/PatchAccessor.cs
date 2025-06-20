using System.Reflection;
using Refresher.Core.Pipelines;

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
    public abstract void CreateDirectory(string path);

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

    /// <summary>
    /// Creates a directory if it doesn't already exist.
    /// </summary>
    /// <param name="path">The path to the directory to create</param>
    /// <returns>True if the directory was created, false if not.</returns>
    public bool CreateDirectoryIfNotExists(string path)
    {
        if (this.DirectoryExists(path)) return false;

        this.CreateDirectory(path);
        return true;
    }

    public static bool Try(Step step, Action action)
    {
        try
        {
            action();
            return true;
        }
        catch (TargetInvocationException targetInvocationException)
        {
            CatchAccessorException(step, targetInvocationException.InnerException!);
            return false;
        }
        catch (Exception ex)
        {
            CatchAccessorException(step, ex);
            return false;
        }
    }
    
    public static async Task<bool> TryAsync(Step step, Func<Task> action)
    {
        try
        {
            await action();
            return true;
        }
        catch (TargetInvocationException targetInvocationException)
        {
            CatchAccessorException(step, targetInvocationException.InnerException!);
            return false;
        }
        catch (Exception ex)
        {
            CatchAccessorException(step, ex);
            return false;
        }
    }

    private static void CatchAccessorException(Step step, Exception ex)
    {
        step.Fail($"Something went wrong while accessing the filesystem: {ex.GetType().Name}: {ex.Message}");
    }
}