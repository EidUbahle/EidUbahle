using CentralIdentity.Application.Common.Interfaces;
using CentralIdentity.Application.Options;
using CentralIdentity.Contracts.OAuth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CentralIdentity.Api.Controllers;

/// <summary>
/// OpenID Connect discovery endpoints: /.well-known/openid-configuration and /.well-known/jwks.json.
/// </summary>
[ApiController]
[Route(".well-known")]
public sealed class WellKnownController : ControllerBase
{
    private readonly IJwtKeyProvider _keyProvider;
    private readonly JwtOptions _jwtOptions;

    public WellKnownController(IJwtKeyProvider keyProvider, IOptions<JwtOptions> jwtOptions)
    {
        _keyProvider = keyProvider;
        _jwtOptions = jwtOptions.Value;
    }

    /// <summary>Returns the OpenID Connect discovery document.</summary>
    [HttpGet("openid-configuration")]
    [ProducesResponseType(typeof(OpenIdConfigurationResponse), StatusCodes.Status200OK)]
    public IActionResult GetOpenIdConfiguration()
    {
        var issuer = _jwtOptions.Issuer.TrimEnd('/');
        var response = new OpenIdConfigurationResponse
        {
            Issuer = issuer,
            AuthorizationEndpoint = $"{issuer}/connect/authorize",
            TokenEndpoint = $"{issuer}/connect/token",
            UserinfoEndpoint = $"{issuer}/connect/userinfo",
            JwksUri = $"{issuer}/.well-known/jwks.json",
            ResponseTypesSupported = new[] { "code" }
        };
        return Ok(response);
    }

    /// <summary>Returns the JSON Web Key Set used to verify access token signatures.</summary>
    [HttpGet("jwks.json")]
    [ProducesResponseType(typeof(JsonWebKeySetResponse), StatusCodes.Status200OK)]
    public IActionResult GetJwks()
    {
        using var publicKey = _keyProvider.GetPublicKey();
        var parameters = publicKey.ExportParameters(includePrivateParameters: false);

        var jwk = new JsonWebKey
        {
            KeyType = "RSA",
            Use = "sig",
            Algorithm = _keyProvider.Algorithm,
            KeyId = _keyProvider.KeyId,
            Modulus = Base64UrlEncode(parameters.Modulus!),
            Exponent = Base64UrlEncode(parameters.Exponent!)
        };

        return Ok(new JsonWebKeySetResponse { Keys = new List<JsonWebKey> { jwk } });
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
}
