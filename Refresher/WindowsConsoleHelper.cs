using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Refresher;

internal static class WindowsConsoleHelper
{
    internal static bool OpenedConsole { get; private set; }
    
    [DllImport("kernel32.dll", EntryPoint = "AllocConsole")]
    [SupportedOSPlatform("windows")]
    private static extern int AllocConsole();

    [SupportedOSPlatform("windows")]
    public static void AllocateConsole()
    {
        int res = AllocConsole();
        Debug.WriteLine($"{nameof(AllocConsole)} result: {res}");

        OpenedConsole = true;
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Hello! This is the secret debugging console for Refresher.");
        Console.WriteLine("This helps the person supporting you collect more information where the normal logs might not help.");
        Console.WriteLine();
        Console.WriteLine("Please use Refresher using the same inputs as you just did.");
        Console.WriteLine("When done, please copy paste the entire contents of this window below this line:");
        Console.BackgroundColor = ConsoleColor.White;
        Console.ForegroundColor = ConsoleColor.Black;
        Console.WriteLine("----------START COPY HERE----------");
        Console.ResetColor();
    }

    public static void ShowEndBlurb()
    {
        Console.BackgroundColor = ConsoleColor.White;
        Console.ForegroundColor = ConsoleColor.Black;
        Console.WriteLine("-----------END COPY HERE-----------");
        Console.ResetColor();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Please click and drag the text above until you see the 'START COPY HERE' line, and then send that to whoever's providing you support.");
        Console.WriteLine("Afterwards you can simply close this window, or press any key to exit.");
        Console.ReadKey();
    }
}