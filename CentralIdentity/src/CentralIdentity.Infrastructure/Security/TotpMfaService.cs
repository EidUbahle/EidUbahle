using System.Security.Cryptography;
using System.Text;
using CentralIdentity.Application.Common.Interfaces;
using CentralIdentity.Application.Options;
using Microsoft.Extensions.Options;
using OtpNet;

namespace CentralIdentity.Infrastructure.Security;

public sealed class TotpMfaService : IMfaService
{
    private readonly string _issuer;
    private readonly byte[] _encryptionKey;

    public TotpMfaService(IOptions<JwtOptions> jwtOptions, IOptions<SecurityOptions> secOpts)
    {
        _issuer = jwtOptions.Value.Issuer;
        var keyStr = secOpts.Value.MfaEncryptionKey;

        if (string.IsNullOrWhiteSpace(keyStr))
        {
            _encryptionKey = SHA256.HashData(Encoding.UTF8.GetBytes(_issuer + "_mfa_key"));
        }
        else
        {
            _encryptionKey = Convert.FromBase64String(keyStr);
            if (_encryptionKey.Length != 32)
            {
                throw new InvalidOperationException("MfaEncryptionKey must be a 32-byte (256-bit) Base64 string.");
            }
        }
    }

    public (string secret, string qrUri) GenerateTotpSetup(string userEmail, string issuer)
    {
        var key = KeyGeneration.GenerateRandomKey(20);
        var base32Secret = Base32Encoding.ToString(key);
        var qrUri = $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(userEmail)}?secret={base32Secret}&issuer={Uri.EscapeDataString(issuer)}&algorithm=SHA1&digits=6&period=30";
        return (base32Secret, qrUri);
    }

    public bool VerifyTotp(string secret, string code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length != 6)
        {
            return false;
        }

        try
        {
            var key = Base32Encoding.ToBytes(secret);
            var totp = new Totp(key);
            return totp.VerifyTotp(code, out _, new VerificationWindow(1, 1));
        }
        catch
        {
            return false;
        }
    }

    public IReadOnlyList<string> GenerateRecoveryCodes(int count = 8)
    {
        var codes = new List<string>(count);
        for (var i = 0; i < count; i++)
        {
            var bytes = RandomNumberGenerator.GetBytes(10);
            codes.Add(Convert.ToHexString(bytes).ToLowerInvariant());
        }

        return codes;
    }

    public string HashRecoveryCode(string code)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(code.ToLowerInvariant()));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public bool VerifyRecoveryCode(string code, string hash)
    {
        var computed = HashRecoveryCode(code);
        return string.Equals(computed, hash, StringComparison.OrdinalIgnoreCase);
    }

    public string EncryptSecret(string plaintext)
    {
        var iv = RandomNumberGenerator.GetBytes(16);
        using var aes = Aes.Create();
        aes.Key = _encryptionKey;
        aes.IV = iv;
        using var encryptor = aes.CreateEncryptor();
        var data = Encoding.UTF8.GetBytes(plaintext);
        var encrypted = encryptor.TransformFinalBlock(data, 0, data.Length);
        var result = new byte[iv.Length + encrypted.Length];
        iv.CopyTo(result, 0);
        encrypted.CopyTo(result, iv.Length);
        return Convert.ToBase64String(result);
    }

    public string DecryptSecret(string ciphertext)
    {
        var raw = Convert.FromBase64String(ciphertext);
        var iv = raw[..16];
        var encrypted = raw[16..];
        using var aes = Aes.Create();
        aes.Key = _encryptionKey;
        aes.IV = iv;
        using var decryptor = aes.CreateDecryptor();
        var decrypted = decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);
        return Encoding.UTF8.GetString(decrypted);
    }
}
