using CentralIdentity.Application.Common.Interfaces;

namespace CentralIdentity.Api.Services;

public sealed class DateTimeService : IDateTime
{
    public DateTime UtcNow => DateTime.UtcNow;
}
