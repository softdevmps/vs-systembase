using Backend.Models.Movementline;
using Backend.Negocio.Gestores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    public class MovementlineController : AppController
    {
        [Authorize]
        [HttpGet(Routes.v1.Movementline.Obtener)]
        public IActionResult Obtener()
        {
            var items = MovementlineGestor.ObtenerTodos(null, null, null);
            return Ok(items);
        }

        [Authorize]
        [HttpGet(Routes.v1.Movementline.ObtenerPorId)]
        public IActionResult ObtenerPorId(int id)
        {
            var item = MovementlineGestor.ObtenerPorId(id);
            if (item == null)
                return NotFound();

            return Ok(item);
        }

        [Authorize]
        [HttpPost(Routes.v1.Movementline.Crear)]
        public IActionResult Crear([FromBody] MovementlineCreateRequest request)
        {
            var result = MovementlineGestor.Crear(request);
            if (!result.Ok)
                return BadRequest(result.Error);

            return Ok();
        }

        [Authorize]
        [HttpPut(Routes.v1.Movementline.Editar)]
        public IActionResult Editar(int id, [FromBody] MovementlineUpdateRequest request)
        {
            var result = MovementlineGestor.Editar(id, request);
            if (!result.Ok)
                return BadRequest(result.Error);

            return Ok();
        }

        [Authorize]
        [HttpDelete(Routes.v1.Movementline.Eliminar)]
        public IActionResult Eliminar(int id)
        {
            var ok = MovementlineGestor.Eliminar(id);
            if (!ok)
                return NotFound();

            return Ok();
        }
    }
}
