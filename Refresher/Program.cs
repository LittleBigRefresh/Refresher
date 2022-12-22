using System.Diagnostics.CodeAnalysis;
using CommandLine;
using Refresher.Patching;
using Refresher.Verification;

namespace Refresher;

#nullable disable

public class Program
{
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
    private class CommandlineOptions
    {
        [Option('i', "input", Required = true, HelpText = "The input EBOOT.elf to patch")]
        public string InputFile { get; set; }
        
        [Option('u', "url", Required = true, HelpText = "The URL to patch to")]
        public string ServerUrl { get; set; }
        
        [Option('o', "output", Required = true, HelpText = "The output EBOOT.elf to save")]
        public string OutputFile { get; set; }
    }

    public static void Main(string[] args)
    {
        Parser.Default.ParseArguments<CommandlineOptions>(args)
            .WithParsed(CommandLine);
    }

    private static void CommandLine(CommandlineOptions options)
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
            Console.WriteLine($"{message.Level}: {message.Content}");
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