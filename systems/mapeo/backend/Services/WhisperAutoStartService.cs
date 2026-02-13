using System.Diagnostics;
using System.Net.Http;
using Backend.Utils;

namespace Backend.Services
{
    public sealed class WhisperAutoStartService : IHostedService
    {
        private readonly HttpClient _httpClient = new();
        private Process? _process;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (!AppConfig.WHISPER_AUTOSTART)
                return;

            var url = AppConfig.WHISPER_URL;
            if (string.IsNullOrWhiteSpace(url))
            {
                Console.WriteLine("[Whisper] WHISPER_URL vacio. AutoStart cancelado.");
                return;
            }

            if (await IsWhisperOnline(url, cancellationToken))
            {
                Console.WriteLine("[Whisper] Servicio ya activo.");
                return;
            }

            var cmd = AppConfig.WHISPER_START_COMMAND;
            if (string.IsNullOrWhiteSpace(cmd))
            {
                Console.WriteLine("[Whisper] WHISPER_START_COMMAND vacio. No se puede auto iniciar.");
                return;
            }

            try
            {
                _process = StartProcess(cmd, AppConfig.WHISPER_START_WORKDIR, AppConfig.WHISPER_START_USE_SHELL);
                Console.WriteLine("[Whisper] Iniciando proceso...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Whisper] Error al iniciar proceso: {ex.Message}");
                return;
            }

            var timeout = TimeSpan.FromSeconds(Math.Max(5, AppConfig.WHISPER_START_WAIT_SECONDS));
            var started = await WaitUntilOnline(url, timeout, cancellationToken);
            if (started)
            {
                Console.WriteLine("[Whisper] Servicio iniciado.");
            }
            else
            {
                Console.WriteLine("[Whisper] No se pudo verificar el inicio del servicio.");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            if (AppConfig.WHISPER_KILL_ON_STOP && _process != null && !_process.HasExited)
            {
                try
                {
                    _process.Kill(entireProcessTree: true);
                }
                catch
                {
                    // ignore
                }
            }

            return Task.CompletedTask;
        }

        private async Task<bool> IsWhisperOnline(string url, CancellationToken ct)
        {
            try
            {
                using var response = await _httpClient.GetAsync(url, ct);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> WaitUntilOnline(string url, TimeSpan timeout, CancellationToken ct)
        {
            var start = DateTime.UtcNow;
            while (DateTime.UtcNow - start < timeout && !ct.IsCancellationRequested)
            {
                if (await IsWhisperOnline(url, ct))
                    return true;

                await Task.Delay(1000, ct);
            }
            return false;
        }

        private static Process StartProcess(string command, string workdir, bool useShell)
        {
            if (useShell)
            {
                if (OperatingSystem.IsWindows())
                {
                    return Process.Start(new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c {command}",
                        WorkingDirectory = string.IsNullOrWhiteSpace(workdir) ? Environment.CurrentDirectory : workdir,
                        CreateNoWindow = true,
                        UseShellExecute = false
                    })!;
                }

                return Process.Start(new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-lc \"{command}\"",
                    WorkingDirectory = string.IsNullOrWhiteSpace(workdir) ? Environment.CurrentDirectory : workdir,
                    CreateNoWindow = true,
                    UseShellExecute = false
                })!;
            }

            var parts = command.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            var file = parts.Length > 0 ? parts[0] : command;
            var args = parts.Length > 1 ? parts[1] : "";

            return Process.Start(new ProcessStartInfo
            {
                FileName = file,
                Arguments = args,
                WorkingDirectory = string.IsNullOrWhiteSpace(workdir) ? Environment.CurrentDirectory : workdir,
                CreateNoWindow = true,
                UseShellExecute = false
            })!;
        }
    }
}
