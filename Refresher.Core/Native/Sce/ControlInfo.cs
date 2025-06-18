// This file contains modified & ported code from jjolano's make_fself C project.
// make_fself is licensed under GPL-3.0.
// Find it here: https://github.com/jjolano/make_fself

using System.Runtime.InteropServices;

namespace Refresher.Core.Native.Sce;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ControlInfo
{
    public uint type;            // 0x2
    public uint size;            // 0x40
    public ulong next;           // 0x0

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
    public byte[] digest1;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
    public byte[] digest2;

    public ulong padding;
}