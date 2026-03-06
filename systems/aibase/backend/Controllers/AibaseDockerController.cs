using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Backend.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Authorize]
    public class AibaseDockerController : AppController
    {
        private static readonly Regex SafeTokenRegex = new("^[a-zA-Z0-9_.-]+$", RegexOptions.Compiled);

        private readonly IWebHostEnvironment _env;

        public AibaseDockerController(IWebHostEnvironment env)
        {
            _env = env;
        }

        [HttpGet(Routes.v1.Aibase.DockerStatus)]
        public async Task<IActionResult> DockerStatus([FromQuery] string? stackName = null)
        {
            var gate = EnsureDockerAccess();
            if (gate != null) return gate;

            var ctx = ResolveContext(stackName);
            if (!ctx.Ok)
                return BadRequest(new { error = ctx.Error, stackName = ctx.StackName, composeFile = ctx.ComposeFile });

            var ps = await RunDockerComposeAsync(ctx, new[] { "ps", "--all", "--format", "json" });
            if (!ps.Ok)
                return StatusCode(502, BuildCommandError("status", ctx, ps));

            var servicesCmd = await RunDockerComposeAsync(ctx, new[] { "config", "--services" });
            var services = servicesCmd.Ok ? ParseServices(servicesCmd.StdOut) : new List<string>();
            var psRows = ParsePs(ps.StdOut);
            var runningServices = psRows
                .Where(x => x.State.Equals("running", StringComparison.OrdinalIgnoreCase))
                .Select(x => x.Service)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return Ok(new
            {
                stackName = ctx.StackName,
                composeFile = ctx.ComposeFile,
                services,
                ps = psRows,
                runningServices,
                command = ps.Command,
                stderr = MergeTexts(ps.StdErr, servicesCmd.StdErr),
                updatedAt = DateTime.UtcNow
            });
        }

        [HttpGet(Routes.v1.Aibase.DockerServices)]
        public async Task<IActionResult> DockerServices([FromQuery] string? stackName = null)
        {
            var gate = EnsureDockerAccess();
            if (gate != null) return gate;

            var ctx = ResolveContext(stackName);
            if (!ctx.Ok)
                return BadRequest(new { error = ctx.Error, stackName = ctx.StackName, composeFile = ctx.ComposeFile });

            var cmd = await RunDockerComposeAsync(ctx, new[] { "config", "--services" });
            if (!cmd.Ok)
                return StatusCode(502, BuildCommandError("services", ctx, cmd));

            return Ok(new
            {
                stackName = ctx.StackName,
                composeFile = ctx.ComposeFile,
                services = ParseServices(cmd.StdOut),
                command = cmd.Command,
                stderr = cmd.StdErr,
                updatedAt = DateTime.UtcNow
            });
        }

        [HttpPost(Routes.v1.Aibase.DockerUp)]
        public async Task<IActionResult> DockerUp([FromBody] DockerUpRequest? request)
        {
            var gate = EnsureDockerAccess();
            if (gate != null) return gate;

            var ctx = ResolveContext(request?.StackName);
            if (!ctx.Ok)
                return BadRequest(new { error = ctx.Error, stackName = ctx.StackName, composeFile = ctx.ComposeFile });

            var services = ValidateServices(request?.Services);
            if (!services.Ok)
                return BadRequest(new { error = services.Error });

            var args = new List<string> { "up", "-d" };
            if (request?.Build == true) args.Add("--build");
            args.AddRange(services.Value);

            var cmd = await RunDockerComposeAsync(ctx, args);
            if (!cmd.Ok)
                return StatusCode(502, BuildCommandError("up", ctx, cmd));

            return Ok(new
            {
                action = "up",
                stackName = ctx.StackName,
                composeFile = ctx.ComposeFile,
                services = services.Value,
                command = cmd.Command,
                stdout = cmd.StdOut,
                stderr = cmd.StdErr,
                updatedAt = DateTime.UtcNow
            });
        }

        [HttpPost(Routes.v1.Aibase.DockerDown)]
        public async Task<IActionResult> DockerDown([FromBody] DockerDownRequest? request)
        {
            var gate = EnsureDockerAccess();
            if (gate != null) return gate;

            var ctx = ResolveContext(request?.StackName);
            if (!ctx.Ok)
                return BadRequest(new { error = ctx.Error, stackName = ctx.StackName, composeFile = ctx.ComposeFile });

            var args = new List<string> { "down" };
            if (request?.RemoveOrphans != false) args.Add("--remove-orphans");
            if (request?.RemoveVolumes == true) args.Add("--volumes");

            var cmd = await RunDockerComposeAsync(ctx, args);
            if (!cmd.Ok)
                return StatusCode(502, BuildCommandError("down", ctx, cmd));

            return Ok(new
            {
                action = "down",
                stackName = ctx.StackName,
                composeFile = ctx.ComposeFile,
                command = cmd.Command,
                stdout = cmd.StdOut,
                stderr = cmd.StdErr,
                updatedAt = DateTime.UtcNow
            });
        }

        [HttpPost(Routes.v1.Aibase.DockerRestart)]
        public async Task<IActionResult> DockerRestart([FromBody] DockerRestartRequest? request)
        {
            var gate = EnsureDockerAccess();
            if (gate != null) return gate;

            var ctx = ResolveContext(request?.StackName);
            if (!ctx.Ok)
                return BadRequest(new { error = ctx.Error, stackName = ctx.StackName, composeFile = ctx.ComposeFile });

            var args = new List<string> { "restart" };
            var service = (request?.Service ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(service))
            {
                if (!IsSafeToken(service))
                    return BadRequest(new { error = $"Servicio inválido: {service}" });
                args.Add(service);
            }

            var cmd = await RunDockerComposeAsync(ctx, args);
            if (!cmd.Ok)
                return StatusCode(502, BuildCommandError("restart", ctx, cmd));

            return Ok(new
            {
                action = string.IsNullOrWhiteSpace(service) ? "restart_stack" : "restart_service",
                stackName = ctx.StackName,
                service = string.IsNullOrWhiteSpace(service) ? null : service,
                composeFile = ctx.ComposeFile,
                command = cmd.Command,
                stdout = cmd.StdOut,
                stderr = cmd.StdErr,
                updatedAt = DateTime.UtcNow
            });
        }

        [HttpGet(Routes.v1.Aibase.DockerLogs)]
        public async Task<IActionResult> DockerLogs(
            [FromQuery] string? stackName = null,
            [FromQuery] string? service = null,
            [FromQuery] int tail = 200)
        {
            var gate = EnsureDockerAccess();
            if (gate != null) return gate;

            var ctx = ResolveContext(stackName);
            if (!ctx.Ok)
                return BadRequest(new { error = ctx.Error, stackName = ctx.StackName, composeFile = ctx.ComposeFile });

            tail = Math.Clamp(tail, 20, 2000);
            var args = new List<string> { "logs", "--no-color", "--tail", tail.ToString() };

            var safeService = (service ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(safeService))
            {
                if (!IsSafeToken(safeService))
                    return BadRequest(new { error = $"Servicio inválido: {safeService}" });
                args.Add(safeService);
            }

            var cmd = await RunDockerComposeAsync(ctx, args, Math.Max(10, AppConfig.AIBASE_DOCKER_COMMAND_TIMEOUT_SECONDS));
            if (!cmd.Ok)
                return StatusCode(502, BuildCommandError("logs", ctx, cmd));

            return Ok(new
            {
                stackName = ctx.StackName,
                service = string.IsNullOrWhiteSpace(safeService) ? null : safeService,
                tail,
                composeFile = ctx.ComposeFile,
                command = cmd.Command,
                logs = cmd.StdOut,
                stderr = cmd.StdErr,
                updatedAt = DateTime.UtcNow
            });
        }

        [HttpPost(Routes.v1.Aibase.DockerServiceAction)]
        public async Task<IActionResult> DockerServiceAction(string service, [FromBody] DockerServiceActionRequest? request)
        {
            var gate = EnsureDockerAccess();
            if (gate != null) return gate;

            var ctx = ResolveContext(request?.StackName);
            if (!ctx.Ok)
                return BadRequest(new { error = ctx.Error, stackName = ctx.StackName, composeFile = ctx.ComposeFile });

            service = service.Trim();
            if (!IsSafeToken(service))
                return BadRequest(new { error = $"Servicio inválido: {service}" });

            var action = (request?.Action ?? "restart").Trim().ToLowerInvariant();
            if (action is not ("start" or "stop" or "restart"))
                return BadRequest(new { error = $"Acción inválida: {action}. Usa start|stop|restart." });

            var cmd = await RunDockerComposeAsync(ctx, new[] { action, service });
            if (!cmd.Ok)
                return StatusCode(502, BuildCommandError(action, ctx, cmd));

            return Ok(new
            {
                action,
                service,
                stackName = ctx.StackName,
                composeFile = ctx.ComposeFile,
                command = cmd.Command,
                stdout = cmd.StdOut,
                stderr = cmd.StdErr,
                updatedAt = DateTime.UtcNow
            });
        }

        private IActionResult? EnsureDockerAccess()
        {
            if (!_env.IsDevelopment())
                return Forbid();

            var remote = HttpContext.Connection.RemoteIpAddress;
            if (remote == null || !IPAddress.IsLoopback(remote))
                return Forbid();

            return null;
        }

        private DockerContext ResolveContext(string? stackName)
        {
            var resolvedStackName = string.IsNullOrWhiteSpace(stackName)
                ? AppConfig.AIBASE_DOCKER_PROJECT
                : stackName.Trim();

            if (!IsSafeToken(resolvedStackName))
            {
                return DockerContext.Fail($"Stack inválido: {resolvedStackName}");
            }

            var composeFile = AppConfig.AIBASE_DOCKER_COMPOSE_FILE;
            if (string.IsNullOrWhiteSpace(composeFile))
            {
                composeFile = Path.Combine(_env.ContentRootPath, "..", "docker", "docker-compose.yml");
            }

            if (!Path.IsPathRooted(composeFile))
            {
                composeFile = Path.Combine(_env.ContentRootPath, composeFile);
            }

            composeFile = Path.GetFullPath(composeFile);
            if (!System.IO.File.Exists(composeFile))
            {
                return DockerContext.Fail(
                    $"No se encontró docker-compose.yml en '{composeFile}'. Configura AIBASE_DOCKER_COMPOSE_FILE.",
                    resolvedStackName,
                    composeFile
                );
            }

            return DockerContext.Success(resolvedStackName, composeFile);
        }

        private static bool IsSafeToken(string value)
        {
            return !string.IsNullOrWhiteSpace(value) && SafeTokenRegex.IsMatch(value);
        }

        private static ValidationResult<List<string>> ValidateServices(IEnumerable<string>? services)
        {
            var result = new List<string>();
            if (services == null) return ValidationResult<List<string>>.Success(result);

            foreach (var raw in services)
            {
                var service = (raw ?? "").Trim();
                if (string.IsNullOrWhiteSpace(service)) continue;
                if (!IsSafeToken(service))
                    return ValidationResult<List<string>>.Fail($"Servicio inválido: {service}");
                result.Add(service);
            }

            return ValidationResult<List<string>>.Success(result.Distinct(StringComparer.OrdinalIgnoreCase).ToList());
        }

        private async Task<DockerCommandResult> RunDockerComposeAsync(DockerContext ctx, IEnumerable<string> composeArgs, int? timeoutSeconds = null)
        {
            var timeout = Math.Clamp(
                timeoutSeconds ?? AppConfig.AIBASE_DOCKER_COMMAND_TIMEOUT_SECONDS,
                5,
                300
            );

            var psi = new ProcessStartInfo
            {
                FileName = "docker",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(ctx.ComposeFile) ?? _env.ContentRootPath
            };

            psi.ArgumentList.Add("compose");
            psi.ArgumentList.Add("-f");
            psi.ArgumentList.Add(ctx.ComposeFile);
            psi.ArgumentList.Add("-p");
            psi.ArgumentList.Add(ctx.StackName);

            foreach (var arg in composeArgs)
            {
                psi.ArgumentList.Add(arg);
            }

            var commandPreview = BuildCommandPreview(psi);

            using var process = new Process { StartInfo = psi };
            var stdOut = new StringBuilder();
            var stdErr = new StringBuilder();
            process.OutputDataReceived += (_, args) =>
            {
                if (args.Data != null) stdOut.AppendLine(args.Data);
            };
            process.ErrorDataReceived += (_, args) =>
            {
                if (args.Data != null) stdErr.AppendLine(args.Data);
            };

            var startedAt = DateTime.UtcNow;
            try
            {
                if (!process.Start())
                {
                    return DockerCommandResult.Fail(commandPreview, "No se pudo iniciar el proceso docker.");
                }
            }
            catch (Exception ex)
            {
                return DockerCommandResult.Fail(commandPreview, ex.Message);
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            var timedOut = false;
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeout)))
            {
                try
                {
                    await process.WaitForExitAsync(cts.Token);
                }
                catch (OperationCanceledException)
                {
                    timedOut = true;
                    try { process.Kill(true); } catch { /* no-op */ }
                }
            }

            var durationMs = (long)(DateTime.UtcNow - startedAt).TotalMilliseconds;
            var stdoutText = stdOut.ToString().Trim();
            var stderrText = stdErr.ToString().Trim();

            if (timedOut)
            {
                return DockerCommandResult.Timeout(commandPreview, stdoutText, stderrText, durationMs, timeout);
            }

            var ok = process.ExitCode == 0;
            return new DockerCommandResult
            {
                Ok = ok,
                ExitCode = process.ExitCode,
                Command = commandPreview,
                StdOut = stdoutText,
                StdErr = stderrText,
                DurationMs = durationMs
            };
        }

        private static string BuildCommandPreview(ProcessStartInfo psi)
        {
            var args = psi.ArgumentList
                .Select(QuoteArg)
                .ToList();
            return $"docker {string.Join(" ", args)}";
        }

        private static string QuoteArg(string value)
        {
            if (string.IsNullOrEmpty(value)) return "\"\"";
            return value.Contains(' ') || value.Contains('"')
                ? $"\"{value.Replace("\"", "\\\"")}\""
                : value;
        }

        private static List<string> ParseServices(string stdout)
        {
            return StringToLines(stdout)
                .Select(x => x.Trim())
                .Where(IsSafeToken)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static List<DockerPsItem> ParsePs(string stdout)
        {
            var text = (stdout ?? "").Trim();
            if (string.IsNullOrWhiteSpace(text))
                return new List<DockerPsItem>();

            try
            {
                using var doc = JsonDocument.Parse(text);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    return doc.RootElement.EnumerateArray().Select(ParsePsItem).ToList();
                }
                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    return new List<DockerPsItem> { ParsePsItem(doc.RootElement) };
                }
            }
            catch
            {
                // fallback ndjson
            }

            var rows = new List<DockerPsItem>();
            foreach (var line in StringToLines(text))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed) || (!trimmed.StartsWith("{") && !trimmed.StartsWith("[")))
                    continue;
                try
                {
                    using var doc = JsonDocument.Parse(trimmed);
                    if (doc.RootElement.ValueKind == JsonValueKind.Object)
                        rows.Add(ParsePsItem(doc.RootElement));
                }
                catch
                {
                    // ignored
                }
            }

            return rows;
        }

        private static DockerPsItem ParsePsItem(JsonElement item)
        {
            var name = ReadJson(item, "Name", "name");
            var service = ReadJson(item, "Service", "service");
            var state = ReadJson(item, "State", "state");
            var status = ReadJson(item, "Status", "status");
            var health = ReadJson(item, "Health", "health");

            var ports = "";
            if (TryGetProperty(item, out var publishers, "Publishers", "publishers"))
            {
                if (publishers.ValueKind == JsonValueKind.Array)
                {
                    var parts = new List<string>();
                    foreach (var pub in publishers.EnumerateArray())
                    {
                        var url = ReadJson(pub, "URL", "url");
                        var publishedPort = ReadJson(pub, "PublishedPort", "publishedPort");
                        var targetPort = ReadJson(pub, "TargetPort", "targetPort");
                        var protocol = ReadJson(pub, "Protocol", "protocol");

                        var head = string.IsNullOrWhiteSpace(url) ? "" : $"{url}:";
                        var target = string.IsNullOrWhiteSpace(targetPort) ? "" : targetPort;
                        var published = string.IsNullOrWhiteSpace(publishedPort) ? "" : publishedPort;
                        var proto = string.IsNullOrWhiteSpace(protocol) ? "" : $"/{protocol}";
                        var text = $"{head}{published}->{target}{proto}".Trim('-', '>', ':');
                        if (!string.IsNullOrWhiteSpace(text)) parts.Add(text);
                    }
                    ports = string.Join(", ", parts);
                }
                else
                {
                    ports = publishers.ToString();
                }
            }

            return new DockerPsItem
            {
                Name = name,
                Service = service,
                State = state,
                Status = status,
                Health = health,
                Ports = ports
            };
        }

        private static string ReadJson(JsonElement element, params string[] names)
        {
            if (!TryGetProperty(element, out var value, names))
                return "";

            return value.ValueKind switch
            {
                JsonValueKind.String => value.GetString() ?? "",
                JsonValueKind.Number => value.ToString(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => value.ToString()
            };
        }

        private static bool TryGetProperty(JsonElement element, out JsonElement value, params string[] names)
        {
            foreach (var prop in element.EnumerateObject())
            {
                foreach (var name in names)
                {
                    if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
                    {
                        value = prop.Value;
                        return true;
                    }
                }
            }

            value = default;
            return false;
        }

        private static List<string> StringToLines(string input)
        {
            return input
                .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .ToList();
        }

        private static string MergeTexts(string a, string b)
        {
            var merged = new List<string>();
            if (!string.IsNullOrWhiteSpace(a)) merged.Add(a.Trim());
            if (!string.IsNullOrWhiteSpace(b)) merged.Add(b.Trim());
            return string.Join("\n", merged);
        }

        private static object BuildCommandError(string action, DockerContext ctx, DockerCommandResult cmd)
        {
            return new
            {
                error = $"No se pudo ejecutar Docker action '{action}'.",
                stackName = ctx.StackName,
                composeFile = ctx.ComposeFile,
                command = cmd.Command,
                exitCode = cmd.ExitCode,
                timedOut = cmd.TimedOut,
                timeoutSec = cmd.TimeoutSeconds,
                durationMs = cmd.DurationMs,
                stdout = cmd.StdOut,
                stderr = cmd.StdErr
            };
        }

        public sealed class DockerUpRequest
        {
            public string? StackName { get; set; }
            public List<string>? Services { get; set; }
            public bool Build { get; set; }
        }

        public sealed class DockerDownRequest
        {
            public string? StackName { get; set; }
            public bool RemoveOrphans { get; set; } = true;
            public bool RemoveVolumes { get; set; }
        }

        public sealed class DockerRestartRequest
        {
            public string? StackName { get; set; }
            public string? Service { get; set; }
        }

        public sealed class DockerServiceActionRequest
        {
            public string? StackName { get; set; }
            public string? Action { get; set; }
        }

        private sealed class ValidationResult<T>
        {
            public bool Ok { get; private init; }
            public T Value { get; private init; } = default!;
            public string Error { get; private init; } = "";

            public static ValidationResult<T> Success(T value) => new() { Ok = true, Value = value };
            public static ValidationResult<T> Fail(string error) => new() { Ok = false, Error = error };
        }

        private sealed class DockerContext
        {
            public bool Ok { get; private init; }
            public string StackName { get; private init; } = "";
            public string ComposeFile { get; private init; } = "";
            public string Error { get; private init; } = "";

            public static DockerContext Success(string stackName, string composeFile)
                => new() { Ok = true, StackName = stackName, ComposeFile = composeFile };

            public static DockerContext Fail(string error, string? stackName = null, string? composeFile = null)
                => new()
                {
                    Ok = false,
                    Error = error,
                    StackName = stackName ?? "",
                    ComposeFile = composeFile ?? ""
                };
        }

        private sealed class DockerCommandResult
        {
            public bool Ok { get; init; }
            public int ExitCode { get; init; }
            public string Command { get; init; } = "";
            public string StdOut { get; init; } = "";
            public string StdErr { get; init; } = "";
            public bool TimedOut { get; init; }
            public int TimeoutSeconds { get; init; }
            public long DurationMs { get; init; }

            public static DockerCommandResult Fail(string command, string error) =>
                new()
                {
                    Ok = false,
                    ExitCode = -1,
                    Command = command,
                    StdErr = error
                };

            public static DockerCommandResult Timeout(string command, string stdOut, string stdErr, long durationMs, int timeoutSeconds) =>
                new()
                {
                    Ok = false,
                    ExitCode = -1,
                    Command = command,
                    StdOut = stdOut,
                    StdErr = string.IsNullOrWhiteSpace(stdErr)
                        ? $"Timeout al ejecutar comando docker ({timeoutSeconds}s)."
                        : stdErr,
                    TimedOut = true,
                    TimeoutSeconds = timeoutSeconds,
                    DurationMs = durationMs
                };
        }

        public sealed class DockerPsItem
        {
            public string Name { get; set; } = "";
            public string Service { get; set; } = "";
            public string State { get; set; } = "";
            public string Status { get; set; } = "";
            public string Health { get; set; } = "";
            public string Ports { get; set; } = "";
        }
    }
}
