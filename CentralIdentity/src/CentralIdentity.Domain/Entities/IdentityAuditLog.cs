namespace CentralIdentity.Domain.Entities;

public sealed class IdentityAuditLog
{
    public long AuditLogId { get; set; }
    public long? UserId { get; set; }
    public long? ApplicationId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
