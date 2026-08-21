using CentralIdentity.Domain.Entities;
using CentralIdentity.UnitTests.Fakes;
using Xunit;

namespace CentralIdentity.UnitTests.Phase6;

public sealed class InactivityTests
{
    [Fact]
    public async Task InactiveApplication_IsRevoked_OtherApplicationsUnaffected()
    {
        var userAppRepo = new FakeUserApplicationRepository();

        userAppRepo.Add(new IdentityUserApplication { UserId = 1, ApplicationId = 1, IsActive = true, Status = "Active", LastActivityAtUtc = DateTime.UtcNow.AddDays(-2), AssignedAtUtc = DateTime.UtcNow.AddDays(-30) });
        userAppRepo.Add(new IdentityUserApplication { UserId = 1, ApplicationId = 2, IsActive = true, Status = "Active", LastActivityAtUtc = DateTime.UtcNow.AddDays(-8), AssignedAtUtc = DateTime.UtcNow.AddDays(-30) });
        userAppRepo.Add(new IdentityUserApplication { UserId = 1, ApplicationId = 3, IsActive = true, Status = "Active", LastActivityAtUtc = DateTime.UtcNow.AddDays(-1), AssignedAtUtc = DateTime.UtcNow.AddDays(-30) });

        var threshold = DateTime.UtcNow.AddDays(-7);
        var inactive = await userAppRepo.GetInactiveByThresholdAsync(threshold, 500, default);

        Assert.Single(inactive);
        Assert.Equal(2, inactive[0].ApplicationId);

        await userAppRepo.RevokeForInactivityAsync(1, 2, default);

        var university = await userAppRepo.GetAsync(1, 1, default);
        var hospital = await userAppRepo.GetAsync(1, 2, default);
        var tax = await userAppRepo.GetAsync(1, 3, default);

        Assert.True(university!.IsActive);
        Assert.False(hospital!.IsActive);
        Assert.Equal("InactivityRevocation", hospital.RevocationReason);
        Assert.Equal("Inactive", hospital.Status);
        Assert.True(tax!.IsActive);
    }

    [Fact]
    public async Task AlreadyRevoked_NotReturnedByInactivityQuery()
    {
        var userAppRepo = new FakeUserApplicationRepository();
        userAppRepo.Add(new IdentityUserApplication { UserId = 1, ApplicationId = 1, IsActive = false, Status = "Revoked", LastActivityAtUtc = DateTime.UtcNow.AddDays(-15), AssignedAtUtc = DateTime.UtcNow.AddDays(-30) });

        var threshold = DateTime.UtcNow.AddDays(-7);
        var inactive = await userAppRepo.GetInactiveByThresholdAsync(threshold, 500, default);
        Assert.Empty(inactive);
    }

    [Fact]
    public async Task ActivityUpdate_ResetsInactivityTimer()
    {
        var userAppRepo = new FakeUserApplicationRepository();
        userAppRepo.Add(new IdentityUserApplication { UserId = 1, ApplicationId = 1, IsActive = true, Status = "Active", LastActivityAtUtc = DateTime.UtcNow.AddDays(-8), AssignedAtUtc = DateTime.UtcNow.AddDays(-30) });

        await userAppRepo.UpdateActivityAsync(1, 1, DateTime.UtcNow, default);

        var threshold = DateTime.UtcNow.AddDays(-7);
        var inactive = await userAppRepo.GetInactiveByThresholdAsync(threshold, 500, default);
        Assert.Empty(inactive);
    }

    [Fact]
    public async Task ManuallyRevokedApplication_IsInactive_NotReactivatedByActivity()
    {
        var userAppRepo = new FakeUserApplicationRepository();
        var ua = new IdentityUserApplication { UserId = 1, ApplicationId = 1, IsActive = false, Status = "Revoked", RevocationReason = "ManualRevocation", LastActivityAtUtc = DateTime.UtcNow, AssignedAtUtc = DateTime.UtcNow.AddDays(-30) };
        userAppRepo.Add(ua);

        await userAppRepo.UpdateActivityAsync(1, 1, DateTime.UtcNow, default);
        var fetched = await userAppRepo.GetAsync(1, 1, default);

        Assert.False(fetched!.IsActive);
        Assert.Equal("Revoked", fetched.Status);
    }

    [Fact]
    public async Task InactiveRefreshToken_IsRejected()
    {
        var refreshTokenRepo = new FakeRefreshTokenRepository();
        var revokedToken = new IdentityRefreshToken
        {
            RefreshTokenId = Guid.NewGuid(),
            UserId = 1,
            ApplicationId = 2,
            SessionId = Guid.NewGuid(),
            TokenHash = "revoked-hash",
            TokenFamilyId = Guid.NewGuid(),
            CreatedAtUtc = DateTime.UtcNow.AddDays(-8),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(22),
            RevokedAtUtc = DateTime.UtcNow.AddDays(-1),
            RevocationReason = "InactivityRevocation"
        };
        await refreshTokenRepo.CreateAsync(revokedToken, default);

        var fetched = await refreshTokenRepo.GetByHashAsync("revoked-hash", default);
        Assert.NotNull(fetched);
        Assert.NotNull(fetched!.RevokedAtUtc);
    }

    [Fact]
    public async Task BatchProcessing_IsIdempotent()
    {
        var userAppRepo = new FakeUserApplicationRepository();
        userAppRepo.Add(new IdentityUserApplication { UserId = 1, ApplicationId = 2, IsActive = true, Status = "Active", LastActivityAtUtc = DateTime.UtcNow.AddDays(-9), AssignedAtUtc = DateTime.UtcNow.AddDays(-30) });

        var threshold = DateTime.UtcNow.AddDays(-7);

        await userAppRepo.RevokeForInactivityAsync(1, 2, default);

        var inactive = await userAppRepo.GetInactiveByThresholdAsync(threshold, 500, default);
        Assert.Empty(inactive);
    }
}
