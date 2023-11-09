namespace Refresher.Patching;

public enum PatchTargetType
{
    /// <summary>
    /// A bog standard URL
    /// </summary>
    Url,
    /// <summary>
    /// A domain only, no scheme, no port
    /// </summary>
    Host,
    /// <summary>
    /// 2 32-bit big endian port numbers right after one another
    /// </summary>
    DoubleNumeric32BitPort,
    /// <summary>
    /// A LBP digest key
    /// </summary>
    Digest,
}