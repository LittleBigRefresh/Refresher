using System.Text.Json.Serialization;

namespace Refresher.Core.Verification.AutoDiscover;

public class AutoDiscoverResponse
{
    public const int SupportedVersion = 3;

    [JsonPropertyName("version")]
    public int Version { get; set; }

    [JsonPropertyName("serverBrand")]
    public string ServerBrand { get; set; } = null!;

    [JsonPropertyName("serverDescription")]
    public string ServerDescription { get; set; } = "";
        
    [JsonPropertyName("url")]
    public string Url { get; set; } = null!;
    
    [JsonPropertyName("bannerImageUrl")]
    public string? BannerImageUrl { get; set; }

    [JsonPropertyName("usesCustomDigestKey")]
    public bool? UsesCustomDigestKey { get; set; } = false; // We mark as nullable, as this was added in version 2
}