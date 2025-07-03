using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Security.Principal;
using Eto.Forms;
using Microsoft.Win32;
using Velopack.Locators;

namespace Refresher;

public static class UrlAssociationHandler
{
    private const string Protocol = "refresher";
    private static readonly string ApplicationPath = string.Empty;

    static UrlAssociationHandler()
    {
        if (OperatingSystem.IsWindows())
        {
            ApplicationPath = Process.GetCurrentProcess().MainModule?.FileName ?? throw new UnreachableException();
            // ApplicationPath = Path.Combine(Environment.GetEnvironmentVariable("LocalAppData") ?? throw new UnreachableException(), "Refresher", "current", "Refresher.exe");
        }
    }

    public static bool IsArgsUrl(string[] args)
    {
        if (args.Length == 0)
            return false;
        return args[0].StartsWith(Protocol);
    }

    public static bool IsArgsTryingToRegisterAssociations(string[] args)
    {
        if (args.Length == 0)
            return false;

        return args[0] == "--register-associations";
    }
    
    [SupportedOSPlatform("windows")]
    public static bool IsAdministrator()
    {
        using WindowsIdentity identity = WindowsIdentity.GetCurrent();

        WindowsPrincipal principal = new(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    /// <summary>
    /// Registers the proper registry associations
    /// </summary>
    /// <returns>True if the association was created or already present, false if we couldn't create it</returns>
    [SupportedOSPlatform("windows")]
    public static bool RegisterAssociationIfNotPresent()
    {
        if (!OperatingSystem.IsWindows())
            return false;
        
        if (VelopackLocator.Current.IsPortable)
            return false;

        RegistryKey? key = Registry.ClassesRoot.OpenSubKey(Protocol);
        if (key != null)
            return true;

        if (!IsAdministrator())
        {
            const string msg =
                "Refresher supports URL associations for clickable patch links. Would you like to enable them?\n\n" +
                "This operation runs once and requires administrator privileges.";
            
            DialogResult result = MessageBox.Show(msg, "Refresher", MessageBoxButtons.YesNo, MessageBoxType.Question);
            if (result == DialogResult.No)
                return false;

            bool registered = ElevateToRegisterAssociations();
            if (registered)
            {
                MessageBox.Show("Successfully registered!", "Refresher", MessageBoxButtons.OK);
            }
            else
            {
                MessageBox.Show("Failed to register the URL associations.", "Refresher", MessageBoxButtons.OK, MessageBoxType.Warning);
            }
            
            return true;
        }

        key = Registry.ClassesRoot.CreateSubKey(Protocol);
        key.SetValue("", "URL:Refresher");
        key.SetValue("URL Protocol", "");

        RegistryKey subKey = key.CreateSubKey(@"Shell\Open\Command");
        subKey.SetValue("", $"{ApplicationPath} %1");
        return true;
    }

    [SupportedOSPlatform("windows")]
    private static bool ElevateToRegisterAssociations()
    {
        ProcessStartInfo info = new(Process.GetCurrentProcess().MainModule?.FileName ?? throw new UnreachableException())
            {
                UseShellExecute = true,
                Verb = "runas",
                Arguments = "--register-associations",
            };

        Process? process;

        try
        {
            process = Process.Start(info);
        }
        // The operation was canceled by the user.
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            return false;
        }
        
        if (process == null)
            throw new Exception("Process didn't start");

        process.WaitForExit();
        if (process.ExitCode != 0)
            throw new Exception("Process exited with code " + process.ExitCode);

        return true;
    }
}