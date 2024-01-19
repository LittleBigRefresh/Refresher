using CommandLine;
using Eto.Forms;
using Refresher.CLI;
using Refresher.UI;

namespace Refresher;

#nullable disable

public class Program
{
    public static Application App;
    
    [STAThread]
    public static void Main(string[] args)
    {
        if (args.Length > 0)
        {
            Console.WriteLine("Launching in CLI mode");
            Parser.Default.ParseArguments<CommandLineOptions>(args)
                .WithParsed(CLI.CommandLine.Run);
        }
        else
        {
            Console.WriteLine("Launching in GUI mode");
            App = new Application();
            App.UnhandledException += (sender, eventArgs)
                => MessageBox.Show($"""
                                    There was an unhandled error in Refresher!
                                    *Please* screenshot this message box and send it to us over GitHub or Discord with details on what you were doing. This is likely a bug in Refresher.

                                    Exception details: {eventArgs.ExceptionObject}
                                    """,
                    "Critical Error");
            
            try
            {
                App.Run(new MainForm());
            }
            catch(Exception ex)
            {
                MessageBox.Show($"""
                                 There was an unhandled error in Refresher!
                                 *Please* screenshot this message box and send it to us over GitHub or Discord with details on what you were doing. This is likely a bug in Refresher.

                                 Exception details: {ex}
                                 """, "Critical Error");
            }
            App.Dispose();
        }
    }
}