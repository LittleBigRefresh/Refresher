// This file contains modified & ported code from jjolano's make_fself C project.
// make_fself is licensed under GPL-3.0.
// Find it here: https://github.com/jjolano/make_fself

using System.Runtime.InteropServices;

namespace Refresher.Core.Native.Sce;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct AppInfo
{
    public ulong auth_id;       // 0x1010000001000003
    public uint vendor_id;      // 0x1000002
    public uint self_type;      // 0x4 (application)
    public ulong version;       // 0x0001000000000000
    public ulong padding;
}