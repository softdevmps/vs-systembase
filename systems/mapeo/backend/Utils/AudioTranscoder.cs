using System.Diagnostics;

namespace Backend.Utils
{
    public sealed class AudioTranscodeResult
    {
        public string RelativePath { get; init; } = "";
        public string Format { get; init; } = "";
        public string Hash { get; init; } = "";
    }

    public static class AudioTranscoder
    {
        public static async Task<AudioTranscodeResult?> TranscodeIfEnabledAsync(
            string relativePath,
            string? currentFormat,
            CancellationToken ct = default
        )
        {
            if (!AppConfig.AUDIO_TRANSCODE_ENABLED)
                return null;

            if (AudioStorage.Provider is not LocalAudioStorageProvider)
                return null;

            var targetFormat = (AppConfig.AUDIO_TRANSCODE_FORMAT ?? "opus").Trim().ToLowerInvariant();
            var current = (currentFormat ?? "").Trim().ToLowerInvariant();
            if (!string.IsNullOrWhiteSpace(current) && current == targetFormat)
                return null;

            var inputPath = AudioStorage.ResolveFullPath(relativePath);
            if (!File.Exists(inputPath))
                return null;

            var outputRelative = Path.ChangeExtension(relativePath.Replace("\\", "/"), targetFormat);
            var outputPath = AudioStorage.ResolveFullPath(outputRelative ?? relativePath);
            var outputDir = Path.GetDirectoryName(outputPath) ?? Directory.GetCurrentDirectory();
            Directory.CreateDirectory(outputDir);

            var codec = ResolveCodec(targetFormat);
            var bitrate = (AppConfig.AUDIO_TRANSCODE_BITRATE ?? "32k").Trim();
            var filter = (AppConfig.AUDIO_TRANSCODE_FILTER ?? "").Trim();
            var sampleRate = (AppConfig.AUDIO_TRANSCODE_SAMPLE_RATE ?? "").Trim();
            if (string.IsNullOrWhiteSpace(sampleRate))
            {
                sampleRate = targetFormat switch
                {
                    "mp3" => "44100",
                    "m4a" => "44100",
                    "aac" => "44100",
                    "wav" => "44100",
                    _ => "16000"
                };
            }

            var args = new List<string>
            {
                "-y",
                "-i", Quote(inputPath),
                "-vn",
                "-c:a", codec,
                "-ac", "1",
                "-ar", sampleRate
            };
            if (!string.IsNullOrWhiteSpace(filter))
            {
                args.Add("-af");
                args.Add($"\"{filter}\"");
            }
            if (!string.IsNullOrWhiteSpace(bitrate))
            {
                args.Add("-b:a");
                args.Add(bitrate);
            }
            args.Add(Quote(outputPath));

            var ffmpeg = AppConfig.FFMPEG_PATH ?? "ffmpeg";
            var psi = new ProcessStartInfo
            {
                FileName = ffmpeg,
                Arguments = string.Join(" ", args),
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
                return null;

            await process.WaitForExitAsync(ct);
            if (process.ExitCode != 0 || !File.Exists(outputPath))
            {
                var error = await process.StandardError.ReadToEndAsync();
                Console.WriteLine($"[Transcode] ffmpeg error: {error}");
                return null;
            }

            var hash = AudioStorage.ComputeSha256File(outputPath);

            if (AppConfig.AUDIO_TRANSCODE_DELETE_ORIGINAL && !string.Equals(outputPath, inputPath, StringComparison.OrdinalIgnoreCase))
            {
                try { File.Delete(inputPath); } catch { /* ignore */ }
            }

            return new AudioTranscodeResult
            {
                RelativePath = outputRelative ?? relativePath,
                Format = targetFormat,
                Hash = hash
            };
        }

        private static string ResolveCodec(string format)
        {
            return format switch
            {
                "opus" => "libopus",
                "ogg" => "libopus",
                "webm" => "libopus",
                "mp3" => "libmp3lame",
                "aac" => "aac",
                "wav" => "pcm_s16le",
                "flac" => "flac",
                _ => "libopus"
            };
        }

        private static string Quote(string value)
        {
            return value.Contains(' ') ? $"\"{value}\"" : value;
        }
    }
}
