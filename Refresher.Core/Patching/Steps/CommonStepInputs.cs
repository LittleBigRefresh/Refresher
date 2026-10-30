using Refresher.Core.Helpers;

namespace Refresher.Core.Patching.Steps;

public static class CommonStepInputs
{
    internal static readonly StepInput TitleId = new("title-id", "Game", StepInputType.Game)
    {
        Placeholder = "NPUA80662",
        Required = true,
    };
    
    public static readonly StepInput ServerUrl = new("url", "Server URL", StepInputType.Url)
    {
        Placeholder = "http://lbp.lbpbonsai.com/lbp",
        Required = true,
    };
    
    internal static readonly StepInput RPCS3Folder = new("hdd0-path", "RPCS3 dev_hdd0 folder", StepInputType.Directory)
    {
        // provide an example to Windows users.
        // don't bother with other platforms because they should be automatic
        Placeholder = @$"C:\Users\{Environment.UserName}\RPCS3\dev_hdd0",
        DetermineDefaultValue = PathHelper.GetDefaultRPCS3DevHdd0Path,
        ShouldCauseGameDownloadWhenChanged = true,
        Required = true,
    };
    
    internal static readonly StepInput ConsoleIP = new("ip", "Console IP", StepInputType.ConsoleIp)
    {
        Placeholder = "192.168.1.123",
        Required = true,
    };
    
    public static readonly StepInput LobbyPassword = new("lobby-password", "Join Key")
    {
        Placeholder = "(leave empty to randomize)",
    };
    
    internal static readonly StepInput ElfInput = new("elf-input", "Input .ELF", StepInputType.OpenFile)
    {
        Placeholder = @"C:\path\to\EBOOT.elf",
        Required = true,
    };
    
    internal static readonly StepInput ElfOutput = new("elf-output", "Output .ELF", StepInputType.SaveFile)
    {
        Placeholder = @"C:\path\to\EBOOT.elf",
        Required = true,
    };
}