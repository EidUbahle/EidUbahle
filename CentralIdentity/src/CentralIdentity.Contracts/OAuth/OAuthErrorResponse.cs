using System.Text.Json.Serialization;

namespace CentralIdentity.Contracts.OAuth;

/// <summary>OAuth2 error response (RFC 6749 §5.2 / §4.1.2.1).</summary>
public sealed class OAuthErrorResponse
{
    [JsonPropertyName("error")]
    public string Error { get; set; } = string.Empty;

    [JsonPropertyName("error_description")]
    public string? ErrorDescription { get; set; }
}
