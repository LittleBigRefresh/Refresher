using CommandLine;
using Eto.Forms;
using Refresher.CLI;
using Refresher.UI;

namespace Refresher;

#nullable disable

public class Program
{
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
            new Application().Run(new PatchForm());
        }
    }
}