using System.Text;

namespace Refresher.Verification;

// TODO: Move to separate repository with its own nuget package
// Maybe combine with NPTicket?
public class ParamSfo
{
    private Stream _stream;

    public uint Version;

    public Dictionary<string, object> Table { get; set; }= new();

    // https://psdevwiki.com/ps3/PARAM.SFO#Internal_Structure
    public ParamSfo(Stream stream)
    {
        this._stream = stream;
        using BinaryReader reader = new(this._stream);

        ReadOnlySpan<byte> headerToMatch = "\0PSF"u8;

        byte[] header = reader.ReadBytes(4);
        
        // ReSharper disable once LoopCanBeConvertedToQuery
        for (int i = 0; i < header.Length; i++)
        {
            if (header[i] == headerToMatch[i])
                continue;

            throw new InvalidDataException("Magic header does not match the expected data from a typical PARAM.SFO file!");
        }

        this.Version = reader.ReadUInt32();
        
        uint keyTableStart = reader.ReadUInt32();
        uint dataTableStart = reader.ReadUInt32();
        
        uint tableEntries = reader.ReadUInt32(); // Number of pairs in dictionary
        
        for (uint i = 0; i < tableEntries; i++)
        {
            ushort keyOffset = reader.ReadUInt16();
            reader.ReadByte(); // All types start with 0x04 (0x0402 => string), very unnecessary
            byte type = reader.ReadByte(); // 0x02

            uint dataLength = reader.ReadUInt32(); // used bytes
            uint dataTotalLength = reader.ReadUInt32(); // total bytes
            uint dataOffset = reader.ReadUInt32();

            long oldPosition = stream.Position;

            stream.Position = keyTableStart + keyOffset;
            string name = string.Empty;
            while(true)
            {
                char c = (char)reader.ReadByte();
                if (c == '\0') break;
                
                name += c;
            }

            object value;
            stream.Position = dataTableStart + dataOffset;

            // ReSharper disable once RedundantCaseLabel
            switch (type)
            {
                case 2: // utf8
                    // -1: trim null byte
                    value = Encoding.Default.GetString(reader.ReadBytes((int)dataLength - 1));
                    break;
                case 4: // int32
                    // TODO (im lazy)
                case 0: // utf8-s, binary i assume
                default:
                    value = reader.ReadBytes((int)dataLength);
                    break;
            }
            
            this.Table.Add(name, value);

            stream.Position = oldPosition;
        }
    }
}