// This file contains modified & ported code from jjolano's make_fself C project.
// make_fself is licensed under GPL-3.0.
// Find it here: https://github.com/jjolano/make_fself

using System.Runtime.InteropServices;

namespace Refresher.Core.Native.Elf;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Elf64Phdr
{
    public uint p_type;
    public uint p_flags;
    public ulong p_offset;
    public ulong p_vaddr;
    public ulong p_paddr;
    public ulong p_filesz;
    public ulong p_memsz;
    public ulong p_align;
}