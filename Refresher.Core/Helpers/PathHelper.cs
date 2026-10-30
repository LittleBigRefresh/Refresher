namespace Refresher.Core.Helpers;

public static class PathHelper
{
    public static string? GetDefaultRPCS3DevHdd0Path()
    {
        // RPCS3 builds for Windows are portable, so we can't determine this automatically
        if (OperatingSystem.IsWindows())
            return null;

        // ~/.config/rpcs3/dev_hdd0
        // TODO read RPCS3 config to get the user's current virtual dev_hdd0 path, incase they changed it
        string folder = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "rpcs3", "dev_hdd0");

        if (Directory.Exists(folder))
            return folder;
        
        return null;
    }
}