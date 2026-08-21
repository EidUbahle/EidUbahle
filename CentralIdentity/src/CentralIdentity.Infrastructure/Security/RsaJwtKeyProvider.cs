using CentralIdentity.Application.Common.Interfaces;
using CentralIdentity.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace CentralIdentity.Infrastructure.Security;

/// <summary>
/// Loads (or generates, for local development) the RSA key pair used to sign access
/// tokens with RS256. The private key material is never logged, never returned from
/// <see cref="GetPublicKey"/>, and the PEM file backing it must never be committed to
/// source control (see .gitignore: *.pem, *.key, *.pfx).
/// </summary>
public sealed class RsaJwtKeyProvider : IJwtKeyProvider, IDisposable
{
    private readonly RSA _rsa;
    private readonly JwtOptions _options;
    private readonly ILogger<RsaJwtKeyProvider> _logger;

    public RsaJwtKeyProvider(IOptions<JwtOptions> options, ILogger<RsaJwtKeyProvider> logger)
    {
        _options = options.Value;
        _logger = logger;
        _rsa = LoadOrGenerateKey();
    }

    public string KeyId => _options.SigningKeyId;
    public string Algorithm => _options.SigningAlgorithm;

    /// <summary>Returns the RSA instance holding the private key, used only for signing.</summary>
    public RSA GetPrivateKey() => _rsa;

    /// <summary>
    /// Returns a fresh RSA instance containing only the public key parameters, safe to
    /// expose via JWKS or any other external-facing surface.
    /// </summary>
    public RSA GetPublicKey()
    {
        var publicOnly = RSA.Create();
        publicOnly.ImportParameters(_rsa.ExportParameters(includePrivateParameters: false));
        return publicOnly;
    }

    private RSA LoadOrGenerateKey()
    {
        var pemFile = _options.RsaPrivateKeyPemFile;
        if (!string.IsNullOrWhiteSpace(pemFile) && File.Exists(pemFile))
        {
            _logger.LogInformation("Loading RSA signing key from {Path}", pemFile);
            var pem = File.ReadAllText(pemFile);
            var rsa = RSA.Create();
            rsa.ImportFromPem(pem);
            return rsa;
        }

        _logger.LogWarning(
            "RSA signing key file not found or not configured. Generating an ephemeral 3072-bit key. " +
            "This is NOT suitable for production (tokens will become invalid on restart).");

        var generated = RSA.Create(3072);

        if (!string.IsNullOrWhiteSpace(pemFile))
        {
            try
            {
                var directory = Path.GetDirectoryName(pemFile);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                File.WriteAllText(pemFile, generated.ExportPkcs8PrivateKeyPem());
                _logger.LogInformation("Persisted newly generated RSA signing key to {Path}", pemFile);
            }
            catch (IOException ex)
            {
                _logger.LogWarning(ex, "Could not persist generated RSA signing key to {Path}", pemFile);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Could not persist generated RSA signing key to {Path}", pemFile);
            }
        }

        return generated;
    }

    public void Dispose() => _rsa.Dispose();
}
