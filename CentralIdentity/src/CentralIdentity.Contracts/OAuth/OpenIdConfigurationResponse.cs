using System.Text.Json.Serialization;

namespace CentralIdentity.Contracts.OAuth;

/// <summary>OpenID Connect Discovery document (/.well-known/openid-configuration).</summary>
public sealed class OpenIdConfigurationResponse
{
    [JsonPropertyName("issuer")]
    public string Issuer { get; set; } = string.Empty;

    [JsonPropertyName("authorization_endpoint")]
    public string AuthorizationEndpoint { get; set; } = string.Empty;

    [JsonPropertyName("token_endpoint")]
    public string TokenEndpoint { get; set; } = string.Empty;

    [JsonPropertyName("userinfo_endpoint")]
    public string UserinfoEndpoint { get; set; } = string.Empty;

    [JsonPropertyName("jwks_uri")]
    public string JwksUri { get; set; } = string.Empty;

    [JsonPropertyName("response_types_supported")]
    public string[] ResponseTypesSupported { get; set; } = Array.Empty<string>();

    [JsonPropertyName("subject_types_supported")]
    public string[] SubjectTypesSupported { get; set; } = { "public" };

    [JsonPropertyName("id_token_signing_alg_values_supported")]
    public string[] IdTokenSigningAlgValuesSupported { get; set; } = { "RS256" };

    [JsonPropertyName("code_challenge_methods_supported")]
    public string[] CodeChallengeMethodsSupported { get; set; } = { "S256" };

    [JsonPropertyName("grant_types_supported")]
    public string[] GrantTypesSupported { get; set; } = { "authorization_code" };

    [JsonPropertyName("scopes_supported")]
    public string[] ScopesSupported { get; set; } = { "openid", "profile", "email" };

    [JsonPropertyName("token_endpoint_auth_methods_supported")]
    public string[] TokenEndpointAuthMethodsSupported { get; set; } = { "client_secret_post", "none" };
}
