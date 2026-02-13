using Microsoft.AspNetCore.Http;

namespace Backend.Utils
{
    public interface IAudioStorageProvider
    {
        Task<AudioStorageResult> SaveAsync(IFormFile file, CancellationToken ct = default);
        string ResolveFullPath(string relativePath);
        void Delete(string relativePath);
    }

    public sealed class LocalAudioStorageProvider : IAudioStorageProvider
    {
        public async Task<AudioStorageResult> SaveAsync(IFormFile file, CancellationToken ct = default)
        {
            var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant() ?? "";
            if (ext.StartsWith("."))
                ext = ext[1..];

            var now = DateTime.UtcNow;
            var relativeDir = Path.Combine("audio", now.ToString("yyyy"), now.ToString("MM"), now.ToString("dd"));
            var fileName = $"{Guid.NewGuid():N}.{ext}";
            var relativePath = Path.Combine(relativeDir, fileName).Replace("\\", "/");
            var fullPath = ResolveFullPath(relativePath);
            var fullDir = Path.GetDirectoryName(fullPath) ?? Directory.GetCurrentDirectory();
            Directory.CreateDirectory(fullDir);

            await using (var stream = File.Create(fullPath))
            {
                await file.CopyToAsync(stream, ct);
            }

            var hash = AudioStorage.ComputeSha256File(fullPath);
            return new AudioStorageResult
            {
                RelativePath = relativePath,
                FullPath = fullPath,
                Extension = ext,
                Hash = hash,
                SizeBytes = file.Length
            };
        }

        public string ResolveFullPath(string relativePath)
        {
            var root = AppConfig.AUDIO_STORAGE_ROOT;
            if (string.IsNullOrWhiteSpace(root))
                root = "storage/audio";

            var normalized = relativePath.Replace("\\", "/").TrimStart('/');
            var rootNormalized = root.Replace("\\", "/").TrimEnd('/');
            if (rootNormalized.EndsWith("/audio", StringComparison.OrdinalIgnoreCase) &&
                normalized.StartsWith("audio/", StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring("audio/".Length);
            }

            return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), root, normalized));
        }

        public void Delete(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                return;

            var fullPath = ResolveFullPath(relativePath);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }
    }

    public static class AudioStorageProviderFactory
    {
        public static IAudioStorageProvider Create()
        {
            var provider = AppConfig.AUDIO_STORAGE_PROVIDER?.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(provider) || provider == "local" || provider == "fs")
                return new LocalAudioStorageProvider();

            Console.WriteLine($"[AudioStorage] Provider '{provider}' no soportado. Usando local.");
            return new LocalAudioStorageProvider();
        }
    }
}
