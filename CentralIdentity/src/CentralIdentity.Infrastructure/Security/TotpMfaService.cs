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
            _encryptionKey = RandomNumberGenerator.GetBytes(32);
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
        var nonce = RandomNumberGenerator.GetBytes(12);
        var tag = new byte[16];
        var data = Encoding.UTF8.GetBytes(plaintext);
        var encrypted = new byte[data.Length];
        using var aes = new AesGcm(_encryptionKey, tag.Length);
        aes.Encrypt(nonce, data, encrypted, tag);
        var result = new byte[nonce.Length + tag.Length + encrypted.Length];
        nonce.CopyTo(result, 0);
        tag.CopyTo(result, nonce.Length);
        encrypted.CopyTo(result, nonce.Length + tag.Length);
        return Convert.ToBase64String(result);
    }

    public string DecryptSecret(string ciphertext)
    {
        var raw = Convert.FromBase64String(ciphertext);
        var nonce = raw[..12];
        var tag = raw[12..28];
        var encrypted = raw[28..];
        var decrypted = new byte[encrypted.Length];
        using var aes = new AesGcm(_encryptionKey, tag.Length);
        aes.Decrypt(nonce, encrypted, tag, decrypted);
        return Encoding.UTF8.GetString(decrypted);
    }
}
