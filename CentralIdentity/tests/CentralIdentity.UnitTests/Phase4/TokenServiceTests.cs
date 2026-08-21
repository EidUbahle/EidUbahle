using System.IdentityModel.Tokens.Jwt;
using CentralIdentity.Application.Common.Interfaces;
using CentralIdentity.Application.Options;
using CentralIdentity.Application.Services;
using CentralIdentity.Domain.Entities;
using CentralIdentity.Infrastructure.Security;
using CentralIdentity.UnitTests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CentralIdentity.UnitTests.Phase4;

public class TokenServiceTests
{
    private static (TokenService Service, FakeRefreshTokenRepository RefreshRepo, FakeSessionRepository SessionRepo, FakeAuditLogRepository AuditRepo, FakeUserRepository UserRepo, FakeApplicationRepository AppRepo, TestDateTime Clock) CreateSut()
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

        var sut = new TokenService(
            accessTokenService,
            refreshRepo,
            sessionRepo,
            auditRepo,
            userRepo,
            appRepo,
            clock,
            wrappedOptions,
            NullLogger<TokenService>.Instance);

        return (sut, refreshRepo, sessionRepo, auditRepo, userRepo, appRepo, clock);
    }

    private static IdentityUser CreateUser() => new()
    {
        UserId = 42,
        Username = "jdoe",
        Email = "jdoe@example.com",
        PasswordHash = "hash",
        FirstName = "Jane",
        LastName = "Doe",
        IsActive = true,
        SecurityStamp = "stamp-1"
    };

    private static IdentityApplication CreateApplication(long id = 7, string clientId = "ci_hospital") => new()
    {
        ApplicationId = id,
        ApplicationCode = id == 7 ? "HOSPITAL" : "UNIVERSITY",
        ApplicationName = id == 7 ? "Hospital" : "University",
        ClientId = clientId,
        ClientType = "Confidential",
        Audience = $"https://{clientId}.example.com",
        IsActive = true
    };

    [Fact]
    public async Task IssueTokensAsync_AccessTokenIncludesSessionId()
    {
        var (service, _, _, _, _, _, _) = CreateSut();
        var user = CreateUser();
        var app = CreateApplication();

        var (accessToken, _, session) = await service.IssueTokensAsync(user, app, new[] { "profile" }, null, null, CancellationToken.None);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);

        Assert.Equal(session.SessionId.ToString(), jwt.Claims.Single(c => c.Type == "session_id").Value);
    }

    [Fact]
    public async Task IssueTokensAsync_RefreshTokenHasHighEntropy()
    {
        var (service, _, _, _, _, _, _) = CreateSut();

        var (_, refreshToken, _) = await service.IssueTokensAsync(CreateUser(), CreateApplication(), new[] { "profile" }, null, null, CancellationToken.None);

        var tokenBytes = Decode(refreshToken);
        Assert.True(tokenBytes.Length * 8 >= 256);
    }

    [Fact]
    public async Task IssueTokensAsync_StoresOnlyHashedRefreshToken()
    {
        var (service, refreshRepo, _, _, _, _, _) = CreateSut();

        var (_, refreshToken, _) = await service.IssueTokensAsync(CreateUser(), CreateApplication(), new[] { "profile" }, null, null, CancellationToken.None);

        var stored = Assert.Single(refreshRepo.Tokens);
        Assert.NotEqual(refreshToken, stored.TokenHash);
        Assert.Equal(TokenService.HashRefreshToken(refreshToken), stored.TokenHash);
    }

    [Fact]
    public async Task RefreshAsync_RotatesRefreshToken()
    {
        var (service, refreshRepo, _, _, userRepo, appRepo, _) = CreateSut();
        var user = CreateUser();
        var app = CreateApplication();
        await userRepo.CreateAsync(user);
        await appRepo.CreateAsync(app);
        var (_, refreshToken, _) = await service.IssueTokensAsync(user, app, new[] { "profile", "email" }, null, null, CancellationToken.None);

        var (_, nextRefreshToken) = await service.RefreshAsync(refreshToken, app.ClientId, null, null, CancellationToken.None);

        Assert.NotEqual(refreshToken, nextRefreshToken);
        Assert.Equal(2, refreshRepo.Tokens.Count);
        Assert.Single(refreshRepo.Tokens.Where(t => t.RevokedAtUtc is null));
        Assert.Single(refreshRepo.Tokens.Where(t => t.RevokedAtUtc is not null));
    }

    [Fact]
    public async Task RefreshAsync_ReusedRefreshTokenIsRejected_AndFamilyAndSessionAreRevoked_AndAuditLogged()
    {
        var (service, refreshRepo, sessionRepo, auditRepo, userRepo, appRepo, _) = CreateSut();
        var user = CreateUser();
        var app = CreateApplication();
        await userRepo.CreateAsync(user);
        await appRepo.CreateAsync(app);
        var (_, refreshToken, session) = await service.IssueTokensAsync(user, app, new[] { "profile" }, "127.0.0.1", "agent", CancellationToken.None);

        await service.RefreshAsync(refreshToken, app.ClientId, "127.0.0.1", "agent", CancellationToken.None);
        var ex = await Assert.ThrowsAsync<TokenRequestException>(() => service.RefreshAsync(refreshToken, app.ClientId, "127.0.0.1", "agent", CancellationToken.None));

        Assert.Equal("invalid_grant", ex.Error);
        Assert.All(refreshRepo.Tokens, t => Assert.NotNull(t.RevokedAtUtc));
        Assert.NotNull((await sessionRepo.GetByIdAsync(session.SessionId, CancellationToken.None))!.RevokedAtUtc);
        Assert.Contains(auditRepo.Logs, log => log.EventType == "RefreshTokenReuseDetected" && log.Severity == "High");
    }

    [Fact]
    public async Task IssueTokensAsync_CreatesSession()
    {
        var (service, _, sessionRepo, _, _, _, _) = CreateSut();

        var (_, _, session) = await service.IssueTokensAsync(CreateUser(), CreateApplication(), new[] { "profile" }, null, null, CancellationToken.None);
        var stored = await sessionRepo.GetByIdAsync(session.SessionId, CancellationToken.None);

        Assert.NotNull(stored);
        Assert.True(stored!.IsActive);
    }

    [Fact]
    public async Task RefreshAsync_WrongApplicationIsRejected()
    {
        var (service, _, _, _, userRepo, appRepo, _) = CreateSut();
        var user = CreateUser();
        var hospital = CreateApplication(7, "ci_hospital");
        var university = CreateApplication(8, "ci_university");
        await userRepo.CreateAsync(user);
        await appRepo.CreateAsync(hospital);
        await appRepo.CreateAsync(university);
        var (_, refreshToken, _) = await service.IssueTokensAsync(user, hospital, new[] { "profile" }, null, null, CancellationToken.None);

        var ex = await Assert.ThrowsAsync<TokenRequestException>(() => service.RefreshAsync(refreshToken, university.ClientId, null, null, CancellationToken.None));

        Assert.Equal("invalid_grant", ex.Error);
    }

    [Fact]
    public async Task RefreshAsync_ExpiredRefreshTokenIsRejected()
    {
        var (service, refreshRepo, _, _, userRepo, appRepo, clock) = CreateSut();
        var user = CreateUser();
        var app = CreateApplication();
        await userRepo.CreateAsync(user);
        await appRepo.CreateAsync(app);
        var (_, refreshToken, _) = await service.IssueTokensAsync(user, app, new[] { "profile" }, null, null, CancellationToken.None);
        var stored = Assert.Single(refreshRepo.Tokens);
        stored.ExpiresAtUtc = clock.UtcNow.AddMinutes(-1);

        var ex = await Assert.ThrowsAsync<TokenRequestException>(() => service.RefreshAsync(refreshToken, app.ClientId, null, null, CancellationToken.None));

        Assert.Equal("invalid_grant", ex.Error);
    }

    [Fact]
    public async Task RefreshAsync_RevokedRefreshTokenTriggersReuseDetection()
    {
        var (service, refreshRepo, sessionRepo, _, userRepo, appRepo, _) = CreateSut();
        var user = CreateUser();
        var app = CreateApplication();
        await userRepo.CreateAsync(user);
        await appRepo.CreateAsync(app);
        var (_, refreshToken, session) = await service.IssueTokensAsync(user, app, new[] { "profile" }, null, null, CancellationToken.None);
        var stored = Assert.Single(refreshRepo.Tokens);
        stored.RevokedAtUtc = DateTime.UtcNow.AddSeconds(-1);
        stored.RevocationReason = "Manually revoked";

        await Assert.ThrowsAsync<TokenRequestException>(() => service.RefreshAsync(refreshToken, app.ClientId, null, null, CancellationToken.None));

        Assert.NotNull((await sessionRepo.GetByIdAsync(session.SessionId, CancellationToken.None))!.RevokedAtUtc);
    }

    [Fact]
    public async Task RefreshAsync_SecurityStampMismatchIsRejected()
    {
        var (service, _, sessionRepo, auditRepo, userRepo, appRepo, _) = CreateSut();
        var user = CreateUser();
        var app = CreateApplication();
        await userRepo.CreateAsync(user);
        await appRepo.CreateAsync(app);
        var (_, refreshToken, session) = await service.IssueTokensAsync(user, app, new[] { "profile" }, null, null, CancellationToken.None);
        user.SecurityStamp = "stamp-2";
        await userRepo.UpdateAsync(user);

        var ex = await Assert.ThrowsAsync<TokenRequestException>(() => service.RefreshAsync(refreshToken, app.ClientId, null, null, CancellationToken.None));

        Assert.Equal("invalid_grant", ex.Error);
        Assert.Null((await sessionRepo.GetByIdAsync(session.SessionId, CancellationToken.None))!.RevokedAtUtc);
        Assert.Contains(auditRepo.Logs, log => log.EventType == "SecurityStampMismatch");
    }

    private static byte[] Decode(string token)
    {
        var padded = token.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + ((4 - padded.Length % 4) % 4), '=');
        return Convert.FromBase64String(padded);
    }
}
