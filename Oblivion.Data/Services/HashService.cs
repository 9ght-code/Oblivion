using System.Security.Cryptography;

namespace Oblivion.Data.Services
{
    public static class HashService
    {
        public static async Task<string> ComputeSHA256(string filePath)
        {
            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            var hashBytes = await sha256.ComputeHashAsync(stream);

            return ToBase64(hashBytes);
        }

        public static string ComputeSHA256NoAsync(string filePath)
        {
            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            var hashBytes = sha256.ComputeHash(stream);

            return ToBase64(hashBytes);
        }

        private static string ToBase64(byte[] bytes)
        {
            return Convert.ToBase64String(bytes);
        }
    }
}
