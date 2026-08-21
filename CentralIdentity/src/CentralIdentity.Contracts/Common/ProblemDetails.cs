namespace CentralIdentity.Contracts.Common;

public record ProblemDetails
{
    public string Type { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public int Status { get; init; }
    public string Detail { get; init; } = string.Empty;
    public string? TraceId { get; init; }
    public IDictionary<string, string[]>? Errors { get; init; }
}
