using Refresher.Core.Pipelines;

namespace Refresher.Core.Storage;

/// <summary>
/// Stores used inputs in pipelines for use in later sessions of Refresher.
/// </summary>
public static class PreviousInputStorage
{
    private const string ApplicationDirectory = "Refresher";
    private const string FileName = "inputs.ini";
    private static readonly string DirectoryPath;
    private static readonly string FilePath;
    
    public static readonly Dictionary<string, string> StoredInputs = [];
    
    static PreviousInputStorage()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        DirectoryPath = Path.Combine(appData, ApplicationDirectory);
        FilePath = Path.Combine(appData, ApplicationDirectory, FileName);
    }

    public static void Read()
    {
        if (!File.Exists(FilePath))
            return;
        
        using FileStream stream = File.OpenRead(FilePath);
        using StreamReader reader = new(stream);

        while (!reader.EndOfStream)
        {
            string? line = reader.ReadLine();
            if (line == null)
                break;
            
            if(string.IsNullOrWhiteSpace(line) || line.StartsWith(';') || !line.Contains('='))
                continue;

            int equals = line.IndexOf('=');

            string key = line[..equals];
            string value = line[(equals + 1)..];

            StoredInputs[key] = value;
        }
    }

    public static void ApplyFromPipeline(Pipeline pipeline)
    {
        foreach ((string key, string value) in pipeline.Inputs)
        {
            StoredInputs[key] = value;
        }
    }

    public static void Write()
    {
        Directory.CreateDirectory(DirectoryPath);
        
        using FileStream stream = File.OpenWrite(FilePath);
        using StreamWriter writer = new(stream);
        foreach ((string key, string value) in StoredInputs)
        {
            writer.Write(key);
            writer.Write('=');
            writer.WriteLine(value);
        }
        
        writer.Flush();
        stream.Flush();
    }
}