using System;
using System.Configuration;
using System.Security.Cryptography;

namespace WamoApp
{
    public static class PasswordHasher
    {
        public static string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("Password is required.");
            var iterations = GetIterations();
            var salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            byte[] hash;
            using (var deriveBytes = new Rfc2898DeriveBytes(password, salt, iterations))
            {
                hash = deriveBytes.GetBytes(32);
            }
            return string.Format("PBKDF2$sha1${0}${1}${2}", iterations, Convert.ToBase64String(salt), Convert.ToBase64String(hash));
        }

        public static bool VerifyPassword(string password, string storedHash)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(storedHash)) return false;
            var parts = storedHash.Split('$');
            if (parts.Length != 5 || !parts[0].Equals("PBKDF2", StringComparison.OrdinalIgnoreCase)) return false;
            var iterations = int.Parse(parts[2]);
            var salt = Convert.FromBase64String(parts[3]);
            var expected = Convert.FromBase64String(parts[4]);
            byte[] actual;
            using (var deriveBytes = new Rfc2898DeriveBytes(password, salt, iterations))
            {
                actual = deriveBytes.GetBytes(expected.Length);
            }
            return FixedTimeEquals(actual, expected);
        }

        public static bool MeetsPasswordPolicy(string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 10) return false;
            var upper = false; var lower = false; var number = false; var special = false;
            foreach (var ch in password)
            {
                if (char.IsUpper(ch)) upper = true; else if (char.IsLower(ch)) lower = true; else if (char.IsDigit(ch)) number = true; else special = true;
            }
            return upper && lower && number && special;
        }

        private static int GetIterations() { int v; return int.TryParse(ConfigurationManager.AppSettings["PasswordHashIterations"], out v) ? v : 120000; }

        private static bool FixedTimeEquals(byte[] left, byte[] right)
        {
            if (left == null || right == null || left.Length != right.Length) return false;
            var diff = 0;
            for (var i = 0; i < left.Length; i++)
            {
                diff |= left[i] ^ right[i];
            }
            return diff == 0;
        }
    }
}
