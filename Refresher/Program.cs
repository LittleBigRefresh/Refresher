using System.Diagnostics.CodeAnalysis;
using System.IO.MemoryMappedFiles;
using CommandLine;
using Refresher.Patching;
using Refresher.Verification;

namespace Refresher;

#nullable disable

public class Program
{
    public static void Main(string[] args)
    {
        Parser.Default.ParseArguments<CommandlineOptions>(args)
            .WithParsed(CommandLine);
    }

    private static void CommandLine(CommandlineOptions options)
    {
        //Deletes the temporary file, if it exists
        void DeleteTempFile(string s)
        {
            try
            {
                if (s != null)
                    File.Delete(s);
            }
            catch
            {
                // ignored
            }
        }

        //If the input file does not exist, exit
        if (!File.Exists(options.InputFile))
        {
            Console.WriteLine("Input file does not exist.");
            Environment.Exit(1);
            return;
        }

        string tempFile = null;
        try
        {
            //Create a temp file to store the EBOOT as we work on it
            tempFile = Path.GetTempFileName();

            //Copy the input file to the temp file
            File.Copy(options.InputFile, tempFile, true);
        }
        catch (Exception e)
        {
            Console.WriteLine("Could not create and copy to temporary file.\n" + e);

            DeleteTempFile(tempFile);

            Environment.Exit(1);
            return;
        }

        MemoryMappedFile mappedFile;
        try
        {
            mappedFile =
                MemoryMappedFile.CreateFromFile(tempFile, FileMode.Open, null, 0, MemoryMappedFileAccess.ReadWrite);
        }
        catch (Exception e)
        {
            Console.WriteLine("Could not read data from the input file.\n" + e);

            DeleteTempFile(tempFile);

            Environment.Exit(1);
            return;
        }

        //Create a new patcher with the temp file stream
        Patcher patcher = new(mappedFile.CreateViewStream());
        List<Message> messages = patcher.Verify(options.ServerUrl).ToList();

        //Write the messages to the console
        foreach (Message message in messages) Console.WriteLine($"{message.Level}: {message.Content}");

        //If there are any errors, exit
        if (messages.Any(m => m.Level == MessageLevel.Error))
        {
            Console.WriteLine("\nThe patching operation cannot continue due to errors while verifying. Stopping.");

            mappedFile.Dispose();

            DeleteTempFile(tempFile);

            Environment.Exit(1);
            return;
        }

        try
        {
            //Patch the file
            patcher.PatchUrl(options.ServerUrl);

            //TODO: warn the user if they are overwriting the file
            File.Move(tempFile, options.OutputFile, true);
        }
        catch (Exception e)
        {
            Console.WriteLine("Could not complete patch stopping.\n" + e);

            mappedFile.Dispose();

            DeleteTempFile(tempFile);

            Environment.Exit(1);
            return;
        }

        DeleteTempFile(tempFile);
    }

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
}