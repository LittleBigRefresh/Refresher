namespace Refresher.Core.Patching.PSP;

public static class PSPPluginListParser
{
    public static List<PSPPluginListEntry> Parse(TextReader reader)
    {
        List<PSPPluginListEntry> list = new();

        while (reader.ReadLine() is { } line)
        {
            //Skip blank lines
            if (string.IsNullOrWhiteSpace(line)) continue;
            
            string[] parts = line.Split(" ");

            PSPPluginListEntry entry = new(parts[0]);
            
            if (parts.Length > 1)
            {
                entry.Type = int.Parse(parts[1]);
            }
            
            list.Add(entry);
        }
        
        return list;
    }

    public static void Write(List<PSPPluginListEntry> list, TextWriter writer)
    {
        foreach (PSPPluginListEntry entry in list)
        {
            writer.Write(entry.Path);
            if (entry.Type.HasValue)
            {
                writer.Write(' ');
                writer.Write(entry.Type.Value);
            }
            writer.Write('\n');
        }
        
        writer.Flush();
    }
}