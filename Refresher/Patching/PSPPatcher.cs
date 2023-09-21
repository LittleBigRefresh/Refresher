using Refresher.Patching.PSP;
using Refresher.Verification;

namespace Refresher.Patching;

public class PSPPatcher : IPatcher
{
    public string? PSPDrivePath;
    
    public List<Message> Verify(string url, bool patchDigest)
    {
        List<Message> messages = new();

        if (!Uri.TryCreate(url, UriKind.Absolute, out _))
            messages.Add(new Message(MessageLevel.Error, "URL failed to parse!"));

        if (string.IsNullOrEmpty(this.PSPDrivePath))
            messages.Add(new Message(MessageLevel.Error, "Invalid PSP Drive path!"));
        
        return messages;
    }

    public void Patch(string url, bool patchDigest)
    {
        Uri uri = new(url);

        string domain = uri.Host;
        string format = $"{uri.Scheme}://%s:{uri.Port}{uri.AbsolutePath}%s";

        string pluginsDir = Path.Combine(this.PSPDrivePath!, "SEPLUGINS");

        //If the plugins directory does not exist
        if (!Directory.Exists(pluginsDir))
        {
            //Create it
            Directory.CreateDirectory(pluginsDir);
        }
        
        string domainPath = Path.Combine(pluginsDir, "Allefresher_domain.txt");
        string formatPath = Path.Combine(pluginsDir, "Allefresher_format.txt");
        
        //Delete the existing domain and format configuration files
        File.Delete(domainPath);
        File.Delete(formatPath);
        
        //Write the new domain and format configuration
        File.WriteAllText(domainPath, domain);
        File.WriteAllText(formatPath, format);

        //Match for all files called "game.txt" in the plugins directory
        //NOTE: we do this because the PSP filesystem is case insensitive, and the .NET STL is case sensitive on linux
        List<string> possibleMatches = Directory.EnumerateFiles(pluginsDir, "game.txt", new EnumerationOptions
        {
            MatchCasing = MatchCasing.CaseInsensitive,
        }).ToList();

        FileStream gamePluginsFileStream;

        const string allefresherPath = "ms0:/SEPLUGINS/Allefresher.prx";
        
        List<PSPPluginListEntry> entries;
        if (possibleMatches.Any())
        {
            //Open the first match
            FileStream stream = File.OpenRead(possibleMatches[0]);

            //Read out the matches
            entries = PSPPluginListParser.Parse(new StreamReader(stream));

            //If Allefresher is not in the list,
            if (!entries.Any(entry => entry.Path.Contains("Allefresher.prx", StringComparison.InvariantCultureIgnoreCase)))
            {
                //Add Allefresher to the game plugin list
                entries.Add(new PSPPluginListEntry(allefresherPath, 1));
            }

            //Dispose the read stream
            stream.Dispose();

            //Open a new write stream to the game.txt file
            gamePluginsFileStream = File.Open(possibleMatches[0], FileMode.Truncate);
        }
        else
        {
            //Create a new list, with the only entry being Allefresher
            entries = new List<PSPPluginListEntry>
            {
                new(allefresherPath, 1),
            };

            //Create a new game.txt file
            gamePluginsFileStream = File.Open(Path.Combine(pluginsDir, "game.txt"), FileMode.CreateNew);
        }
        
        //Write the plugin list to the file
        PSPPluginListParser.Write(entries, new StreamWriter(gamePluginsFileStream));

        //Flush and dispose the stream
        gamePluginsFileStream.Flush();
        gamePluginsFileStream.Dispose();
    }
}