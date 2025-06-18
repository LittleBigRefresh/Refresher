// This file contains modified & ported code from jjolano's make_fself C project.
// make_fself is licensed under GPL-3.0.
// Find it here: https://github.com/jjolano/make_fself

using System.Runtime.InteropServices;

namespace Refresher.Core.Native.Sce;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct SceHeader
{
    public uint magic;        // 0x53434500
    public uint version;      // 0x2
    public ushort keyrev;     // 0x8000 (devkit)
    public ushort type;       // 0x1 (self)
    public uint meta_off;     // generated from ELF
    public ulong head_len;    // generated from ELF
    public ulong data_len;
}