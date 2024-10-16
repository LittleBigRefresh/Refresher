namespace Refresher.Core.Pipelines;

internal static class CommonStepInputs
{
    internal static readonly StepInput TitleId = new("title-id", "Game", StepInputType.Game)
    {
        Placeholder = "NPUA80662",
    };
    
    internal static readonly StepInput ServerUrl = new("url", "Server URL")
    {
        Placeholder = "https://lbp.littlebigrefresh.com",
    };
    
    internal static readonly StepInput RPCS3Folder = new("hdd0-path", "RPCS3 dev_hdd0 folder", StepInputType.Directory)
    {
        // provide an example to Windows users.
        // don't bother with other platforms because they should be automatic
        Placeholder = @$"C:\Users\{Environment.UserName}\RPCS3\dev_hdd0",
        DetermineDefaultValue = DetermineDefaultRpcs3Path,
    };
    
    // TODO: Cache the last used location for easier entry
    private static string? DetermineDefaultRpcs3Path()
    {
        // RPCS3 builds for Windows are portable, so we can't determine this automatically
        if (OperatingSystem.IsWindows())
            return null;

        // ~/.config/rpcs3/dev_hdd0
        string folder = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "rpcs3", "dev_hdd0");

        if (Directory.Exists(folder))
            return folder;
        
        return null;
    }
}