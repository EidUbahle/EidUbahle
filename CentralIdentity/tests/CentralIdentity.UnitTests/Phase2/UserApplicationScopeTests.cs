using CentralIdentity.Application.Services;
using CentralIdentity.Domain.Entities;
using CentralIdentity.UnitTests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CentralIdentity.UnitTests.Phase2;

/// <summary>
/// Verifies that access is scoped per-application: revoking a user's access to one
/// application (e.g. "Hospital") must not affect their access to any other application
/// (e.g. "University") they are also assigned to.
/// </summary>
public class UserApplicationScopeTests
{
    private static async Task<(UserApplicationService Service, long UserId, long HospitalAppId, long UniversityAppId)> SeedAsync()
    {
        var userRepo = new FakeUserRepository();
        var appRepo = new FakeApplicationRepository();
        var uaRepo = new FakeUserApplicationRepository();
        var service = new UserApplicationService(uaRepo, userRepo, appRepo, NullLogger<UserApplicationService>.Instance);

        var userId = await userRepo.CreateAsync(new IdentityUser
        {
            Username = "jdoe",
            Email = "jdoe@example.com",
            PasswordHash = "hash",
            FirstName = "Jane",
            LastName = "Doe",
            SecurityStamp = "stamp"
        });

        var hospitalAppId = await appRepo.CreateAsync(new IdentityApplication
        {
            ApplicationCode = "HOSPITAL",
            ApplicationName = "Hospital System",
            ClientId = "ci_hospital",
            ClientType = "Confidential",
            Audience = "https://hospital.example.com"
        });

        var universityAppId = await appRepo.CreateAsync(new IdentityApplication
        {
            ApplicationCode = "UNIVERSITY",
            ApplicationName = "University Portal",
            ClientId = "ci_university",
            ClientType = "Confidential",
            Audience = "https://university.example.com"
        });

        await service.AssignUserToApplicationAsync(userId, hospitalAppId);
        await service.AssignUserToApplicationAsync(userId, universityAppId);

        return (service, userId, hospitalAppId, universityAppId);
    }

    [Fact]
    public async Task RevokeUserApplicationAsync_RevokingHospitalAccess_DoesNotAffectUniversityAccess()
    {
        var (service, userId, hospitalAppId, universityAppId) = await SeedAsync();

        var revokeResult = await service.RevokeUserApplicationAsync(userId, hospitalAppId, "Employment ended");
        Assert.True(revokeResult.IsSuccess);

        var userApps = await service.GetUserApplicationsAsync(userId);
        Assert.True(userApps.IsSuccess);

        var hospital = userApps.Value.Single(a => a.ApplicationId == hospitalAppId);
        var university = userApps.Value.Single(a => a.ApplicationId == universityAppId);

        Assert.False(hospital.IsActive);
        Assert.NotNull(hospital.RevokedAtUtc);
        Assert.Equal("Employment ended", hospital.RevocationReason);

        Assert.True(university.IsActive);
        Assert.Null(university.RevokedAtUtc);
        Assert.Null(university.RevocationReason);
    }

    [Fact]
    public async Task RevokeUserApplicationAsync_RotatesSecurityStamp_ForRevokedApplicationOnly()
    {
        var (service, userId, hospitalAppId, universityAppId) = await SeedAsync();

        var before = await service.GetUserApplicationsAsync(userId);
        var universityStampBefore = before.Value.Single(a => a.ApplicationId == universityAppId).SecurityStamp;

        await service.RevokeUserApplicationAsync(userId, hospitalAppId, "compromised");

        var after = await service.GetUserApplicationsAsync(userId);
        var universityStampAfter = after.Value.Single(a => a.ApplicationId == universityAppId).SecurityStamp;

        Assert.Equal(universityStampBefore, universityStampAfter);
    }

    [Fact]
    public async Task DisableUserApplicationAsync_DoesNotSetRevocationFields()
    {
        var (service, userId, hospitalAppId, _) = await SeedAsync();

        var result = await service.DisableUserApplicationAsync(userId, hospitalAppId);
        Assert.True(result.IsSuccess);

        var userApps = await service.GetUserApplicationsAsync(userId);
        var hospital = userApps.Value.Single(a => a.ApplicationId == hospitalAppId);

        Assert.False(hospital.IsActive);
        Assert.Null(hospital.RevokedAtUtc);
    }

    [Fact]
    public async Task EnableUserApplicationAsync_ClearsRevocationFields()
    {
        var (service, userId, hospitalAppId, _) = await SeedAsync();
        await service.RevokeUserApplicationAsync(userId, hospitalAppId, "temp suspension");

        var result = await service.EnableUserApplicationAsync(userId, hospitalAppId);
        Assert.True(result.IsSuccess);

        var userApps = await service.GetUserApplicationsAsync(userId);
        var hospital = userApps.Value.Single(a => a.ApplicationId == hospitalAppId);

        Assert.True(hospital.IsActive);
        Assert.Null(hospital.RevokedAtUtc);
        Assert.Null(hospital.RevocationReason);
    }
}
