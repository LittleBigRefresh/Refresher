using System.Diagnostics.Contracts;
using System.Text;
using System.Text.RegularExpressions;
using Refresher.Verification;

namespace Refresher.Patching;

public partial class Patcher
{
    private readonly Lazy<List<PatchTargetInfo>> _targets;

    public Patcher(Stream stream)
    {
        if (!stream.CanRead || !stream.CanSeek || !stream.CanWrite)
            throw new ArgumentException("Stream must be readable, seekable and writable", nameof(stream));

        this.Stream = stream;

        this._targets = new Lazy<List<PatchTargetInfo>>(() => FindUrl(stream));
    }

    public Stream Stream { get; }

    [GeneratedRegex("^https?[^\\x00]\\/\\/([0-9a-zA-Z.:]*)\\/([0-9a-zA-Z_]*)$", RegexOptions.Compiled)]
    private static partial Regex UrlMatch();

    private static List<PatchTargetInfo> FindUrl(Stream file)
    {
        BinaryReader reader = new(file);

        //Get the length of the file (this operation can be costly, so lets cache it)
        long fileLength = file.Length;

        // Search for all instances of `http` in the binary
        ReadOnlySpan<byte> http = "http"u8;

        //Found positions of `http` in the binary
        List<long> foundPositions = new();

        //Get the length of the file (http.length also has a non-trivial cost, so we cache it)
        long length = fileLength - http.Length;

        //The buffer we read into
        byte[] buf = new byte[http.Length];
        long readPos = 0;
        //While we are not at the end of the file
        while (readPos < length)
        {
            //Set the position of the stream to the next one to check
            reader.BaseStream.Position = readPos;

            //Read into the buffer
            int read = file.Read(buf);
            //If we read less than the buffer size, we are at the end of the file
            if (read < http.Length)
                break;

            bool equal = true;

            //Check whether the original buffer is equal to the buffer we read
            //NOTE: theres a slightly faster way of doing this with SIMD, but this is fine for now
            //      see https://dev.to/antidisestablishmentarianism/c-simd-byte-array-compare-52p6
            for (int i = 0; i < buf.Length; i++)
            {
                if (buf[i] == http[i])
                    continue;

                equal = false;
                break;
            }

            //If they are equal, we found an instance of HTTP
            if (equal)
            {
                //Mark the position of the found instance
                foundPositions.Add(readPos);

                //Skip the length of the buffer
                readPos += buf.Length;
            }
            else
            {
                //Check the next position
                readPos++;
            }
        }

        List<PatchTargetInfo> found = new();

        bool tooLong = false;
        foreach (long foundPosition in foundPositions)
        {
            int len = 0;

            file.Position = foundPosition;

            //Find the first null byte
            while (reader.ReadByte() != 0)
            {
                len++;

                if (len > 100)
                {
                    tooLong = true;
                    break;
                }
            }

            if (tooLong)
            {
                tooLong = false;
                continue;
            }

            //Keep reading until we arent at a null byte
            while (reader.ReadByte() == 0) len++;

            file.Position = foundPosition;

            //`len` at this point is the amount of bytes that are actually available to repurpose
            //This includes all extra null bytes except for the last one

            byte[] match = new byte[len];
            if (file.Read(match) < len) continue;
            string str = Encoding.UTF8.GetString(match).TrimEnd('\0');

            Regex regex = UrlMatch();
            MatchCollection matches = regex.Matches(str);

            if (matches.Count != 0)
                found.Add(new PatchTargetInfo
                {
                    Length = len,
                    Offset = foundPosition,
                });
        }

        return found;
    }

    /// <summary>
    ///     Checks the contents of the EBOOT to verify that it is patchable.
    /// </summary>
    /// <returns>A list of issues and notes about the EBOOT.</returns>
    [Pure]
    public List<Message> Verify(string url)
    {
        // TODO: check if this is an ELF, correct architecture, if url is correct length, etc.
        List<Message> messages = new();
        
        messages.Add(new Message(MessageLevel.Warning, "im stuff"));
        return messages;

        // Check url
        if (url.EndsWith('/'))
            messages.Add(new Message(MessageLevel.Error,
                "URI cannot end with a trailing slash, invalid requests will be sent to the server"));

        //Try to create an absolute URI, if it fails, its not a valid URI
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri _))
            messages.Add(new Message(MessageLevel.Error, "URI is not valid"));

        // If there are no targets, we cant patch
        if (this._targets.Value.Count == 0)
            messages.Add(new Message(MessageLevel.Error,
                "Could not find any urls in the EBOOT. Nothing can be changed."));

        if (this._targets.Value.Any(x => x.Length < url.Length))
            messages.Add(new Message(MessageLevel.Error,
                "The URL is too short to fit in the EBOOT. Please use a shorter URL."));
        
        return messages;
    }

    public void PatchUrl(string url)
    {
        using BinaryWriter writer = new(this.Stream);

        // Get a null-terminated byte sequence of the URL, as BinaryWriter.Write(string) writes a length-prepended string,
        byte[] urlAsBytes = Encoding.UTF8.GetBytes(url);

        if (this._targets.Value.Count != 0) PatchUrlFromInfoList(writer, this._targets.Value, urlAsBytes);
    }

    private static void PatchUrlFromInfoList(BinaryWriter writer, List<PatchTargetInfo> targets, byte[] url)
    {
        foreach (PatchTargetInfo target in targets)
        {
            writer.BaseStream.Position = target.Offset;
            writer.Write(url);

            //Terminate the rest of the string
            for (int i = 0; i < target.Length - url.Length; i++) writer.Write('\0');
        }
    }
}