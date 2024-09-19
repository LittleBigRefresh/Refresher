using System.Text.Json.Serialization;

namespace Refresher.Core.Verification.Autodiscover;

#nullable disable

public class AutodiscoverResponse
{
    private const int SupportedVersion = 2;

    [JsonPropertyName("version")]
    public int Version { get; set; }

    [JsonPropertyName("serverBrand")]
    public string ServerBrand { get; set; }
        
    [JsonPropertyName("url")]
    public string Url { get; set; }

    [JsonPropertyName("usesCustomDigestKey")]
    public bool? UsesCustomDigestKey { get; set; } = false; // We mark as nullable, as this was added in version 2
}