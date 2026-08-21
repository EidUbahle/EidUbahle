namespace CentralIdentity.Application.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = string.Empty;
    public int AccessTokenLifetimeMinutes { get; set; } = 10;
    public int RefreshTokenLifetimeDays { get; set; } = 30;
    public string SigningKeyId { get; set; } = string.Empty;
    public string SigningAlgorithm { get; set; } = "RS256";
    public string RsaPrivateKeyPemFile { get; set; } = string.Empty;
}
