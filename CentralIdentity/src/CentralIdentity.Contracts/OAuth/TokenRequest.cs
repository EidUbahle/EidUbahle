namespace CentralIdentity.Contracts.OAuth;

/// <summary>
/// Form-encoded token request body (RFC 6749 §4.1.3 authorization_code grant, with PKCE — RFC 7636).
/// Property names use the snake_case wire format mandated by RFC 6749; the API controller binds
/// the individual form fields and maps them onto this DTO (kept framework-agnostic here).
/// </summary>
public sealed class TokenRequest
{
    public string GrantType { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? RedirectUri { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? CodeVerifier { get; set; }
}
