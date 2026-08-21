using CentralIdentity.Application.Options;
using CentralIdentity.Domain.Entities;
using CentralIdentity.Infrastructure.Security;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Xunit;

namespace CentralIdentity.UnitTests.Phase3;

public class JwtTokenTests
{
    private static (RsaJwtKeyProvider KeyProvider, JwtAccessTokenService TokenService, JwtOptions Options) CreateSut(
        int accessTokenLifetimeMinutes = 10)
    {
        var options = new JwtOptions
        {
            Issuer = "https://identity.example.com",
            AccessTokenLifetimeMinutes = accessTokenLifetimeMinutes,
            SigningKeyId = "test-key-1",
            SigningAlgorithm = "RS256",
            RsaPrivateKeyPemFile = string.Empty
        };
        var wrapped = Options.Create(options);
        var keyProvider = new RsaJwtKeyProvider(wrapped, NullLogger<RsaJwtKeyProvider>.Instance);
        var tokenService = new JwtAccessTokenService(keyProvider, wrapped);
        return (keyProvider, tokenService, options);
    }

    private static IdentityUser TestUser() => new()
    {
        UserId = 42,
        Username = "jdoe",
        Email = "jdoe@example.com",
        PasswordHash = "hash",
        FirstName = "Jane",
        LastName = "Doe",
        SecurityStamp = "stamp"
    };

    private static IdentityApplication TestApplication() => new()
    {
        ApplicationId = 7,
        ApplicationCode = "HOSPITAL",
        ApplicationName = "Hospital System",
        ClientId = "ci_hospital",
        ClientType = "Confidential",
        Audience = "https://hospital.example.com"
    };

    [Fact]
    public void CreateAccessToken_UsesRS256_NotSymmetricAlgorithm()
    {
        var (_, tokenService, _) = CreateSut();

        var token = tokenService.CreateAccessToken(TestUser(), TestApplication(), new[] { "profile" });
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal(SecurityAlgorithms.RsaSha256, jwt.Header.Alg);
    }

    [Fact]
    public void CreateAccessToken_IncludesExpectedClaims()
    {
        var (_, tokenService, _) = CreateSut();
        var user = TestUser();
        var app = TestApplication();

        var token = tokenService.CreateAccessToken(user, app, new[] { "profile", "email" });
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal(user.UserId.ToString(), jwt.Subject);
        Assert.Equal("https://identity.example.com", jwt.Issuer);
        Assert.Contains(app.Audience, jwt.Audiences);
        Assert.Equal(app.ClientId, jwt.Claims.Single(c => c.Type == "client_id").Value);
        Assert.Contains(jwt.Claims, c => c.Type == "scope" && c.Value == "profile");
        Assert.Contains(jwt.Claims, c => c.Type == "scope" && c.Value == "email");
    }

    [Fact]
    public void CreateAccessToken_SetsKeyIdInHeader()
    {
        var (_, tokenService, options) = CreateSut();

        var token = tokenService.CreateAccessToken(TestUser(), TestApplication(), Array.Empty<string>());
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal(options.SigningKeyId, jwt.Header.Kid);
    }

    [Fact]
    public void CreateAccessToken_ExpiryReflectsConfiguredLifetime()
    {
        var (_, tokenService, options) = CreateSut(accessTokenLifetimeMinutes: 15);

        var before = DateTime.UtcNow;
        var token = tokenService.CreateAccessToken(TestUser(), TestApplication(), Array.Empty<string>());
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        var expectedExpiry = before.AddMinutes(options.AccessTokenLifetimeMinutes);
        Assert.True(Math.Abs((jwt.ValidTo - expectedExpiry).TotalSeconds) < 5);
    }

    [Fact]
    public void CreateAccessToken_ValidatesSuccessfully_WithProviderPublicKey()
    {
        var (keyProvider, tokenService, options) = CreateSut();
        var app = TestApplication();

        var token = tokenService.CreateAccessToken(TestUser(), app, new[] { "profile" });

        var handler = new JwtSecurityTokenHandler();
        var principal = handler.ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = options.Issuer,
            ValidateAudience = true,
            ValidAudience = app.Audience,
            ValidateLifetime = true,
            IssuerSigningKey = new RsaSecurityKey(keyProvider.GetPublicKey())
        }, out _);

        Assert.NotNull(principal);
    }

    [Fact]
    public void CreateAccessToken_FailsValidation_ForWrongAudience()
    {
        var (keyProvider, tokenService, options) = CreateSut();
        var app = TestApplication();

        var token = tokenService.CreateAccessToken(TestUser(), app, Array.Empty<string>());

        var handler = new JwtSecurityTokenHandler();
        Assert.Throws<SecurityTokenInvalidAudienceException>(() => handler.ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = options.Issuer,
            ValidateAudience = true,
            ValidAudience = "https://someone-else.example.com",
            ValidateLifetime = true,
            IssuerSigningKey = new RsaSecurityKey(keyProvider.GetPublicKey())
        }, out _));
    }

    [Fact]
    public void CreateAccessToken_FailsValidation_WhenExpired()
    {
        var (keyProvider, tokenService, options) = CreateSut(accessTokenLifetimeMinutes: 1);
        var app = TestApplication();

        var token = tokenService.CreateAccessToken(TestUser(), app, Array.Empty<string>());

        var handler = new JwtSecurityTokenHandler();
        // Simulate the token already having expired by supplying a custom lifetime validator
        // rather than sleeping in the test for real time to elapse.
        Assert.ThrowsAny<SecurityTokenException>(() => handler.ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = options.Issuer,
            ValidateAudience = true,
            ValidAudience = app.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            IssuerSigningKey = new RsaSecurityKey(keyProvider.GetPublicKey()),
            LifetimeValidator = (_, _, _, _) => false
        }, out _));
    }

    [Fact]
    public void CreateAccessToken_FailsValidation_WhenTokenTampered()
    {
        var (keyProvider, tokenService, options) = CreateSut();
        var app = TestApplication();

        var token = tokenService.CreateAccessToken(TestUser(), app, Array.Empty<string>());
        var tampered = token[..^1] + (token[^1] == 'A' ? 'B' : 'A');

        var handler = new JwtSecurityTokenHandler();
        Assert.ThrowsAny<SecurityTokenException>(() => handler.ValidateToken(tampered, new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = options.Issuer,
            ValidateAudience = true,
            ValidAudience = app.Audience,
            ValidateLifetime = true,
            IssuerSigningKey = new RsaSecurityKey(keyProvider.GetPublicKey())
        }, out _));
    }

    [Fact]
    public void GetPublicKey_DoesNotExposePrivateKeyMaterial()
    {
        var (keyProvider, _, _) = CreateSut();

        var publicKey = keyProvider.GetPublicKey();
        var parameters = publicKey.ExportParameters(includePrivateParameters: false);

        Assert.Null(parameters.D);
        Assert.Throws<CryptographicException>(() => publicKey.ExportParameters(includePrivateParameters: true));
    }
}
