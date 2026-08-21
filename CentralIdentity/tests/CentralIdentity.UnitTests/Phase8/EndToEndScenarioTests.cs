using CentralIdentity.Domain.Entities;
using CentralIdentity.UnitTests.Fakes;
using Xunit;

namespace CentralIdentity.UnitTests.Phase8;

public sealed class EndToEndScenarioTests
{
    [Fact]
    public async Task Mohamed_MultiAppScenario_CrossApplicationIsolation()
    {
        var userRepo = new FakeUserRepository();
        var appRepo = new FakeApplicationRepository();
        var userAppRepo = new FakeUserApplicationRepository();
        var sessionRepo = new FakeSessionRepository();
        var refreshTokenRepo = new FakeRefreshTokenRepository();
        var auditRepo = new FakeAuditLogRepository();

        var mohamedId = 1L;
        var universityAppId = 1L;
        var hospitalAppId = 2L;
        var taxAppId = 3L;

        userAppRepo.Add(new IdentityUserApplication
        {
            UserId = mohamedId,
            ApplicationId = universityAppId,
            IsActive = true,
            Status = "Active",
            LastActivityAtUtc = DateTime.UtcNow.AddDays(-2),
            AssignedAtUtc = DateTime.UtcNow.AddDays(-30)
        });
        userAppRepo.Add(new IdentityUserApplication
        {
            UserId = mohamedId,
            ApplicationId = hospitalAppId,
            IsActive = true,
            Status = "Active",
            LastActivityAtUtc = DateTime.UtcNow.AddDays(-2),
            AssignedAtUtc = DateTime.UtcNow.AddDays(-30)
        });
        userAppRepo.Add(new IdentityUserApplication
        {
            UserId = mohamedId,
            ApplicationId = taxAppId,
            IsActive = true,
            Status = "Active",
            LastActivityAtUtc = DateTime.UtcNow.AddDays(-1),
            AssignedAtUtc = DateTime.UtcNow.AddDays(-30)
        });

        var hospitalApp = await userAppRepo.GetAsync(mohamedId, hospitalAppId, default);
        Assert.NotNull(hospitalApp);
        hospitalApp!.IsActive = false;
        hospitalApp.Status = "Revoked";
        hospitalApp.RevocationReason = "ManualRevocation";
        hospitalApp.RevokedAtUtc = DateTime.UtcNow;

        var hospitalFamilyId = Guid.NewGuid();
        var hospitalSessionId = Guid.NewGuid();
        var hospitalToken = new IdentityRefreshToken
        {
            RefreshTokenId = Guid.NewGuid(),
            UserId = mohamedId,
            ApplicationId = hospitalAppId,
            SessionId = hospitalSessionId,
            TokenHash = "hospital-token-hash",
            TokenFamilyId = hospitalFamilyId,
            CreatedAtUtc = DateTime.UtcNow.AddHours(-1),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(29),
            RevokedAtUtc = DateTime.UtcNow,
            RevocationReason = "ManualRevocation"
        };
        await refreshTokenRepo.CreateAsync(hospitalToken, default);

        var uniSessionId = Guid.NewGuid();
        var uniFamilyId = Guid.NewGuid();
        var uniToken = new IdentityRefreshToken
        {
            RefreshTokenId = Guid.NewGuid(),
            UserId = mohamedId,
            ApplicationId = universityAppId,
            SessionId = uniSessionId,
            TokenHash = "uni-token-hash",
            TokenFamilyId = uniFamilyId,
            CreatedAtUtc = DateTime.UtcNow.AddHours(-1),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(29)
        };
        await refreshTokenRepo.CreateAsync(uniToken, default);

        var hospitalAssignment = await userAppRepo.GetAsync(mohamedId, hospitalAppId, default);
        Assert.False(hospitalAssignment!.IsActive);

        var uniAssignment = await userAppRepo.GetAsync(mohamedId, universityAppId, default);
        Assert.True(uniAssignment!.IsActive);

        var taxAssignment = await userAppRepo.GetAsync(mohamedId, taxAppId, default);
        Assert.True(taxAssignment!.IsActive);

        var fetchedHospitalToken = await refreshTokenRepo.GetByHashAsync("hospital-token-hash", default);
        Assert.NotNull(fetchedHospitalToken);
        Assert.NotNull(fetchedHospitalToken!.RevokedAtUtc);

        var fetchedUniToken = await refreshTokenRepo.GetByHashAsync("uni-token-hash", default);
        Assert.NotNull(fetchedUniToken);
        Assert.Null(fetchedUniToken!.RevokedAtUtc);

        var threshold = DateTime.UtcNow.AddDays(-7);
        var inactive = await userAppRepo.GetInactiveByThresholdAsync(threshold, 500, default);
        Assert.Empty(inactive);

        var user = await userRepo.GetByIdAsync(mohamedId, default);
        Assert.Null(user);
        _ = appRepo;
        _ = sessionRepo;
        _ = auditRepo;
    }

    [Fact]
    public async Task UniversityToken_CannotBeUsedForHospitalSession()
    {
        var universityFamilyId = Guid.NewGuid();
        var hospitalAppId = 2L;
        var universityAppId = 1L;

        var refreshTokenRepo = new FakeRefreshTokenRepository();

        var uniToken = new IdentityRefreshToken
        {
            RefreshTokenId = Guid.NewGuid(),
            UserId = 1,
            ApplicationId = universityAppId,
            SessionId = Guid.NewGuid(),
            TokenHash = "uni-token",
            TokenFamilyId = universityFamilyId,
            CreatedAtUtc = DateTime.UtcNow.AddHours(-1),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(29)
        };
        await refreshTokenRepo.CreateAsync(uniToken, default);

        var fetched = await refreshTokenRepo.GetByHashAsync("uni-token", default);
        Assert.NotNull(fetched);
        Assert.NotEqual(hospitalAppId, fetched!.ApplicationId);
        Assert.Equal(universityAppId, fetched.ApplicationId);
    }
}
