using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;

namespace Backend.Utils
{
    public sealed class AudioStorageResult
    {
        public string RelativePath { get; init; } = "";
        public string FullPath { get; init; } = "";
        public string Extension { get; init; } = "";
        public string Hash { get; init; } = "";
        public long SizeBytes { get; init; }
    }

    public static class AudioStorage
    {
        private static readonly Lazy<IAudioStorageProvider> ProviderLazy =
            new(() => AudioStorageProviderFactory.Create());

        public static IAudioStorageProvider Provider => ProviderLazy.Value;

        public static Task<AudioStorageResult> SaveAsync(IFormFile file, CancellationToken ct = default)
            => Provider.SaveAsync(file, ct);

        public static void Delete(string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                return;

            Provider.Delete(relativePath);
        }

        public static string ResolveFullPath(string relativePath) => Provider.ResolveFullPath(relativePath);

        public static string ComputeSha256File(string filePath)
        {
            using var sha = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            var hash = sha.ComputeHash(stream);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}
