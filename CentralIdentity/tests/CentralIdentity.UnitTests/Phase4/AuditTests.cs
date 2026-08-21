using CentralIdentity.Application.Options;
using CentralIdentity.Application.Services;
using CentralIdentity.Domain.Entities;
using CentralIdentity.Infrastructure.Security;
using CentralIdentity.UnitTests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CentralIdentity.UnitTests.Phase4;

public class AuditTests
{
    [Fact]
    public async Task AuditLogs_DoNotContainPlainRefreshToken()
    {
        var wrappedOptions = Options.Create(new JwtOptions
        {
            Issuer = "https://identity.example.com",
            AccessTokenLifetimeMinutes = 10,
            RefreshTokenLifetimeDays = 30,
            SigningKeyId = "test-key-1",
            SigningAlgorithm = "RS256",
            RsaPrivateKeyPemFile = string.Empty
        });

        var keyProvider = new RsaJwtKeyProvider(wrappedOptions, NullLogger<RsaJwtKeyProvider>.Instance);
        var accessTokenService = new JwtAccessTokenService(keyProvider, wrappedOptions);
        var refreshRepo = new FakeRefreshTokenRepository();
        var sessionRepo = new FakeSessionRepository();
        var auditRepo = new FakeAuditLogRepository();
        var userRepo = new FakeUserRepository();
        var appRepo = new FakeApplicationRepository();
        var clock = new TestDateTime();
        var service = new TokenService(accessTokenService, refreshRepo, sessionRepo, auditRepo, userRepo, appRepo, clock, wrappedOptions, NullLogger<TokenService>.Instance);
        var user = new IdentityUser { UserId = 42, Username = "jdoe", Email = "jdoe@example.com", PasswordHash = "hash", FirstName = "Jane", LastName = "Doe", IsActive = true, SecurityStamp = "stamp" };
        var app = new IdentityApplication { ApplicationId = 7, ApplicationCode = "HOSPITAL", ApplicationName = "Hospital", ClientId = "ci_hospital", ClientType = "Confidential", Audience = "https://hospital.example.com", IsActive = true };

        var (_, refreshToken, _) = await service.IssueTokensAsync(user, app, new[] { "profile" }, "127.0.0.1", "agent", CancellationToken.None);

        Assert.DoesNotContain(auditRepo.Logs, log => log.Description.Contains(refreshToken, StringComparison.Ordinal));
        Assert.DoesNotContain(auditRepo.Logs, log => log.Description.Contains(TokenService.HashRefreshToken(refreshToken), StringComparison.OrdinalIgnoreCase));
    }
}
