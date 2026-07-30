using System;
using System.Security.Cryptography;
using System.Text;

namespace EidUbahle.Infrastructure.Security
{
    /// <summary>
    /// PBKDF2-SHA256 password hashing with 100,000 iterations.
    /// Also provides TOTP (RFC 6238) generation and verification for 2FA.
    /// </summary>
    public static class PasswordService
    {
        private const int SaltSize = 32;
        private const int HashSize = 32;
        private const int Iterations = 100_000;

        public static (string hash, string salt) HashPassword(string password)
        {
            var saltBytes = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(saltBytes);

            var hashBytes = Pbkdf2(password, saltBytes);
            return (Convert.ToBase64String(hashBytes), Convert.ToBase64String(saltBytes));
        }

        public static bool VerifyPassword(string password, string storedHash, string storedSalt)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash) || string.IsNullOrEmpty(storedSalt))
                return false;

            var saltBytes = Convert.FromBase64String(storedSalt);
            var expectedHash = Pbkdf2(password, saltBytes);
            var actualHash = Convert.FromBase64String(storedHash);
            return ConstantTimeEquals(expectedHash, actualHash);
        }

        private static byte[] Pbkdf2(string password, byte[] salt)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(
                password,
                salt,
                Iterations,
                HashAlgorithmName.SHA256))
            {
                return pbkdf2.GetBytes(HashSize);
            }
        }

        private static bool ConstantTimeEquals(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }

        // ── TOTP (RFC 6238) ──────────────────────────────────────────────
        public static string GenerateTotpSecret()
        {
            var bytes = new byte[20];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(bytes);
            return Base32Encode(bytes);
        }

        public static bool VerifyTotp(string secret, string code)
        {
            if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(code)) return false;
            long timeStep = ToUnixSeconds(DateTime.UtcNow) / 30;
            // Check current window and ±1 for clock skew
            for (long t = timeStep - 1; t <= timeStep + 1; t++)
            {
                if (GenerateTotp(secret, t) == code) return true;
            }
            return false;
        }

        private static string GenerateTotp(string secret, long timeStep)
        {
            var key = Base32Decode(secret);
            var msg = BitConverter.GetBytes(timeStep);
            if (BitConverter.IsLittleEndian) Array.Reverse(msg);
            using (var hmac = new HMACSHA1(key))
            {
                var hash = hmac.ComputeHash(msg);
                int offset = hash[hash.Length - 1] & 0x0F;
                int code = ((hash[offset] & 0x7F) << 24)
                         | ((hash[offset + 1] & 0xFF) << 16)
                         | ((hash[offset + 2] & 0xFF) << 8)
                         | (hash[offset + 3] & 0xFF);
                return (code % 1_000_000).ToString("D6");
            }
        }

        private static long ToUnixSeconds(DateTime dt) =>
            (long)(dt.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;

        private static readonly string Base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

        private static string Base32Encode(byte[] data)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < data.Length; i += 5)
            {
                var block = new byte[8];
                int count = Math.Min(5, data.Length - i);
                for (int j = 0; j < count; j++) block[j] = data[i + j];
                sb.Append(Base32Chars[(block[0] >> 3) & 0x1F]);
                sb.Append(Base32Chars[((block[0] & 0x07) << 2) | ((block[1] >> 6) & 0x03)]);
                if (count > 1) sb.Append(Base32Chars[(block[1] >> 1) & 0x1F]);
                if (count > 1) sb.Append(Base32Chars[((block[1] & 0x01) << 4) | ((block[2] >> 4) & 0x0F)]);
                if (count > 2) sb.Append(Base32Chars[((block[2] & 0x0F) << 1) | ((block[3] >> 7) & 0x01)]);
                if (count > 3) sb.Append(Base32Chars[(block[3] >> 2) & 0x1F]);
                if (count > 3) sb.Append(Base32Chars[((block[3] & 0x03) << 3) | ((block[4] >> 5) & 0x07)]);
                if (count > 4) sb.Append(Base32Chars[block[4] & 0x1F]);
            }
            return sb.ToString();
        }

        private static byte[] Base32Decode(string s)
        {
            s = s.TrimEnd('=').ToUpper();
            var output = new byte[s.Length * 5 / 8];
            int bitIndex = 0, inputIndex = 0, outputIndex = 0;
            int currentByte = 0;
            while (outputIndex < output.Length)
            {
                var currentChar = Base32Chars.IndexOf(s[inputIndex++]);
                if (currentChar < 0) break;
                var bitsRemaining = Math.Min(5, 8 - bitIndex);
                currentByte <<= bitsRemaining;
                currentByte |= currentChar >> (5 - bitsRemaining);
                bitIndex += bitsRemaining;
                if (bitIndex >= 8)
                {
                    output[outputIndex++] = (byte)currentByte;
                    bitIndex -= 8;
                    currentByte = currentChar & ((1 << bitIndex) - 1);
                }
                else
                {
                    currentByte = (currentByte << (5 - bitsRemaining)) | (currentChar & ((1 << (5 - bitsRemaining)) - 1));
                }
            }
            return output;
        }
    }
}
