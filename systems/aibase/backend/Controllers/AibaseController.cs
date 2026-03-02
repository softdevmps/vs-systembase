using Backend.Models.AiBase;
using Backend.Negocio.Gestores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Authorize]
    public class AibaseController : AppController
    {
        [HttpGet(Routes.v1.Aibase.ObtenerTemplates)]
        public IActionResult ObtenerTemplates()
        {
            return Ok(AibaseTemplatesGestor.ObtenerActivos());
        }

        [HttpGet(Routes.v1.Aibase.ObtenerProyectos)]
        public IActionResult ObtenerProyectos()
        {
            return Ok(AibaseProjectsGestor.ObtenerTodos());
        }

        [HttpGet(Routes.v1.Aibase.ObtenerProyectoPorId)]
        public IActionResult ObtenerProyectoPorId(int id)
        {
            var proyecto = AibaseProjectsGestor.ObtenerPorId(id);
            return proyecto == null ? NotFound() : Ok(proyecto);
        }

        [HttpPost(Routes.v1.Aibase.CrearProyecto)]
        public IActionResult CrearProyecto([FromBody] AibaseProjectCreateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var usuario = UsuarioToken();
            var result = AibaseProjectsGestor.Crear(request, usuario.UsuarioId);
            if (result.projectId == null)
                return BadRequest(new { message = result.error ?? "No se pudo crear el proyecto." });

            return Ok(new { id = result.projectId.Value });
        }

        [HttpGet(Routes.v1.Aibase.ObtenerRunsProyecto)]
        public IActionResult ObtenerRunsProyecto(int projectId)
        {
            return Ok(AibaseRunsGestor.ObtenerPorProyecto(projectId));
        }

        [HttpPost(Routes.v1.Aibase.CrearRunProyecto)]
        public async Task<IActionResult> CrearRunProyecto(int projectId, [FromBody] AibaseRunCreateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var usuario = UsuarioToken();
            var result = await AibaseRunsGestor.CrearYDespacharAsync(
                projectId,
                request,
                usuario.UsuarioId,
                HttpContext.RequestServices.GetRequiredService<ILogger<AibaseController>>());

            if (result.runId == null)
                return BadRequest(new { message = result.error ?? "No se pudo crear el run." });

            return Ok(new { id = result.runId.Value });
        }

        [HttpGet(Routes.v1.Aibase.ObtenerRunPorId)]
        public IActionResult ObtenerRunPorId(int id)
        {
            var run = AibaseRunsGestor.ObtenerPorId(id);
            return run == null ? NotFound() : Ok(run);
        }

        [HttpPost(Routes.v1.Aibase.SincronizarRunPorId)]
        public async Task<IActionResult> SincronizarRunPorId(int id)
        {
            var result = await AibaseRunsGestor.SincronizarAsync(id, HttpContext.RequestServices.GetRequiredService<ILogger<AibaseController>>());
            if (result.run == null)
                return NotFound(new { message = result.error ?? "Run no encontrado." });

            return Ok(result.run);
        }
    }
}
