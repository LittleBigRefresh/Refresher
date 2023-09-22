namespace Refresher.Patching.PSP;

public class PSPPluginListEntry
{
    public string Path;
    public int? Type;

    public PSPPluginListEntry(string path, int? type = null)
    {
        this.Path = path;
        this.Type = type;
    }
}