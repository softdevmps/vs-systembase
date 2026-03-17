using Backend.Models.Location;
using Backend.Negocio.Gestores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    public class LocationController : AppController
    {
        [Authorize]
        [HttpGet(Routes.v1.Location.Obtener)]
        public IActionResult Obtener()
        {
            var items = LocationGestor.ObtenerTodos(null, null, null);
            return Ok(items);
        }

        [Authorize]
        [HttpGet(Routes.v1.Location.ObtenerPorId)]
        public IActionResult ObtenerPorId(int id)
        {
            var item = LocationGestor.ObtenerPorId(id);
            if (item == null)
                return NotFound();

            return Ok(item);
        }

        [Authorize]
        [HttpPost(Routes.v1.Location.Crear)]
        public IActionResult Crear([FromBody] LocationCreateRequest request)
        {
            var result = LocationGestor.Crear(request);
            if (!result.Ok)
                return BadRequest(result.Error);

            return Ok();
        }

        [Authorize]
        [HttpPut(Routes.v1.Location.Editar)]
        public IActionResult Editar(int id, [FromBody] LocationUpdateRequest request)
        {
            var result = LocationGestor.Editar(id, request);
            if (!result.Ok)
                return BadRequest(result.Error);

            return Ok();
        }

        [Authorize]
        [HttpDelete(Routes.v1.Location.Eliminar)]
        public IActionResult Eliminar(int id)
        {
            var ok = LocationGestor.Eliminar(id);
            if (!ok)
                return NotFound();

            return Ok();
        }
    }
}
