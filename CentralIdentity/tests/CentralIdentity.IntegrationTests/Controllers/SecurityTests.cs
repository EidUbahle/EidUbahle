using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using CentralIdentity.Application.Common.Interfaces;
using CentralIdentity.Application.Options;
using CentralIdentity.Domain.Entities;
using CentralIdentity.IntegrationTests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace CentralIdentity.IntegrationTests.Controllers;

/// <summary>
/// Verifies /connect/userinfo (protected by the JWT bearer scheme configured in
/// AddJwtAuthentication) correctly rejects missing, tampered, expired, and wrong-audience
/// tokens with 401, and accepts a legitimately issued token.
/// </summary>
public class SecurityTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SecurityTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services => services.ReplaceRepositoriesWithFakes());
        });
    }

    private async Task<(IdentityUser User, IdentityApplication App, IdentitySession Session)> SeedUserAndApplicationAsync()
    {
        var userRepo = _factory.Services.GetRequiredService<IUserRepository>();
        var appRepo = _factory.Services.GetRequiredService<IApplicationRepository>();
        var userAppRepo = _factory.Services.GetRequiredService<IUserApplicationRepository>();
        var sessionRepo = _factory.Services.GetRequiredService<ISessionRepository>();

        var userId = await userRepo.CreateAsync(new IdentityUser
        {
            Username = "security-test-user",
            Email = "security-test@example.com",
            PasswordHash = "hash",
            FirstName = "Sec",
            LastName = "Tester",
            IsActive = true,
            SecurityStamp = "stamp"
        });

        var appId = await appRepo.CreateAsync(new IdentityApplication
        {
            ApplicationCode = "SECTEST",
            ApplicationName = "Security Test App",
            ClientId = "ci_sectest",
            ClientType = "Confidential",
            Audience = "https://sectest.example.com",
            IsActive = true
        });

        await userAppRepo.AssignAsync(new IdentityUserApplication
        {
            UserId = userId,
            ApplicationId = appId,
            IsActive = true,
            SecurityStamp = "stamp"
        });

        var session = new IdentitySession
        {
            SessionId = Guid.NewGuid(),
            UserId = userId,
            ApplicationId = appId,
            ClientId = "ci_sectest",
            CreatedAtUtc = DateTime.UtcNow,
            LastActivityAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(10),
            SecurityStamp = "stamp",
            IsActive = true
        };
        await sessionRepo.CreateAsync(session, CancellationToken.None);

        var user = await userRepo.GetByIdAsync(userId);
        var app = await appRepo.GetByIdAsync(appId);
        return (user!, app!, session);
    }

    private async Task SeedAdminRoleAsync(long userId, long applicationId)
    {
        var roleRepo = _factory.Services.GetRequiredService<IRoleRepository>();
        var userRoleRepo = _factory.Services.GetRequiredService<IUserRoleRepository>();

        var roleId = await roleRepo.CreateAsync(new IdentityRole
        {
            ApplicationId = applicationId,
            RoleCode = "admin",
            RoleName = "Administrator",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        }, CancellationToken.None);

        await userRoleRepo.AssignAsync(new IdentityUserRole
        {
            UserId = userId,
            ApplicationId = applicationId,
            RoleId = roleId,
            AssignedAtUtc = DateTime.UtcNow,
            IsActive = true
        }, CancellationToken.None);
    }

    private string MintAccessToken(IdentityApplication app, IdentityUser user, IdentitySession session, bool useValidAudience = true, bool expired = false)
    {
        var keyProvider = _factory.Services.GetRequiredService<IJwtKeyProvider>();
        var jwtOptions = _factory.Services.GetRequiredService<IOptions<JwtOptions>>().Value;

        // For an "already expired" token, anchor both NotBefore and Expires safely in the past
        // (Expires must always be strictly after NotBefore, so we can't just push Expires back
        // while leaving NotBefore at "now").
        var notBefore = expired ? DateTime.UtcNow.AddHours(-1) : DateTime.UtcNow;
        var expires = expired ? DateTime.UtcNow.AddMinutes(-30) : DateTime.UtcNow.AddMinutes(jwtOptions.AccessTokenLifetimeMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new("client_id", app.ClientId),
            new("session_id", session.SessionId.ToString())
        };

        var rsaKey = new RsaSecurityKey(keyProvider.GetPrivateKey()) { KeyId = keyProvider.KeyId };
        var signingCredentials = new SigningCredentials(rsaKey, SecurityAlgorithms.RsaSha256);

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = jwtOptions.Issuer,
            Audience = useValidAudience ? app.Audience : "https://wrong-audience.example.com",
            NotBefore = notBefore,
            IssuedAt = notBefore,
            Expires = expires,
            SigningCredentials = signingCredentials
        };

        var handler = new JwtSecurityTokenHandler();
        return handler.WriteToken(handler.CreateJwtSecurityToken(descriptor));
    }

    [Fact]
    public async Task GET_userinfo_without_token_returns_401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/connect/userinfo");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GET_userinfo_with_valid_token_returns_200_with_user_claims()
    {
        var (user, app, session) = await SeedUserAndApplicationAsync();
        var token = MintAccessToken(app, user, session);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/connect/userinfo");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal(user.UserId.ToString(), body.GetProperty("sub").GetString());
        Assert.Equal(user.Username, body.GetProperty("preferred_username").GetString());
    }

    [Fact]
    public async Task GET_userinfo_with_tampered_token_returns_401()
    {
        var (user, app, session) = await SeedUserAndApplicationAsync();
        var token = MintAccessToken(app, user, session);
        var tamperedToken = token[..^2] + (token[^2] == 'A' ? "BB" : "AA");

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tamperedToken);

        var response = await client.GetAsync("/connect/userinfo");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GET_userinfo_with_expired_token_returns_401()
    {
        var (user, app, session) = await SeedUserAndApplicationAsync();
        var expiredToken = MintAccessToken(app, user, session, expired: true);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", expiredToken);

        var response = await client.GetAsync("/connect/userinfo");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GET_userinfo_with_wrong_audience_returns_401()
    {
        var (user, app, session) = await SeedUserAndApplicationAsync();
        var wrongAudienceToken = MintAccessToken(app, user, session, useValidAudience: false);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", wrongAudienceToken);

        var response = await client.GetAsync("/connect/userinfo");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GET_userinfo_with_malformed_bearer_token_returns_401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-jwt-at-all");

        var response = await client.GetAsync("/connect/userinfo");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GET_userinfo_with_revoked_session_returns_401()
    {
        var (user, app, session) = await SeedUserAndApplicationAsync();
        var sessionRepo = _factory.Services.GetRequiredService<ISessionRepository>();
        await sessionRepo.RevokeAsync(session.SessionId, "test", CancellationToken.None);

        var token = MintAccessToken(app, user, session);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/connect/userinfo");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GET_users_without_token_returns_401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GET_users_with_non_admin_token_returns_403()
    {
        var (user, app, session) = await SeedUserAndApplicationAsync();
        var token = MintAccessToken(app, user, session);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GET_users_with_admin_token_returns_200()
    {
        var (user, app, session) = await SeedUserAndApplicationAsync();
        await SeedAdminRoleAsync(user.UserId, app.ApplicationId);

        var token = MintAccessToken(app, user, session);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GET_authorize_in_production_rejects_query_user_shortcut()
    {
        var productionFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.ConfigureServices(services => services.ReplaceRepositoriesWithFakes());
        });

        var userRepo = productionFactory.Services.GetRequiredService<IUserRepository>();
        var appRepo = productionFactory.Services.GetRequiredService<IApplicationRepository>();
        var userAppRepo = productionFactory.Services.GetRequiredService<IUserApplicationRepository>();

        var userId = await userRepo.CreateAsync(new IdentityUser
        {
            Username = "prod-user",
            Email = "prod-user@example.com",
            PasswordHash = "hash",
            FirstName = "Prod",
            LastName = "User",
            IsActive = true,
            SecurityStamp = "stamp"
        });

        var appId = await appRepo.CreateAsync(new IdentityApplication
        {
            ApplicationCode = "PRODTEST",
            ApplicationName = "Production Test App",
            ClientId = "ci_prodtest",
            ClientType = "Public",
            Audience = "https://prodtest.example.com",
            AllowedRedirectUris = "https://client.example.com/callback",
            IsActive = true
        });

        await userAppRepo.AssignAsync(new IdentityUserApplication
        {
            UserId = userId,
            ApplicationId = appId,
            IsActive = true,
            SecurityStamp = "stamp"
        });

        var client = productionFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("/connect/authorize?response_type=code&client_id=ci_prodtest&redirect_uri=https://client.example.com/callback&user_id=1");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal("login_required", body.GetProperty("error").GetString());
    }
}
