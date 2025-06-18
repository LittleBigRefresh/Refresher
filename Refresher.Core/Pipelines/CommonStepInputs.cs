namespace Refresher.Core.Pipelines;

public static class CommonStepInputs
{
    internal static readonly StepInput TitleId = new("title-id", "Game", StepInputType.Game)
    {
        Placeholder = "NPUA80662",
    };
    
    public static readonly StepInput ServerUrl = new("url", "Server URL", StepInputType.Url)
    {
        Placeholder = "https://lbp.littlebigrefresh.com",
    };
    
    internal static readonly StepInput RPCS3Folder = new("hdd0-path", "RPCS3 dev_hdd0 folder", StepInputType.Directory)
    {
        // provide an example to Windows users.
        // don't bother with other platforms because they should be automatic
        Placeholder = @$"C:\Users\{Environment.UserName}\RPCS3\dev_hdd0",
        DetermineDefaultValue = DetermineDefaultRpcs3Path,
        ShouldCauseGameDownloadWhenChanged = true,
    };
    
    internal static readonly StepInput ConsoleIP = new("ip", "Console IP", StepInputType.ConsoleIp)
    {
        Placeholder = "192.168.1.123",
    };
    
    internal static readonly StepInput LobbyPassword = new("lobby-password", "Lobby Password")
    {
        Placeholder = "(leave empty to randomize)",
    };
    
    internal static readonly StepInput ElfInput = new("elf-input", "Input .ELF", StepInputType.OpenFile)
    {
        Placeholder = @"C:\path\to\EBOOT.elf",
    };
    
    internal static readonly StepInput ElfOutput = new("elf-output", "Output .ELF", StepInputType.SaveFile)
    {
        Placeholder = @"C:\path\to\EBOOT.elf",
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