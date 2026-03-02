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
    }
}
