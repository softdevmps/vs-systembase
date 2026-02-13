using Backend.Models.Incidenteubicacion;
using Backend.Negocio.Gestores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    public class IncidenteubicacionController : AppController
    {
        [Authorize]
        [HttpGet(Routes.v1.Incidenteubicacion.Obtener)]
        public IActionResult Obtener()
        {
            var items = IncidenteubicacionGestor.ObtenerTodos(null, null, null);
            return Ok(items);
        }

        [Authorize]
        [HttpGet(Routes.v1.Incidenteubicacion.ObtenerPorId)]
        public IActionResult ObtenerPorId(int id)
        {
            var item = IncidenteubicacionGestor.ObtenerPorId(id);
            if (item == null)
                return NotFound();

            return Ok(item);
        }

        [Authorize]
        [HttpPost(Routes.v1.Incidenteubicacion.Crear)]
        public IActionResult Crear([FromBody] IncidenteubicacionCreateRequest request)
        {
            var result = IncidenteubicacionGestor.Crear(request);
            if (!result.Ok)
                return BadRequest(result.Error);

            return Ok();
        }

        [Authorize]
        [HttpPut(Routes.v1.Incidenteubicacion.Editar)]
        public IActionResult Editar(int id, [FromBody] IncidenteubicacionUpdateRequest request)
        {
            var result = IncidenteubicacionGestor.Editar(id, request);
            if (!result.Ok)
                return BadRequest(result.Error);

            return Ok();
        }

        [Authorize]
        [HttpDelete(Routes.v1.Incidenteubicacion.Eliminar)]
        public IActionResult Eliminar(int id)
        {
            var ok = IncidenteubicacionGestor.Eliminar(id);
            if (!ok)
                return NotFound();

            return Ok();
        }
    }
}
