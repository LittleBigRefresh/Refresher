using Refresher.Patching;
using Refresher.Verification;

namespace Refresher.CLI;

public class CommandLine
{
    public static void Run(CommandLineOptions options)
    {
        byte[] data;
        try
        {
            data = File.ReadAllBytes(options.InputFile);
        }
        catch (Exception e)
        {
            Console.WriteLine("Could not read data from the input file.\n" + e);
            Environment.Exit(1);
            return;
        }

        Patcher patcher = new(data);
        List<Message> messages = patcher.Verify(options.ServerUrl).ToList();
        
        foreach (Message message in messages)
        {
            Console.WriteLine(message.ToString());
        }

        if (messages.Any(m => m.Level == MessageLevel.Error))
        {
            Console.WriteLine("\nThe patching operation cannot continue due to errors while verifying. Stopping.");
            Environment.Exit(1);
            return;
        }
        
        patcher.PatchUrl(options.ServerUrl);
        
        File.WriteAllBytes(options.OutputFile, patcher.Data);
    }
}