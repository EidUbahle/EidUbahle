using CentralIdentity.Application.Common.Interfaces;

namespace CentralIdentity.UnitTests.Phase4;

internal sealed class TestDateTime : IDateTime
{
    public DateTime UtcNow { get; set; } = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
}
