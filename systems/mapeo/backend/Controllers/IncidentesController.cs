using Backend.Models.Incidentes;
using Backend.Negocio.Gestores;
using Backend.Negocio.Pipeline;
using Backend.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace Backend.Controllers
{
    [ApiController]
    public class IncidentesController : AppController
    {
        [Authorize]
        [HttpGet(Routes.v1.Incidentes.Obtener)]
        public IActionResult Obtener()
        {
            var items = IncidentesGestor.ObtenerTodos(null, null, null);
            return Ok(items);
        }

        [Authorize]
        [HttpGet(Routes.v1.Incidentes.ObtenerPorId)]
        public IActionResult ObtenerPorId(int id)
        {
            var item = IncidentesGestor.ObtenerPorId(id);
            if (item == null)
                return NotFound();

            return Ok(item);
        }

        [Authorize]
        [HttpPost(Routes.v1.Incidentes.Crear)]
        public IActionResult Crear([FromBody] IncidentesCreateRequest request)
        {
            var result = IncidentesGestor.Crear(request);
            if (!result.Ok)
                return BadRequest(result.Error);

            return Ok();
        }

        [Authorize]
        [HttpPut(Routes.v1.Incidentes.Editar)]
        public IActionResult Editar(int id, [FromBody] IncidentesUpdateRequest request)
        {
            var result = IncidentesGestor.Editar(id, request);
            if (!result.Ok)
                return BadRequest(result.Error);

            return Ok();
        }

        [Authorize]
        [HttpDelete(Routes.v1.Incidentes.Eliminar)]
        public IActionResult Eliminar(int id)
        {
            var ok = IncidentesGestor.Eliminar(id);
            if (!ok)
                return NotFound();

            return Ok();
        }

        [Authorize]
        [HttpPost(Routes.v1.Incidentes.UploadAudio)]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(100_000_000)]
        public async Task<IActionResult> UploadAudio([FromForm] IncidentesAudioUploadRequest request)
        {
            var audio = request.Audio;
            if (audio == null || audio.Length == 0)
                return BadRequest("Audio requerido.");

            var allowed = AppConfig.AUDIO_ALLOWED_EXT
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(e => e.ToLowerInvariant())
                .ToHashSet();

            var ext = Path.GetExtension(audio.FileName)?.ToLowerInvariant() ?? "";
            if (ext.StartsWith("."))
                ext = ext[1..];

            if (!allowed.Contains(ext))
                return BadRequest($"Formato no permitido. Permitidos: {string.Join(", ", allowed)}");

            var maxBytes = AppConfig.AUDIO_MAX_MB * 1024L * 1024L;
            if (audio.Length > maxBytes)
                return BadRequest($"Archivo demasiado grande. Max: {AppConfig.AUDIO_MAX_MB} MB");

            var storage = await AudioStorage.SaveAsync(audio);
            var repo = new IncidentePipelineRepository();

            var (incidenteId, audioId, jobId) = repo.CrearIncidenteConAudio(
                request.Descripcion ?? "Pendiente",
                DateTime.UtcNow,
                "pending",
                storage.RelativePath,
                ext,
                storage.Hash
            );

            return Ok(new
            {
                incidenteId,
                audioId,
                jobId,
                status = "queued",
                message = "Audio recibido. En cola de procesamiento."
            });
        }
    }
}
