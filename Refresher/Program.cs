using CommandLine;
using Eto.Forms;
using Refresher.CLI;
using Refresher.UI;

namespace Refresher;

#nullable disable

public class Program {
    public static Application App;
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
            App.Run(new PatchForm());
            App.Dispose();
        }
    }
}