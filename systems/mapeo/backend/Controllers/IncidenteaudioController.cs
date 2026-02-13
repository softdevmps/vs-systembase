using Backend.Models.Incidenteaudio;
using Backend.Negocio.Gestores;
using Backend.Negocio.Pipeline;
using Backend.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace Backend.Controllers
{
    [ApiController]
    public class IncidenteaudioController : AppController
    {
        [Authorize]
        [HttpGet(Routes.v1.Incidenteaudio.Obtener)]
        public IActionResult Obtener()
        {
            var items = IncidenteaudioGestor.ObtenerTodos(null, null, null);
            return Ok(items);
        }

        [Authorize]
        [HttpGet(Routes.v1.Incidenteaudio.ObtenerPorId)]
        public IActionResult ObtenerPorId(int id)
        {
            var item = IncidenteaudioGestor.ObtenerPorId(id);
            if (item == null)
                return NotFound();

            return Ok(item);
        }

        [Authorize]
        [HttpGet(Routes.v1.Incidenteaudio.Descargar)]
        public async Task<IActionResult> Descargar(int id)
        {
            try
            {
                var result = await ResolveAudioFileAsync(id);
                if (result == null)
                    return NotFound();
                return PhysicalFile(result.FullPath, result.ContentType, enableRangeProcessing: true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Audio] Descargar error: {ex}");
                return StatusCode(500, ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpGet(Routes.v1.Incidenteaudio.Stream)]
        public async Task<IActionResult> Stream(int id, [FromQuery] string? token)
        {
            if (!IsDevelopment())
            {
                if (string.IsNullOrWhiteSpace(token))
                    return Unauthorized();

                if (!ValidateJwtToken(token))
                    return Unauthorized();
            }

            try
            {
                var result = await ResolveAudioFileAsync(id);
                if (result == null)
                    return NotFound();
                return PhysicalFile(result.FullPath, result.ContentType, enableRangeProcessing: true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Audio] Stream error: {ex}");
                return StatusCode(500, ex.Message);
            }
        }

        [Authorize]
        [HttpPost(Routes.v1.Incidenteaudio.Crear)]
        public IActionResult Crear([FromBody] IncidenteaudioCreateRequest request)
        {
            var result = IncidenteaudioGestor.Crear(request);
            if (!result.Ok)
                return BadRequest(result.Error);

            return Ok();
        }

        [Authorize]
        [HttpPut(Routes.v1.Incidenteaudio.Editar)]
        public IActionResult Editar(int id, [FromBody] IncidenteaudioUpdateRequest request)
        {
            var result = IncidenteaudioGestor.Editar(id, request);
            if (!result.Ok)
                return BadRequest(result.Error);

            return Ok();
        }

        [Authorize]
        [HttpDelete(Routes.v1.Incidenteaudio.Eliminar)]
        public IActionResult Eliminar(int id, [FromQuery] string mode = "soft", [FromQuery] bool deleteFile = false)
        {
            var item = IncidenteaudioGestor.ObtenerPorId(id);
            if (item == null)
                return NotFound();

            var hard = mode.Equals("hard", StringComparison.OrdinalIgnoreCase);
            var ok = hard
                ? IncidenteaudioGestor.Eliminar(id)
                : IncidenteaudioGestor.EliminarSoft(id);

            if (!ok)
                return NotFound();

            if (deleteFile && !string.IsNullOrWhiteSpace(item.Filepath))
            {
                AudioStorage.Delete(item.Filepath);
            }

            return Ok();
        }

        private static bool ValidateJwtToken(string token)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(AppConfig.JWT_SECRET);
                handler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = AppConfig.JWT_ISSUER,
                    ValidateAudience = true,
                    ValidAudience = AppConfig.JWT_AUDIENCE,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(2)
                }, out _);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsDevelopment()
        {
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "";
            return env.Equals("Development", StringComparison.OrdinalIgnoreCase);
        }

        private sealed class AudioFileResult
        {
            public string FullPath { get; init; } = "";
            public string ContentType { get; init; } = "";
            public string FileName { get; init; } = "";
        }

        private static async Task<AudioFileResult?> ResolveAudioFileAsync(int id)
        {
            var item = IncidenteaudioGestor.ObtenerPorId(id);
            if (item == null || string.IsNullOrWhiteSpace(item.Filepath))
                return null;

            var relativePath = item.Filepath;
            var currentFormat = !string.IsNullOrWhiteSpace(item.Format)
                ? item.Format.Trim().ToLowerInvariant()
                : Path.GetExtension(relativePath)?.Trim('.').ToLowerInvariant() ?? "";

            if (AppConfig.AUDIO_TRANSCODE_ENABLED)
            {
                try
                {
                    var transcode = await AudioTranscoder.TranscodeIfEnabledAsync(relativePath, currentFormat);
                    if (transcode != null && !string.IsNullOrWhiteSpace(transcode.RelativePath))
                    {
                        relativePath = transcode.RelativePath;
                        currentFormat = transcode.Format;
                        var repo = new IncidentePipelineRepository();
                        repo.ActualizarAudioArchivo(item.Id, relativePath, currentFormat, transcode.Hash);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Audio] Transcode on-demand failed: {ex.Message}");
                }
            }

            var fullPath = AudioStorage.ResolveFullPath(relativePath);
            if (!System.IO.File.Exists(fullPath))
                return null;

            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(fullPath, out var contentType))
                contentType = "application/octet-stream";

            return new AudioFileResult
            {
                FullPath = fullPath,
                ContentType = contentType,
                FileName = Path.GetFileName(fullPath)
            };
        }
    }
}
