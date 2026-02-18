using Backend.Models.Sistemas;
using Backend.Negocio.Generadores;
using Backend.Negocio.Gestores;
using Backend.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http;

namespace Backend.Controllers
{
    [ApiController]
    [Authorize]
    public class SistemasController : AppController
    {
        private static readonly ConcurrentDictionary<int, Process> BackendProcesses = new();
        private static readonly ConcurrentDictionary<int, Process> FrontendProcesses = new();
        private static readonly HttpClient Http = new();
        private readonly IWebHostEnvironment _env;

        public SistemasController(IWebHostEnvironment env)
        {
            _env = env;
        }

        [HttpGet(Routes.v1.Sistemas.Obtener)]
        public IActionResult Obtener()
        {
            var sistemas = SistemasGestor.ObtenerTodos();
            return Ok(sistemas);
        }

        [HttpGet(Routes.v1.Sistemas.ObtenerPorId)]
        public IActionResult ObtenerPorId(int id)
        {
            var sistema = SistemasGestor.ObtenerPorId(id);
            return sistema == null ? NotFound() : Ok(sistema);
        }

        [HttpGet(Routes.v1.Sistemas.ObtenerPorSlug)]
        public IActionResult ObtenerPorSlug(string slug)
        {
            var sistema = SistemasGestor.ObtenerPorSlug(slug);
            return sistema == null ? NotFound() : Ok(sistema);
        }

        [HttpPost(Routes.v1.Sistemas.Crear)]
        public IActionResult Crear([FromBody] SistemaCreateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest();

            var id = SistemasGestor.Crear(request);
            if (id == null)
                return Conflict("Slug invalido o ya existe.");

            return Ok(new { id });
        }

        [HttpPut(Routes.v1.Sistemas.Editar)]
        public IActionResult Editar(int id, [FromBody] SistemaUpdateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest();

            var ok = SistemasGestor.Editar(id, request);
            return ok ? Ok() : NotFound();
        }

        [HttpPost(Routes.v1.Sistemas.Publicar)]
        public IActionResult Publicar(int id)
        {
            var result = SistemasPublicador.Publicar(id);
            return result.Ok ? Ok(result) : BadRequest(result);
        }

        [HttpPost(Routes.v1.Sistemas.Exportar)]
        public IActionResult Exportar(
            int id,
            [FromQuery] bool full = false,
            [FromQuery] string mode = "zip",
            [FromQuery] bool overwrite = false,
            [FromQuery] string source = "")
        {
            var normalizedMode = (mode ?? "zip").Trim().ToLowerInvariant();
            var normalizedSource = (source ?? string.Empty).Trim().ToLowerInvariant();
            var repoRoot = Directory.GetParent(_env.ContentRootPath)?.FullName ?? _env.ContentRootPath;

            var systemsRoot = Environment.GetEnvironmentVariable("SYSTEMBASE_SYSTEMS_ROOT");
            if (string.IsNullOrWhiteSpace(systemsRoot))
                systemsRoot = Path.Combine(repoRoot, "systems");

            var exportsRoot = Environment.GetEnvironmentVariable("SYSTEMBASE_EXPORT_ROOT");
            if (string.IsNullOrWhiteSpace(exportsRoot))
                exportsRoot = Path.Combine(repoRoot, "exports");

            var preferWorkspaceZip = normalizedMode == "zip" &&
                (string.IsNullOrWhiteSpace(normalizedSource) || normalizedSource == "workspace");

            if (preferWorkspaceZip)
            {
                var sistema = SistemasGestor.ObtenerPorId(id);
                if (sistema == null)
                    return NotFound();

                var workspacePath = Path.Combine(systemsRoot, sistema.Slug);
                if (!Directory.Exists(workspacePath))
                {
                    var exportResult = SistemasExportador.Exportar(
                        id,
                        systemsRoot,
                        _env.ContentRootPath,
                        full,
                        true,
                        overwrite
                    );

                    if (!exportResult.Ok)
                        return BadRequest(exportResult);

                    workspacePath = exportResult.ExportPath;
                }

                var metadataResult = SistemasExportador.ActualizarMetadata(
                    id,
                    workspacePath,
                    _env.ContentRootPath,
                    full
                );

                if (!metadataResult.Ok)
                    return BadRequest(metadataResult);

                Directory.CreateDirectory(exportsRoot);
                var rawName = string.IsNullOrWhiteSpace(sistema.Name) ? sistema.Slug : sistema.Name.Trim();
                var safeName = string.Join("_", rawName.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
                if (string.IsNullOrWhiteSpace(safeName))
                    safeName = sistema.Slug;
                var zipFileName = $"{safeName}.zip";
                var zipPath = Path.Combine(exportsRoot, zipFileName);
                if (System.IO.File.Exists(zipPath))
                    System.IO.File.Delete(zipPath);

                ZipFile.CreateFromDirectory(workspacePath, zipPath, CompressionLevel.Fastest, false);
                return PhysicalFile(zipPath, "application/zip", zipFileName);
            }

            var exportRoot = normalizedMode == "workspace" ? systemsRoot : exportsRoot;

            var result = SistemasExportador.Exportar(
                id,
                exportRoot,
                _env.ContentRootPath,
                full,
                normalizedMode == "workspace",
                overwrite
            );

            if (!result.Ok)
                return BadRequest(result);

            if (normalizedMode == "workspace")
                return Ok(result);

            if (string.IsNullOrWhiteSpace(result.ZipPath) || string.IsNullOrWhiteSpace(result.ZipFileName))
                return BadRequest(result);

            return PhysicalFile(result.ZipPath, "application/zip", result.ZipFileName);
        }

        [HttpPost(Routes.v1.Sistemas.GenerarBackend)]
        public IActionResult GenerarBackend(int id, [FromQuery] bool overwrite = false)
        {
            var repoRoot = Directory.GetParent(_env.ContentRootPath)?.FullName ?? _env.ContentRootPath;
            var sistema = SistemasGestor.ObtenerPorId(id);
            if (sistema == null)
                return NotFound();

            var outputRoot = Path.Combine(repoRoot, "systems", sistema.Slug, "backend");
            var result = SistemasBackendGenerator.Generar(id, outputRoot, overwrite);

            if (!result.Ok || string.IsNullOrWhiteSpace(result.OutputPath))
                return BadRequest(result);

            var restoreResult = RunDotnetRestore(outputRoot);
            result.RestoreOk = restoreResult.Ok;
            result.RestoreOutput = restoreResult.Output;
            result.RestoreError = restoreResult.Error;

            return Ok(result);
        }

        [HttpPost(Routes.v1.Sistemas.GenerarFrontend)]
        public IActionResult GenerarFrontend(int id, [FromQuery] bool overwrite = false)
        {
            var repoRoot = Directory.GetParent(_env.ContentRootPath)?.FullName ?? _env.ContentRootPath;
            var sistema = SistemasGestor.ObtenerPorId(id);
            if (sistema == null)
                return NotFound();

            var frontendSource = Path.Combine(repoRoot, "frontend-runtime");
            var outputRoot = Path.Combine(repoRoot, "systems", sistema.Slug, "frontend");

            var result = SistemasFrontendGenerator.Generar(id, frontendSource, outputRoot, overwrite);
            if (result.Ok)
            {
                FrontendConfigGestor.HabilitarFrontend(id);
                return Ok(result);
            }

            return BadRequest(result);
        }

        [HttpPost(Routes.v1.Sistemas.IniciarBackend)]
        public async Task<IActionResult> IniciarBackend(int id)
        {
            if (!_env.IsDevelopment())
                return Forbid();

            var repoRoot = Directory.GetParent(_env.ContentRootPath)?.FullName ?? _env.ContentRootPath;
            var sistema = SistemasGestor.ObtenerPorId(id);
            if (sistema == null)
                return NotFound();

            var backendPath = Path.Combine(repoRoot, "systems", sistema.Slug, "backend");
            if (!Directory.Exists(backendPath))
                return BadRequest(new { message = "El backend no esta generado. Genera backend primero." });

            if (BackendProcesses.TryGetValue(id, out var existing) && !existing.HasExited)
            {
                BackendProcessLogStore.Add(id, "info", "Inicio solicitado: backend ya en ejecucion.");
                return Ok(new { status = "running", message = "El backend ya esta en ejecucion." });
            }

            if (await IsBackendOnline(id))
            {
                BackendProcessLogStore.Add(id, "info", "Inicio solicitado: backend ya estaba online.");
                return Ok(new { status = "running", message = "El backend ya esta en ejecucion." });
            }

            var logBuffer = BackendProcessLogStore.Reset(id);
            logBuffer.Add("info", "Iniciando backend (dotnet watch run)...");
            logBuffer.Add("info", "Sin restore previo: dotnet watch restaurara paquetes si hace falta.");

            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "watch run",
                WorkingDirectory = backendPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
            startInfo.Environment["DOTNET_WATCH_SUPPRESS_LAUNCH_BROWSER"] = "1";

            var process = Process.Start(startInfo);
            if (process == null)
            {
                logBuffer.Add("error", "No se pudo iniciar el proceso dotnet.");
                return BadRequest(new { message = "No se pudo iniciar el backend." });
            }

            process.EnableRaisingEvents = true;
            process.OutputDataReceived += (_, args) =>
            {
                if (!string.IsNullOrWhiteSpace(args.Data))
                    logBuffer.Add("stdout", args.Data);
            };
            process.ErrorDataReceived += (_, args) =>
            {
                if (!string.IsNullOrWhiteSpace(args.Data))
                    logBuffer.Add("stderr", args.Data);
            };
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.Exited += (_, __) =>
            {
                logBuffer.Add("info", $"dotnet watch terminó (exit code {process.ExitCode}).");
                BackendProcesses.TryRemove(id, out Process removedProcess);
            };

            BackendProcesses[id] = process;
            return Ok(new { status = "started", message = "Backend iniciando..." });
        }

        [HttpPost(Routes.v1.Sistemas.DetenerBackend)]
        public IActionResult DetenerBackend(int id)
        {
            if (!_env.IsDevelopment())
                return Forbid();

            if (!BackendProcesses.TryGetValue(id, out var process) || process.HasExited)
            {
                BackendProcessLogStore.Add(id, "info", "Detener solicitado: backend no estaba en ejecucion.");
                return Ok(new { status = "not_running", message = "El backend no esta en ejecucion." });
            }

            try
            {
                BackendProcessLogStore.Add(id, "info", "Deteniendo backend...");
                process.Kill(true);
                process.WaitForExit(10000);
            }
            catch
            {
                // ignore
            }

            BackendProcesses.TryRemove(id, out Process removedProcess);
            BackendProcessLogStore.Add(id, "info", "Backend detenido.");
            return Ok(new { status = "stopped", message = "Backend detenido." });
        }

        [HttpGet(Routes.v1.Sistemas.PingBackend)]
        public async Task<IActionResult> PingBackend(int id)
        {
            if (!_env.IsDevelopment())
                return Forbid();

            var online = await IsBackendOnline(id);
            var running = IsBackendProcessRunning(id);
            return Ok(new { online, running });
        }

        [HttpGet(Routes.v1.Sistemas.LogsBackend)]
        public IActionResult LogsBackend(int id, [FromQuery] long after = 0, [FromQuery] int take = 200)
        {
            if (!_env.IsDevelopment())
                return Forbid();

            take = Math.Clamp(take, 1, 500);
            var buffer = BackendProcessLogStore.Get(id);
            var items = buffer.Read(after, take, out var lastId);
            return Ok(new { items, lastId });
        }

        [HttpPost(Routes.v1.Sistemas.IniciarFrontend)]
        public async Task<IActionResult> IniciarFrontend(int id)
        {
            if (!_env.IsDevelopment())
                return Forbid();

            var repoRoot = Directory.GetParent(_env.ContentRootPath)?.FullName ?? _env.ContentRootPath;
            var sistema = SistemasGestor.ObtenerPorId(id);
            if (sistema == null)
                return NotFound();

            var frontendPath = Path.Combine(repoRoot, "systems", sistema.Slug, "frontend");
            if (!Directory.Exists(frontendPath))
                return BadRequest(new { message = "El frontend no esta generado. Genera frontend primero." });

            if (FrontendProcesses.TryGetValue(id, out var existing) && !existing.HasExited)
            {
                FrontendProcessLogStore.Add(id, "info", "Inicio solicitado: frontend ya en ejecucion.");
                return Ok(new { status = "running", message = "El frontend ya esta en ejecucion." });
            }

            if (await IsFrontendOnline(id))
            {
                FrontendProcessLogStore.Add(id, "info", "Inicio solicitado: frontend ya estaba online.");
                return Ok(new { status = "running", message = "El frontend ya esta en ejecucion." });
            }

            var logBuffer = FrontendProcessLogStore.Reset(id);
            logBuffer.Add("info", "Iniciando frontend (npm run dev)...");

            var nodeModules = Path.Combine(frontendPath, "node_modules");
            if (!Directory.Exists(nodeModules))
            {
                logBuffer.Add("info", "Instalando dependencias (npm install)...");
                var install = RunNpmInstall(frontendPath);
                if (!install.Ok)
                {
                    logBuffer.Add("error", $"npm install fallo: {install.Error}");
                    return BadRequest(new { message = "npm install fallo. Revisa la consola del frontend." });
                }
                logBuffer.Add("info", "npm install ok.");
            }

            var port = GetFrontendPort(id);
            var startInfo = new ProcessStartInfo
            {
                FileName = "npm",
                Arguments = $"run dev -- --port {port}",
                WorkingDirectory = frontendPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            startInfo.Environment["BROWSER"] = "none";
            startInfo.Environment["VITE_PORT"] = port.ToString();

            var process = Process.Start(startInfo);
            if (process == null)
            {
                logBuffer.Add("error", "No se pudo iniciar el proceso npm.");
                return BadRequest(new { message = "No se pudo iniciar el frontend." });
            }

            process.EnableRaisingEvents = true;
            process.OutputDataReceived += (_, args) =>
            {
                if (!string.IsNullOrWhiteSpace(args.Data))
                    logBuffer.Add("stdout", args.Data);
            };
            process.ErrorDataReceived += (_, args) =>
            {
                if (!string.IsNullOrWhiteSpace(args.Data))
                    logBuffer.Add("stderr", args.Data);
            };
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.Exited += (_, __) =>
            {
                logBuffer.Add("info", $"npm dev terminó (exit code {process.ExitCode}).");
                FrontendProcesses.TryRemove(id, out Process removedProcess);
            };

            FrontendProcesses[id] = process;
            return Ok(new { status = "started", message = "Frontend iniciando...", port });
        }

        [HttpPost(Routes.v1.Sistemas.DetenerFrontend)]
        public IActionResult DetenerFrontend(int id)
        {
            if (!_env.IsDevelopment())
                return Forbid();

            if (!FrontendProcesses.TryGetValue(id, out var process) || process.HasExited)
            {
                FrontendProcessLogStore.Add(id, "info", "Detener solicitado: frontend no estaba en ejecucion.");
                return Ok(new { status = "not_running", message = "El frontend no esta en ejecucion." });
            }

            try
            {
                FrontendProcessLogStore.Add(id, "info", "Deteniendo frontend...");
                process.Kill(true);
                process.WaitForExit(10000);
            }
            catch
            {
                // ignore
            }

            FrontendProcesses.TryRemove(id, out Process removedProcess);
            FrontendProcessLogStore.Add(id, "info", "Frontend detenido.");
            return Ok(new { status = "stopped", message = "Frontend detenido." });
        }

        [HttpGet(Routes.v1.Sistemas.PingFrontend)]
        public async Task<IActionResult> PingFrontend(int id)
        {
            if (!_env.IsDevelopment())
                return Forbid();

            var online = await IsFrontendOnline(id);
            return Ok(new { online });
        }

        [HttpGet(Routes.v1.Sistemas.LogsFrontend)]
        public IActionResult LogsFrontend(int id, [FromQuery] long after = 0, [FromQuery] int take = 200)
        {
            if (!_env.IsDevelopment())
                return Forbid();

            take = Math.Clamp(take, 1, 500);
            var buffer = FrontendProcessLogStore.Get(id);
            var items = buffer.Read(after, take, out var lastId);
            return Ok(new { items, lastId });
        }

        private async Task<bool> IsBackendOnline(int systemId)
        {
            var port = 5032 + systemId;
            var configuredBasePath = NormalizeApiBase(BackendConfigGestor.ObtenerPorSistema(systemId)?.System?.ApiBase);
            var candidates = new List<string>
            {
                $"{configuredBasePath}/dev/ping",
                "/api/v1/dev/ping",
                "/dev/ping"
            };

            foreach (var candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                try
                {
                    var url = $"http://localhost:{port}{candidate}";
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                    using var response = await Http.GetAsync(url, cts.Token);
                    if (response.IsSuccessStatusCode)
                        return true;
                }
                catch
                {
                    // probamos el siguiente candidato
                }
            }

            return false;
        }

        private static string NormalizeApiBase(string? apiBase)
        {
            var value = string.IsNullOrWhiteSpace(apiBase) ? "api/v1" : apiBase.Trim();
            value = value.Trim('/');
            return "/" + value;
        }

        private static int GetFrontendPort(int systemId)
        {
            return 5173 + systemId;
        }

        private static bool IsBackendProcessRunning(int systemId)
        {
            return BackendProcesses.TryGetValue(systemId, out var process) && !process.HasExited;
        }

        private async Task<bool> IsFrontendOnline(int systemId)
        {
            try
            {
                var port = GetFrontendPort(systemId);
                var url = $"http://localhost:{port}";
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                using var response = await Http.GetAsync(url, cts.Token);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private (bool Ok, string Output, string Error) RunDotnetRestore(string workingDirectory)
        {
            try
            {
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = "restore",
                    WorkingDirectory = workingDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = System.Diagnostics.Process.Start(startInfo);
                if (process == null)
                    return (false, string.Empty, "No se pudo iniciar dotnet restore.");

                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();

                if (!process.WaitForExit(120000))
                {
                    try { process.Kill(true); } catch { }
                    return (false, output, "Timeout ejecutando dotnet restore.");
                }

                var ok = process.ExitCode == 0;
                return (ok, output, error);
            }
            catch (Exception ex)
            {
                return (false, string.Empty, ex.Message);
            }
        }

        private (bool Ok, string Output, string Error) RunNpmInstall(string workingDirectory)
        {
            try
            {
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "npm",
                    Arguments = "install",
                    WorkingDirectory = workingDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = System.Diagnostics.Process.Start(startInfo);
                if (process == null)
                    return (false, string.Empty, "No se pudo iniciar npm install.");

                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();

                if (!process.WaitForExit(240000))
                {
                    try { process.Kill(true); } catch { }
                    return (false, output, "Timeout ejecutando npm install.");
                }

                var ok = process.ExitCode == 0;
                return (ok, output, error);
            }
            catch (Exception ex)
            {
                return (false, string.Empty, ex.Message);
            }
        }
    }
}
