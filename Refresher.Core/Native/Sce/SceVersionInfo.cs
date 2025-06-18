// This file contains modified & ported code from jjolano's make_fself C project.
// make_fself is licensed under GPL-3.0.
// Find it here: https://github.com/jjolano/make_fself

using System.Runtime.InteropServices;

namespace Refresher.Core.Native.Sce;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct SceVersionInfo
{
    public uint subheader_type;  // 0x1
    public uint present;         // 0x0
    public uint size;            // 0x10
    public uint unknown4;        // 0x0
}