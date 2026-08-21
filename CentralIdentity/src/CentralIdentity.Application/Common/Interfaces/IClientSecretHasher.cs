namespace CentralIdentity.Application.Common.Interfaces;

public interface IClientSecretHasher
{
    string HashSecret(string secret);
    bool VerifySecret(string secret, string hash);
}
