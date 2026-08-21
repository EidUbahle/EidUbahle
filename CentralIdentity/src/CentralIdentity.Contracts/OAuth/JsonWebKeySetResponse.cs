using System.Text.Json.Serialization;

namespace CentralIdentity.Contracts.OAuth;

/// <summary>A single JSON Web Key (RFC 7517) describing an RSA public key.</summary>
public sealed class JsonWebKey
{
    [JsonPropertyName("kty")]
    public string KeyType { get; set; } = "RSA";

    [JsonPropertyName("use")]
    public string Use { get; set; } = "sig";

    [JsonPropertyName("alg")]
    public string Algorithm { get; set; } = "RS256";

    [JsonPropertyName("kid")]
    public string KeyId { get; set; } = string.Empty;

    /// <summary>Base64url-encoded RSA modulus.</summary>
    [JsonPropertyName("n")]
    public string Modulus { get; set; } = string.Empty;

    /// <summary>Base64url-encoded RSA public exponent.</summary>
    [JsonPropertyName("e")]
    public string Exponent { get; set; } = string.Empty;
}

/// <summary>JSON Web Key Set response (/.well-known/jwks.json).</summary>
public sealed class JsonWebKeySetResponse
{
    [JsonPropertyName("keys")]
    public List<JsonWebKey> Keys { get; set; } = new();
}
