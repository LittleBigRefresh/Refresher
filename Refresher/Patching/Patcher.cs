using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Text;
using System.Text.RegularExpressions;
using ELFSharp.ELF;
using ELFSharp.ELF.Sections;
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

        this.Stream.Position = 0;

        this._targets = new(() => FindPatchableElements(stream));
    }

    public Stream Stream { get; }

    [GeneratedRegex("^https?[^\\x00]//([0-9a-zA-Z.:].*)/?([0-9a-zA-Z_]*)$", RegexOptions.Compiled)]
    private static partial Regex UrlMatch();

    [GeneratedRegex("[a-zA-Z0-9!@#$%^&*()?/<>]", RegexOptions.Compiled)]
    private static partial Regex DigestMatch();

    /// <summary>
    /// Finds a set of URLs and Digest keys in the given file, excluding C printf strings.
    /// </summary>
    /// <param name="file">A seekable stream containing the file to look through</param>
    /// <returns>A list of the URLs and Digest keys</returns>
    private static List<PatchTargetInfo> FindPatchableElements(Stream file)
    {
        file.Position = 0;
        using IELF? elf = ELFReader.Load(file, false);
        file.Position = 0;

        BinaryReader reader = new(file);

        // The string "http" in ASCII, as an int
        int httpInt = BitConverter.ToInt32("http"u8);

        // The first 4-byte word of the word "cookie", which seems to always proceed digest keys
        int cookieStartInt = BitConverter.ToInt32("cook"u8);

        //Found positions of `http` in the binary
        List<long> possibleUrls = new();
        //Found positions of `cook` in the binary
        List<long> cookiePositions = new();

        foreach (ISection section in elf.Sections)
        {
            // Assume a word size of 4, i would use `ProgBitsSection<T>.Alignment`
            // but that seems to be unrelated to the actual alignment and offset chosen for the string constants specifically (the section with all the strings is marked as 32-byte alignment[?????])
            // so we'll just assume 4 byte alignment since that seems universal here
            int wordSize = 4;
            ulong sectionLength;
            switch (section)
            {
                case ProgBitsSection<ulong> progBitsSectionUlong:
                    file.Position = (long)progBitsSectionUlong.Offset;
                    sectionLength = progBitsSectionUlong.Size;
                    break;
                case ProgBitsSection<uint> progBitsSectionUint:
                    file.Position = progBitsSectionUint.Offset;
                    sectionLength = progBitsSectionUint.Size;
                    break;
                default:
                    continue;
            }

            int read = 0;

            byte[] buf = new byte[wordSize];
            //While we are not at the end of the file, read each word in
            while (read < (int)sectionLength - wordSize)
            {
                read += file.Read(buf);

                int bufInt = BitConverter.ToInt32(buf);

                //If they are equal, we found an instance of HTTP
                if (bufInt == httpInt)
                {
                    //Mark the position of the found instance
                    possibleUrls.Add(reader.BaseStream.Position - wordSize);
                }

                if (bufInt == cookieStartInt)
                {
                    cookiePositions.Add(reader.BaseStream.Position - wordSize);
                }
            }
        }

        List<PatchTargetInfo> foundItems = new();
        FilterValidUrls(file, possibleUrls, reader, foundItems);
        FindDigestAroundCookie(file, cookiePositions, reader, foundItems);
        return foundItems;
    }

    private static void FilterValidUrls(Stream file, List<long> foundPossibleUrlPositions, BinaryReader reader, List<PatchTargetInfo> foundItems)
    {
        bool tooLong = false;
        foreach (long foundPosition in foundPossibleUrlPositions)
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

            if (str.Contains('%')) continue; // Ignore printf strings, e.g. %s

            if (UrlMatch().Matches(str).Count != 0)
                foundItems.Add(new PatchTargetInfo
                {
                    Length = len,
                    Offset = foundPosition,
                    Data = str,
                    Type = PatchTargetType.Url,
                });
        }
    }

    private static void FindDigestAroundCookie(Stream file, List<long> foundPossibleCookiePositions, BinaryReader reader, List<PatchTargetInfo> foundItems)
    {
        foreach (long foundPosition in foundPossibleCookiePositions)
        {
            file.Position = foundPosition;

            byte[] cookieBuf = new byte[8];
            //If we didnt read enough or what we read isnt "cookie\0\0"
            if (reader.Read(cookieBuf) < cookieBuf.Length || !"cookie\0\0"u8.SequenceEqual(cookieBuf))
            {
                //Skip this possibility
                continue;
            }

            const int checkSize = 1000;

            //Go back half the check size in bytes (so that `cookie` is in the middle)
            file.Position -= checkSize / 2;

            byte[] checkArr = new byte[checkSize];
            Span<byte> toCheck = checkArr.AsSpan().Slice(0, reader.Read(checkArr));

            Regex regex = DigestMatch();
            for (int i = 0; i < toCheck.Length; i++)
            {
                //Skip all null bytes
                while (i < toCheck.Length && toCheck[i] == 0) i++;

                int len = 0;
                int start = i;

                //Go over all non-null bytes
                while (i < toCheck.Length && toCheck[i] != 0)
                {
                    len++;
                    i++;
                }

                if (len != 18)
                    continue;

                string str = Encoding.UTF8.GetString(toCheck.Slice(start, len));

                //If theres exactly 18 matches,
                if (regex.Matches(str).Count == 18)
                {
                    //Then we have found a digest key
                    foundItems.Add(new PatchTargetInfo
                    {
                        Length = len,
                        Offset = file.Position - checkSize + start,
                        Data = str,
                        Type = PatchTargetType.Digest,
                    });
                }
            }
        }
    }

    /// <summary>
    ///     Checks the contents of the EBOOT to verify that it is patchable.
    /// </summary>
    /// <returns>A list of issues and notes about the EBOOT.</returns>
    [Pure]
    public List<Message> Verify(string url, bool patchDigest)
    {
        List<Message> messages = new();

        this.Stream.Position = 0;
        Class output = ELFReader.CheckELFType(this.Stream);
        if (output == Class.NotELF)
            messages.Add(new Message(MessageLevel.Warning, 
                                     "File is not a valid ELF!"));

        // Check url
        if (url.EndsWith('/'))
            messages.Add(new Message(MessageLevel.Error,
                                     "URI cannot end with a trailing slash, invalid requests will be sent to the server"));

        //Try to create an absolute URI, if it fails, its not a valid URI
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri _))
            messages.Add(new Message(MessageLevel.Error, "URI is not valid"));

        // If there are no targets, we cant patch
        // ReSharper disable once SimplifyLinqExpressionUseAll
        if (!this._targets.Value.Any(x => x.Type == PatchTargetType.Url))
            messages.Add(new Message(MessageLevel.Error,
                                     "Could not find any urls in the EBOOT. Nothing can be changed."));

        if (this._targets.Value.Any(x => x.Type == PatchTargetType.Url && x.Length < url.Length))
            messages.Add(new Message(MessageLevel.Error,
                                     "The URL is too long to fit in the EBOOT. Please use a shorter URL."));

        // If we are patching digest, check if we found a digest in the EBOOT
        if (patchDigest && this._targets.Value.Count(x => x.Type == PatchTargetType.Digest) == 0)
            messages.Add(new Message(MessageLevel.Warning,
                                     "Could not find the digest in the EBOOT. Resulting EBOOT may still work depending on game."));

        return messages;
    }

    public void Patch(string url, bool patchDigest)
    {
        using BinaryWriter writer = new(this.Stream);

        PatchFromInfoList(writer, this._targets.Value, url, patchDigest);
    }

    private static void PatchFromInfoList(BinaryWriter writer, List<PatchTargetInfo> targets, string url, bool patchDigest)
    {
        byte[] urlAsBytes = Encoding.UTF8.GetBytes(url);
        foreach (PatchTargetInfo target in targets.Where(x => x.Type == PatchTargetType.Url))
        {
            writer.BaseStream.Position = target.Offset;
            writer.Write(urlAsBytes);

            //Terminate the rest of the string
            for (int i = 0; i < target.Length - url.Length; i++) writer.Write('\0');
        }

        if (patchDigest)
        {
            ReadOnlySpan<byte> digestBytes = "CustomServerDigest"u8;

            foreach (PatchTargetInfo target in targets.Where(x => x.Type == PatchTargetType.Digest))
            {
                Debug.Assert(target.Length == 18);

                writer.BaseStream.Position = target.Offset;
                writer.Write(digestBytes);
            }
        }
    }
}