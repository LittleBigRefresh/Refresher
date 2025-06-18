// This file contains modified & ported code from jjolano's make_fself C project.
// make_fself is licensed under GPL-3.0.
// Find it here: https://github.com/jjolano/make_fself

using System.Runtime.InteropServices;

namespace Refresher.Core.Native.Sce;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct SegmentInfo
{
    public ulong offset;
    public ulong size;
    public uint compressed;
    public uint unknown1;
    public uint unknown2;
    public uint encrypted;
}