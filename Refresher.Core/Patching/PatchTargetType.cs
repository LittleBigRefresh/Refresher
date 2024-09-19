namespace Refresher.Core.Patching;

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
    /// A LBP digest key
    /// </summary>
    Digest,
}