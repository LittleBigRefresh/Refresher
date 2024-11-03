using System.IO.MemoryMappedFiles;
using Refresher.Core;
using Refresher.Core.Patching;
using Refresher.Core.Verification;

namespace Refresher.CLI;

public class CommandLine
{
    public static void Run(CommandLineOptions options)
    {
        //Deletes the temporary file, if it exists
        void DeleteTempFile(string? s)
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
            State.Logger.LogCritical(LogType.CLI, "Input file does not exist.");
            Environment.Exit(1);
            return;
        }

        string? tempFile = null;
        try
        {
            //Create a temp file to store the EBOOT as we work on it
            tempFile = Path.GetTempFileName();

            //Copy the input file to the temp file
            File.Copy(options.InputFile, tempFile, true);
        }
        catch (Exception e)
        {
            State.Logger.LogCritical(LogType.CLI, "Could not create and copy to temporary file.\n" + e);

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
            State.Logger.LogCritical(LogType.CLI, "Could not read data from the input file.\n" + e);
            
            DeleteTempFile(tempFile);
            Environment.Exit(1);
            return;
        }

        //Create a new patcher with the temp file stream
        EbootPatcher ebootPatcher = new(mappedFile.CreateViewStream());
        List<Message> messages = ebootPatcher.Verify(options.ServerUrl, options.Digest ?? false).ToList();

        //Write the messages to the console
        foreach (Message message in messages) State.Logger.LogInfo(Verify, $"{message.Level}: {message.Content}");

        //If there are any errors, exit
        if (messages.Any(m => m.Level == MessageLevel.Error))
        {
            State.Logger.LogCritical(Verify, "\nThe patching operation cannot continue due to errors while verifying. Stopping.");

            mappedFile.Dispose();
            DeleteTempFile(tempFile);
            Environment.Exit(1);
            return;
        }
        
        if (messages.Any(m => m.Level == MessageLevel.Warning))
        {
            Console.Write("\nThere were warnings while verifying. Would you like to continue? [Y/n] ");
            ConsoleKeyInfo key = Console.ReadKey();
            Console.Write('\n');

            if (key.KeyChar == 'n')
            {
                State.Logger.LogCritical(Verify, "Patching cancelled due to warnings.");

                mappedFile.Dispose();
                DeleteTempFile(tempFile);
                Environment.Exit(1);
                return;
            }
        }
        
        State.Logger.LogInfo(LogType.CLI, "Patching...");

        try
        {
            //Patch the file
            ebootPatcher.Patch(options.ServerUrl, options.Digest ?? false);

            //TODO: warn the user if they are overwriting the file
            File.Move(tempFile, options.OutputFile, true);
        }
        catch (Exception e)
        {
            State.Logger.LogCritical(LogType.CLI, "Could not complete patch, stopping.\n" + e);

            mappedFile.Dispose();
            DeleteTempFile(tempFile);
            Environment.Exit(1);
            return;
        }
        
        DeleteTempFile(tempFile);
        State.Logger.LogInfo(LogType.CLI, "Successfully patched EBOOT!");
    }
}