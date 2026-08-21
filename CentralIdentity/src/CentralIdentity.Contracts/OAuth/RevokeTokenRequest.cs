namespace CentralIdentity.Contracts.OAuth;

public sealed class RevokeTokenRequest
{
    public string? Token { get; set; }
    public string? TokenTypeHint { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
}
