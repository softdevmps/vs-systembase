using Backend.Models.Incidenteextraccion;
using Backend.Negocio.Gestores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    public class IncidenteextraccionController : AppController
    {
        [Authorize]
        [HttpGet(Routes.v1.Incidenteextraccion.Obtener)]
        public IActionResult Obtener()
        {
            var items = IncidenteextraccionGestor.ObtenerTodos(null, null, null);
            return Ok(items);
        }

        [Authorize]
        [HttpGet(Routes.v1.Incidenteextraccion.ObtenerPorId)]
        public IActionResult ObtenerPorId(int id)
        {
            var item = IncidenteextraccionGestor.ObtenerPorId(id);
            if (item == null)
                return NotFound();

            return Ok(item);
        }

        [Authorize]
        [HttpPost(Routes.v1.Incidenteextraccion.Crear)]
        public IActionResult Crear([FromBody] IncidenteextraccionCreateRequest request)
        {
            var result = IncidenteextraccionGestor.Crear(request);
            if (!result.Ok)
                return BadRequest(result.Error);

            return Ok();
        }

        [Authorize]
        [HttpPut(Routes.v1.Incidenteextraccion.Editar)]
        public IActionResult Editar(int id, [FromBody] IncidenteextraccionUpdateRequest request)
        {
            var result = IncidenteextraccionGestor.Editar(id, request);
            if (!result.Ok)
                return BadRequest(result.Error);

            return Ok();
        }

        [Authorize]
        [HttpDelete(Routes.v1.Incidenteextraccion.Eliminar)]
        public IActionResult Eliminar(int id)
        {
            var ok = IncidenteextraccionGestor.Eliminar(id);
            if (!ok)
                return NotFound();

            return Ok();
        }
    }
}
