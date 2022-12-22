using System.Diagnostics.Contracts;
using System.Text;
using Refresher.Verification;

namespace Refresher.Patching;

public class Patcher
{
    public byte[] Data { get; }

    public Patcher(byte[] data)
    {
        this.Data = data;
        string dataStr = Encoding.ASCII.GetString(data);

        this._httpUrlInfo = new Lazy<PatchTargetInfo?>(() => FindUrl(dataStr, HttpUrls));
        this._httpsUrlInfo = new Lazy<PatchTargetInfo?>(() => FindUrl(dataStr, HttpsUrls));
    }

    private static readonly string[] HttpUrls =
    {
        "http://littlebigplanetps3.online.scee.com:10060/LITTLEBIGPLANETPS3_XML",
    };
    
    private static readonly string[] HttpsUrls =
    {
        "https://littlebigplanetps3.online.scee.com:10061/LITTLEBIGPLANETPS3_XML",
    };
    
    private readonly Lazy<PatchTargetInfo?> _httpUrlInfo;
    private readonly Lazy<PatchTargetInfo?> _httpsUrlInfo;

    private static PatchTargetInfo? FindUrl(string data, IEnumerable<string> list)
    {
        foreach (string url in list)
        {
            int offset = data.IndexOf(url, StringComparison.Ordinal);
            if(offset < 1) continue;

            return new PatchTargetInfo
            {
                Offset = offset,
                Length = url.Length,
            };
        }

        return null;
    }

    /// <summary>
    /// Checks the contents of the EBOOT to verify that it is patchable.
    /// </summary>
    /// <returns>A list of issues and notes about the EBOOT.</returns>
    [Pure]
    public IEnumerable<Message> Verify(string url)
    {
        // TODO: check if this is an ELF, correct architecture, if url is correct length, etc.
        List<Message> messages = new();
        
        // Check url
        if(url.EndsWith('/'))
            messages.Add(new Message(MessageLevel.Error, "URI cannot end with a trailing slash, invalid requests will be sent to the server"));

        if(!Uri.TryCreate(url, UriKind.Absolute, out Uri _))
            messages.Add(new Message(MessageLevel.Error, "URI is not valid"));

        // Check if URLs exist in eboot
        if (!this._httpUrlInfo.Value.HasValue)
            messages.Add(new Message(MessageLevel.Warning, "Could not find the HTTP url in the EBOOT."));
        
        if (!this._httpsUrlInfo.Value.HasValue)
            messages.Add(new Message(MessageLevel.Warning, "Could not find the HTTPS url in the EBOOT."));

        // Its okay(?) to only be able to patch one URL, but if there are none then it's a problem
        if (!this._httpUrlInfo.Value.HasValue && !this._httpsUrlInfo.Value.HasValue)
            messages.Add(new Message(MessageLevel.Error,
                "Could not find any urls in the EBOOT. Nothing can be changed."));

        return messages;
    }

    public void PatchUrl(string url)
    {
        using MemoryStream ms = new(this.Data);
        using BinaryWriter writer = new(ms);

        // Using BinaryWriter.Write writes a length-oriented string, get this for null terminated instead
        byte[] urlAsBytes = Encoding.Default.GetBytes(url);

        if (this._httpUrlInfo.Value.HasValue) PatchUrlFromInfo(writer, this._httpUrlInfo.Value.Value, urlAsBytes);
        if (this._httpsUrlInfo.Value.HasValue) PatchUrlFromInfo(writer, this._httpsUrlInfo.Value.Value, urlAsBytes);
    }

    private static void PatchUrlFromInfo(BinaryWriter writer, PatchTargetInfo info, byte[] url)
    {
        writer.BaseStream.Position = info.Offset;
        writer.Write(url);
        
        for (int i = 0; i < info.Length - url.Length; i++)
        {
            writer.Write('\0');
        }
    }
}