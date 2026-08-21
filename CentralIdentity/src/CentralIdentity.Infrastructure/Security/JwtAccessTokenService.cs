using CentralIdentity.Application.Common.Interfaces;
using CentralIdentity.Application.Options;
using CentralIdentity.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CentralIdentity.Infrastructure.Security;

/// <summary>
/// Issues RS256-signed (asymmetric) JWT access tokens. Never uses a symmetric HS256 key.
/// </summary>
public sealed class JwtAccessTokenService : IAccessTokenService
{
    private readonly IJwtKeyProvider _keyProvider;
    private readonly JwtOptions _options;

    public JwtAccessTokenService(IJwtKeyProvider keyProvider, IOptions<JwtOptions> options)
    {
        _keyProvider = keyProvider;
        _options = options.Value;
    }

    public string CreateAccessToken(IdentityUser user, IdentityApplication application, IEnumerable<string> scopes, Guid sessionId)
    {
        var now = DateTime.UtcNow;
        var expiry = now.AddMinutes(_options.AccessTokenLifetimeMinutes);
        var jti = Guid.NewGuid().ToString("N");

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new(JwtRegisteredClaimNames.Jti, jti),
            new(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new("client_id", application.ClientId),
            new("application_id", application.ApplicationId.ToString()),
            new("application_code", application.ApplicationCode),
            new("session_id", sessionId.ToString())
        };
        foreach (var scope in scopes)
            claims.Add(new Claim("scope", scope));

        var rsaKey = new RsaSecurityKey(_keyProvider.GetPrivateKey()) { KeyId = _keyProvider.KeyId };
        var signingCredentials = new SigningCredentials(rsaKey, SecurityAlgorithms.RsaSha256);

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = _options.Issuer,
            Audience = application.Audience,
            NotBefore = now,
            IssuedAt = now,
            Expires = expiry,
            SigningCredentials = signingCredentials
        };

        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateJwtSecurityToken(descriptor);
        return handler.WriteToken(token);
    }
}
