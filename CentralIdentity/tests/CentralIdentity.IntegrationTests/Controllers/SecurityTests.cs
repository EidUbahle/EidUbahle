using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using CentralIdentity.Application.Common.Interfaces;
using CentralIdentity.Application.Options;
using CentralIdentity.Domain.Entities;
using CentralIdentity.IntegrationTests.Support;
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

    private async Task<(IdentityUser User, IdentityApplication App)> SeedUserAndApplicationAsync()
    {
        var userRepo = _factory.Services.GetRequiredService<IUserRepository>();
        var appRepo = _factory.Services.GetRequiredService<IApplicationRepository>();
        var userAppRepo = _factory.Services.GetRequiredService<IUserApplicationRepository>();

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

        var user = await userRepo.GetByIdAsync(userId);
        var app = await appRepo.GetByIdAsync(appId);
        return (user!, app!);
    }

    private string MintAccessToken(IdentityApplication app, IdentityUser user, bool useValidAudience = true, bool expired = false)
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
            new("client_id", app.ClientId)
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
        var (user, app) = await SeedUserAndApplicationAsync();
        var token = MintAccessToken(app, user);
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
        var (user, app) = await SeedUserAndApplicationAsync();
        var token = MintAccessToken(app, user);
        var tamperedToken = token[..^2] + (token[^2] == 'A' ? "BB" : "AA");

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tamperedToken);

        var response = await client.GetAsync("/connect/userinfo");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GET_userinfo_with_expired_token_returns_401()
    {
        var (user, app) = await SeedUserAndApplicationAsync();
        var expiredToken = MintAccessToken(app, user, expired: true);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", expiredToken);

        var response = await client.GetAsync("/connect/userinfo");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GET_userinfo_with_wrong_audience_returns_401()
    {
        var (user, app) = await SeedUserAndApplicationAsync();
        var wrongAudienceToken = MintAccessToken(app, user, useValidAudience: false);

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
}
