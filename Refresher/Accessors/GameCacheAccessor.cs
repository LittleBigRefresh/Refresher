namespace Refresher.Accessors;

public static class GameCacheAccessor
{
    private const string ApplicationDirectory = "Refresher";
    private const string CacheDirectory = "cache";
    private static readonly string FullCacheDirectory;

    static GameCacheAccessor()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        FullCacheDirectory = Path.Combine(appData, ApplicationDirectory, CacheDirectory);
        Directory.CreateDirectory(FullCacheDirectory);
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