namespace CentralIdentity.Contracts.Health;

public record HealthResponse
{
    public string Status { get; init; } = string.Empty;
    public IReadOnlyList<HealthEntry> Checks { get; init; } = Array.Empty<HealthEntry>();
    public TimeSpan TotalDuration { get; init; }
}

public record HealthEntry
{
    public string Name { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? Description { get; init; }
    public TimeSpan Duration { get; init; }
}
