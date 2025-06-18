// This file contains modified & ported code from jjolano's make_fself C project.
// make_fself is licensed under GPL-3.0.
// Find it here: https://github.com/jjolano/make_fself

using System.Runtime.InteropServices;

namespace Refresher.Core.Native.Elf;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Elf64Ehdr
{
    public unsafe fixed byte e_ident[16];
    public ushort e_type;
    public ushort e_machine;
    public uint e_version;
    public ulong e_entry;
    public ulong e_phoff;
    public ulong e_shoff;
    public uint e_flags;
    public ushort e_ehsize;
    public ushort e_phentsize;
    public ushort e_phnum;
    public ushort e_shentsize;
    public ushort e_shnum;
    public ushort e_shstrndx;
}