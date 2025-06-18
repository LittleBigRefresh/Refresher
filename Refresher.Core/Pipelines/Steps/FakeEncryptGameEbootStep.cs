using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Refresher.Core.Native.Elf;
using Refresher.Core.Native.Sce;

namespace Refresher.Core.Pipelines.Steps;

public class FakeEncryptGameEbootStep : Step
{
    public FakeEncryptGameEbootStep(Pipeline pipeline) : base(pipeline)
    {}
    
    private static ushort Swap16(ushort val) => (ushort)(((val & 0xFF00) >> 8) | ((val & 0x00FF) << 8));
    private static uint Swap32(uint val) =>
        ((val & 0xFF000000) >> 24) |
        ((val & 0x00FF0000) >> 8) |
        ((val & 0x0000FF00) << 8) |
        ((val & 0x000000FF) << 24);

    private static ulong Swap64(ulong val) =>
        ((val & 0xFF00000000000000UL) >> 56) |
        ((val & 0x00FF000000000000UL) >> 40) |
        ((val & 0x0000FF0000000000UL) >> 24) |
        ((val & 0x000000FF00000000UL) >> 8) |
        ((val & 0x00000000FF000000UL) << 8) |
        ((val & 0x0000000000FF0000UL) << 24) |
        ((val & 0x000000000000FF00UL) << 40) |
        ((val & 0x00000000000000FFUL) << 56);
    
    private static void WriteStruct<T>(Stream stream, T @struct) where T : struct
    {
        int size = Marshal.SizeOf<T>();
        byte[] buffer = new byte[size];
        IntPtr ptr = Marshal.AllocHGlobal(size);

        Marshal.StructureToPtr(@struct, ptr, false);
        Marshal.Copy(ptr, buffer, 0, size);
        Marshal.FreeHGlobal(ptr);
        
        stream.Write(buffer, 0, size);
    }

    public override float Progress { get; protected set; }
    public override Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        State.Logger.LogDebug(Encrypt, "Loading ELF to memory...");
        byte[] elfData = File.ReadAllBytes(this.Game.DecryptedEbootPath);

        Elf64Ehdr elfHeader = MemoryMarshal.Read<Elf64Ehdr>(elfData);

        State.Logger.LogDebug(Encrypt, "Calculating offsets...");

        SceHeader sceHeader = new();
        SelfHeader selfHeader = new();
        AppInfo appInfo = new();
        SceVersionInfo sceVersionInfo = new();
        SceVersionData sceVersionData = new();
        ControlInfo controlInfo = new();

        int fselfHeaderSize = Marshal.SizeOf<SceHeader>() + Marshal.SizeOf<SelfHeader>() + Marshal.SizeOf<AppInfo>();

        ulong phdrOffset = Swap64(elfHeader.e_phoff) + (ulong)fselfHeaderSize;
        ushort phdrCount = Swap16(elfHeader.e_phnum);

        ulong sectionInfoOffset = (ulong)fselfHeaderSize + Swap16(elfHeader.e_ehsize) + ((ulong)Swap16(elfHeader.e_phentsize) * phdrCount);
        ulong sceVersionOffset = sectionInfoOffset + (ulong)(phdrCount * Marshal.SizeOf<SegmentInfo>());
        ulong controlInfoOffset = sceVersionOffset + (ulong)Marshal.SizeOf<SceVersionInfo>();

        sceHeader.magic = Swap32(0x53434500);
        sceHeader.version = Swap32(0x2);
        sceHeader.keyrev = Swap16(0x8000);
        sceHeader.type = Swap16(0x1);
        sceHeader.meta_off = Swap32((uint)(controlInfoOffset + (ulong)Marshal.SizeOf<SceVersionData>() + (ulong)(Marshal.SizeOf<ControlInfo>() / 2)));
        sceHeader.head_len = Swap64(controlInfoOffset + (ulong)Marshal.SizeOf<SceVersionData>() + (ulong)Marshal.SizeOf<ControlInfo>());
        sceHeader.data_len = Swap64((ulong)elfData.Length);

        ulong shdrOffset = Swap64(elfHeader.e_shoff) + Swap64(sceHeader.head_len);

        State.Logger.LogDebug(Encrypt, "Calculating segments ...");
        SegmentInfo[] segments = new SegmentInfo[phdrCount];

        for (int i = 0; i < phdrCount; i++)
        {
            long offset = (long)Swap64(elfHeader.e_phoff) + i * Marshal.SizeOf<Elf64_Phdr>();
            Elf64_Phdr elfPhdr = MemoryMarshal.Read<Elf64_Phdr>(elfData.AsSpan((int)offset));

            segments[i].offset = Swap64(Swap64(elfPhdr.p_offset) + Swap64(sceHeader.head_len));
            segments[i].size = elfPhdr.p_filesz;
            segments[i].compressed = Swap32(0x1);
            segments[i].encrypted = Swap32(0x2);
        }

        selfHeader.header_type = Swap64(0x3);
        selfHeader.appinfo_offset = Swap64(0x70);
        selfHeader.elf_offset = Swap64(0x90);
        selfHeader.phdr_offset = Swap64(phdrOffset);
        selfHeader.shdr_offset = Swap64(shdrOffset);
        selfHeader.section_info_offset = Swap64(sectionInfoOffset);
        selfHeader.sceversion_offset = Swap64(sceVersionOffset);
        selfHeader.controlinfo_offset = Swap64(controlInfoOffset);
        selfHeader.controlinfo_length = Swap64((ulong)(Marshal.SizeOf<ControlInfo>() + Marshal.SizeOf<SceVersionData>()));

        appInfo.auth_id = Swap64(0x1010000001000003);
        appInfo.vendor_id = Swap32(0x1000002);
        appInfo.self_type = Swap32(0x4);
        appInfo.version = Swap64(0x0001000000000000);

        sceVersionInfo.subheader_type = Swap32(0x1);
        sceVersionInfo.size = Swap32((uint)Marshal.SizeOf<SceVersionInfo>());

        sceVersionData.unknown2 = Swap16(0x1);
        sceVersionData.unknown3 = Swap32((uint)Marshal.SizeOf<SceVersionData>());
        sceVersionData.unknown5 = Swap32(0x1);
        sceVersionData.control_flags = new byte[16];

        controlInfo.type = Swap32(0x2);
        controlInfo.size = Swap32((uint)Marshal.SizeOf<ControlInfo>());
        controlInfo.digest1 = new byte[20];
        controlInfo.digest2 = new byte[20];

        State.Logger.LogDebug(Encrypt, "Calculating hashes...");
        byte[] hardcodedSha = [0x62, 0x7c, 0xb1, 0x80, 0x8a, 0xb9, 0x38, 0xe3, 0x2c, 0x8c, 0x09, 0x17, 0x08, 0x72, 0x6a, 0x57, 0x9e, 0x25, 0x86, 0xe4];
        Buffer.BlockCopy(hardcodedSha, 0, controlInfo.digest1, 0, 20);
        controlInfo.digest2 = SHA1.HashData(elfData);

        State.Logger.LogDebug(Encrypt, "FSELF built, writing...");
        string outputPath = this.Game.EncryptedEbootPath = Path.GetTempFileName();
        using FileStream output = File.Open(outputPath, FileMode.Create);
        WriteStruct(output, sceHeader);
        WriteStruct(output, selfHeader);
        WriteStruct(output, appInfo);
        output.Write(elfData, 0, Swap16(elfHeader.e_ehsize) + (Swap16(elfHeader.e_phentsize) * phdrCount));

        foreach (SegmentInfo seg in segments)
            WriteStruct(output, seg);

        WriteStruct(output, sceVersionInfo);
        WriteStruct(output, sceVersionData);
        WriteStruct(output, controlInfo);

        output.Write(elfData);
        output.Flush();

        return Task.CompletedTask;
    }


}