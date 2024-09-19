namespace Refresher.Core.Patching;

public struct PatchTargetInfo
{
    public long Offset;
    public int Length;
    public string? Data;
    public PatchTargetType Type;
}