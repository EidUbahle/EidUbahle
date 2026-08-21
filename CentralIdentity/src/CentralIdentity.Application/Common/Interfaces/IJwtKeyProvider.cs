using System.Security.Cryptography;

namespace CentralIdentity.Application.Common.Interfaces;

public interface IJwtKeyProvider
{
    /// <summary>Returns the RSA instance holding the private key, used only for signing.</summary>
    RSA GetPrivateKey();

    /// <summary>
    /// Returns the RSA instance containing only the public key parameters, safe to expose via
    /// JWKS or any other external-facing surface. The returned instance is owned by the provider
    /// implementation — callers must NOT dispose it.
    /// </summary>
    RSA GetPublicKey();
    string KeyId { get; }
    string Algorithm { get; }
}
