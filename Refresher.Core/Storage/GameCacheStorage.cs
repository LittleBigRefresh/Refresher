namespace Refresher.Core.Storage;

public static class GameCacheStorage
{
    private const string ApplicationDirectory = "Refresher";
    private const string CacheDirectory = "cache";
    private static readonly string FullCacheDirectory;

    static GameCacheStorage()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        FullCacheDirectory = Path.Combine(appData, ApplicationDirectory, CacheDirectory);

        try
        {
            Directory.CreateDirectory(FullCacheDirectory);
        }
        catch (IOException e)
        {
            // FIXME: no native MessageBox in Refresher.Core
            // MessageBox.Show($"Couldn't create the directory for the games cache: {e.Message}\n" +
            //                 $"This error is rare and I don't know how to cleanly handle this scenario so Refresher is just gonna exit.");
            Environment.Exit(1); // shrug
        }
    }

    private static string GetIconPath(string titleId) => Path.Combine(FullCacheDirectory, $"{titleId}.png");

    public static bool IconExistsInCache(string titleId)
        => File.Exists(GetIconPath(titleId));

    public static FileStream GetIconFromCache(string titleId)
        => File.OpenRead(GetIconPath(titleId));

    public static void WriteIconToCache(string titleId, Stream stream)
    {
        using FileStream writeStream = File.OpenWrite(GetIconPath(titleId));
        stream.CopyTo(writeStream);
        stream.Seek(0, SeekOrigin.Begin);
    }
}