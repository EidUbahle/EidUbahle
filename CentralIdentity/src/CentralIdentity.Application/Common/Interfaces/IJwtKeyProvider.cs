using System.Security.Cryptography;

namespace CentralIdentity.Application.Common.Interfaces;

public interface IJwtKeyProvider
{
    RSA GetPrivateKey();
    RSA GetPublicKey();
    string KeyId { get; }
    string Algorithm { get; }
}
