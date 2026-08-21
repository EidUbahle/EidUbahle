namespace CentralIdentity.Application.Common.Interfaces;

public interface IMfaService
{
    (string secret, string qrUri) GenerateTotpSetup(string userEmail, string issuer);
    bool VerifyTotp(string secret, string code);
    IReadOnlyList<string> GenerateRecoveryCodes(int count = 8);
    string HashRecoveryCode(string code);
    bool VerifyRecoveryCode(string code, string hash);
    string EncryptSecret(string plaintext);
    string DecryptSecret(string ciphertext);
}
