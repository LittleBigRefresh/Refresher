using System.Runtime.InteropServices;

namespace Refresher.Core.Native.Sce;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct SelfHeader
{
    public ulong header_type;            // 0x3 (self)
    public ulong appinfo_offset;         // 0x70
    public ulong elf_offset;             // 0x90
    public ulong phdr_offset;           // generated from ELF
    public ulong shdr_offset;           // generated from ELF
    public ulong section_info_offset;   // generated from ELF
    public ulong sceversion_offset;     // generated from ELF
    public ulong controlinfo_offset;    // generated from ELF
    public ulong controlinfo_length;    // generated from ELF
    public ulong padding;
}