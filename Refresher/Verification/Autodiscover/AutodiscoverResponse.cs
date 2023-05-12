using Newtonsoft.Json;

namespace Refresher.Verification.Autodiscover;

#nullable disable

public class AutodiscoverResponse
{
    private const int SupportedVersion = 2;

    [JsonProperty("version")]
    public int Version { get; set; }

    [JsonProperty("serverBrand")]
    public string ServerBrand { get; set; }
        
    [JsonProperty("url")]
    public string Url { get; set; }

    [JsonProperty("usesCustomDigestKey")]
    public bool? UsesCustomDigestKey { get; set; } = false; // We mark as nullable, as this was added in version 2
}