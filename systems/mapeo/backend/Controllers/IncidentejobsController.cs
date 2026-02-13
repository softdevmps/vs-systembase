using Backend.Models.Incidentejobs;
using Backend.Negocio.Gestores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    public class IncidentejobsController : AppController
    {
        [Authorize]
        [HttpGet(Routes.v1.Incidentejobs.Obtener)]
        public IActionResult Obtener()
        {
            var items = IncidentejobsGestor.ObtenerTodos(null, null, null);
            return Ok(items);
        }

        [Authorize]
        [HttpGet(Routes.v1.Incidentejobs.ObtenerPorId)]
        public IActionResult ObtenerPorId(int id)
        {
            var item = IncidentejobsGestor.ObtenerPorId(id);
            if (item == null)
                return NotFound();

            return Ok(item);
        }

        [Authorize]
        [HttpPost(Routes.v1.Incidentejobs.Crear)]
        public IActionResult Crear([FromBody] IncidentejobsCreateRequest request)
        {
            var result = IncidentejobsGestor.Crear(request);
            if (!result.Ok)
                return BadRequest(result.Error);

            return Ok();
        }

        [Authorize]
        [HttpPut(Routes.v1.Incidentejobs.Editar)]
        public IActionResult Editar(int id, [FromBody] IncidentejobsUpdateRequest request)
        {
            var result = IncidentejobsGestor.Editar(id, request);
            if (!result.Ok)
                return BadRequest(result.Error);

            return Ok();
        }

        [Authorize]
        [HttpDelete(Routes.v1.Incidentejobs.Eliminar)]
        public IActionResult Eliminar(int id)
        {
            var ok = IncidentejobsGestor.Eliminar(id);
            if (!ok)
                return NotFound();

            return Ok();
        }

        [Authorize]
        [HttpPost(Routes.v1.Incidentejobs.Reintentar)]
        public IActionResult Reintentar(int id)
        {
            var ok = IncidentejobsGestor.Reintentar(id);
            if (!ok)
                return NotFound();

            return Ok();
        }
    }
}
