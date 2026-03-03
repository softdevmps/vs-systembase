using Backend.Models.Projects;
using Backend.Negocio.Gestores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    public class ProjectsController : AppController
    {
        [Authorize]
        [HttpGet(Routes.v1.Projects.Obtener)]
        public IActionResult Obtener()
        {
            var items = ProjectsGestor.ObtenerTodos(null, null, null);
            return Ok(items);
        }

        [Authorize]
        [HttpGet(Routes.v1.Projects.ObtenerPorId)]
        public IActionResult ObtenerPorId(int id)
        {
            var item = ProjectsGestor.ObtenerPorId(id);
            if (item == null)
                return NotFound();

            return Ok(item);
        }

        [Authorize]
        [HttpPost(Routes.v1.Projects.Crear)]
        public IActionResult Crear([FromBody] ProjectsCreateRequest request)
        {
            var result = ProjectsGestor.Crear(request);
            if (!result.Ok)
                return BadRequest(result.Error);

            return Ok();
        }

        [Authorize]
        [HttpPut(Routes.v1.Projects.Editar)]
        public IActionResult Editar(int id, [FromBody] ProjectsUpdateRequest request)
        {
            var result = ProjectsGestor.Editar(id, request);
            if (!result.Ok)
                return BadRequest(result.Error);

            return Ok();
        }

        [Authorize]
        [HttpDelete(Routes.v1.Projects.Eliminar)]
        public IActionResult Eliminar(int id)
        {
            var ok = ProjectsGestor.Eliminar(id);
            if (!ok)
                return NotFound();

            return Ok();
        }
    }
}
