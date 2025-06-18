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