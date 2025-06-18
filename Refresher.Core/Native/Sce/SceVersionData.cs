// This file contains modified & ported code from jjolano's make_fself C project.
// make_fself is licensed under GPL-3.0.
// Find it here: https://github.com/jjolano/make_fself

using System.Runtime.InteropServices;

namespace Refresher.Core.Native.Sce;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct SceVersionData
{
    public ushort unknown1;
    public ushort unknown2;      // 0x1
    public uint unknown3;        // 0x30
    public uint unknown4;        // 0x0
    public uint unknown5;        // 0x1
    public ulong offset;         // 0x0
    public ulong size;           // 0x0

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
    public byte[] control_flags;
}