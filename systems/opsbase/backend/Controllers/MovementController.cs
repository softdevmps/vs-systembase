using Backend.Models.Movement;
using Backend.Negocio.Gestores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    public class MovementController : AppController
    {
        [Authorize]
        [HttpGet(Routes.v1.Movement.Obtener)]
        public IActionResult Obtener()
        {
            var items = MovementGestor.ObtenerTodos(null, null, null);
            return Ok(items);
        }

        [Authorize]
        [HttpGet(Routes.v1.Movement.ObtenerPorId)]
        public IActionResult ObtenerPorId(int id)
        {
            var item = MovementGestor.ObtenerPorId(id);
            if (item == null)
                return NotFound();

            return Ok(item);
        }

        [Authorize]
        [HttpPost(Routes.v1.Movement.Crear)]
        public IActionResult Crear([FromBody] MovementCreateRequest request)
        {
            var result = MovementGestor.Crear(request);
            if (!result.Ok)
                return BadRequest(result.Error);

            return Ok();
        }

        [Authorize]
        [HttpPut(Routes.v1.Movement.Editar)]
        public IActionResult Editar(int id, [FromBody] MovementUpdateRequest request)
        {
            var result = MovementGestor.Editar(id, request);
            if (!result.Ok)
                return BadRequest(result.Error);

            return Ok();
        }

        [Authorize]
        [HttpDelete(Routes.v1.Movement.Eliminar)]
        public IActionResult Eliminar(int id)
        {
            var ok = MovementGestor.Eliminar(id);
            if (!ok)
                return NotFound();

            return Ok();
        }
    }
}
