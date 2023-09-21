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
        Uri uri = new Uri(url);

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
        
        File.Delete(domainPath);
        File.Delete(formatPath);
        
        File.WriteAllText(domainPath, domain);
        File.WriteAllText(formatPath, format);
    }
}