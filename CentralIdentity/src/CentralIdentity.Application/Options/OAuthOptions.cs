namespace CentralIdentity.Application.Options;

public sealed class OAuthOptions
{
    public const string SectionName = "OAuth";
    public int AuthorizationCodeLifetimeMinutes { get; set; } = 5;
    public bool RequirePkce { get; set; } = true;
    public string[] AllowedResponseTypes { get; set; } = new[] { "code" };
}
