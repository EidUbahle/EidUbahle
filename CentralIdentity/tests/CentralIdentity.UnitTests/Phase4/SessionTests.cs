using CentralIdentity.Domain.Entities;
using CentralIdentity.UnitTests.Fakes;

namespace CentralIdentity.UnitTests.Phase4;

public class SessionTests
{
    [Fact]
    public async Task SessionRepository_CanCreateAndLoadActiveSession()
    {
        var repo = new FakeSessionRepository();
        var session = new IdentitySession
        {
            SessionId = Guid.NewGuid(),
            UserId = 42,
            ApplicationId = 7,
            ClientId = "ci_hospital",
            CreatedAtUtc = DateTime.UtcNow,
            LastActivityAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            SecurityStamp = "stamp",
            IsActive = true
        };

        await repo.CreateAsync(session, CancellationToken.None);
        var active = await repo.GetActiveByUserAsync(42, CancellationToken.None);

        Assert.Single(active);
        Assert.Equal(session.SessionId, active[0].SessionId);
    }

    [Fact]
    public async Task SessionRepository_RevokeRemovesSessionFromActiveResults()
    {
        var repo = new FakeSessionRepository();
        var session = new IdentitySession
        {
            SessionId = Guid.NewGuid(),
            UserId = 42,
            ApplicationId = 7,
            ClientId = "ci_hospital",
            CreatedAtUtc = DateTime.UtcNow,
            LastActivityAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            SecurityStamp = "stamp",
            IsActive = true
        };

        await repo.CreateAsync(session, CancellationToken.None);
        await repo.RevokeAsync(session.SessionId, "logout", CancellationToken.None);
        var active = await repo.GetActiveByUserAsync(42, CancellationToken.None);

        Assert.Empty(active);
    }
}
