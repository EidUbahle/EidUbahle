namespace CentralIdentity.Application.Options;

public sealed class SecurityOptions
{
    public const string SectionName = "Security";
    public int ApplicationInactivityDays { get; set; } = 7;
    public int MaxFailedLoginAttempts { get; set; } = 5;
    public int LockoutMinutes { get; set; } = 15;
    public int ActivityUpdateIntervalMinutes { get; set; } = 15;
    public int InactivityJobIntervalMinutes { get; set; } = 60;
    public int InactivityBatchSize { get; set; } = 500;
}
