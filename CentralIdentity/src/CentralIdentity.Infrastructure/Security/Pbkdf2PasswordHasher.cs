using CentralIdentity.Application.Common.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace CentralIdentity.Infrastructure.Security;

/// <summary>
/// Hashes user passwords using PBKDF2-HMAC-SHA512 with a per-password random salt
/// (NIST SP 800-63B recommended iteration count). Stored format:
/// v1$&lt;base64 salt&gt;$&lt;base64 hash&gt;$&lt;iterations&gt;
/// </summary>
public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int Iterations = 310_000;
    private const int SaltSize = 32;
    private const int HashSize = 32;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA512;

    public string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            Iterations,
            Algorithm,
            HashSize);
        return $"v1${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}${Iterations}";
    }

    public bool VerifyPassword(string password, string hash)
    {
        try
        {
            var parts = hash.Split('$');
            if (parts.Length != 4 || parts[0] != "v1") return false;
            var salt = Convert.FromBase64String(parts[1]);
            var expectedHash = Convert.FromBase64String(parts[2]);
            if (!int.TryParse(parts[3], out var iterations) || iterations < 1) return false;
            var actualHash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                iterations,
                Algorithm,
                HashSize);
            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
