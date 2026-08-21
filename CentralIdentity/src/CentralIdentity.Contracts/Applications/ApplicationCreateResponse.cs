namespace CentralIdentity.Contracts.Applications;

/// <summary>
/// Returned exactly once at registration time. The plaintext client secret is shown
/// only here — it cannot be retrieved again afterwards since only its hash is persisted.
/// </summary>
public sealed class ApplicationCreateResponse
{
    public long ApplicationId { get; set; }
    public string ApplicationCode { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;

    /// <summary>Null for Public clients (which do not receive a client secret).</summary>
    public string? PlaintextClientSecret { get; set; }

    public string ClientType { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
}
