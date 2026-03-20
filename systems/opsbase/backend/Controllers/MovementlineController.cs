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
            var denial = RequirePermission("ops.movementline.view");
            if (denial != null) return denial;

            var items = MovementlineGestor.ObtenerTodos(null, null, null);
            return Ok(items);
        }

        [Authorize]
        [HttpGet(Routes.v1.Movementline.ObtenerPorId)]
        public IActionResult ObtenerPorId(int id)
        {
            var denial = RequirePermission("ops.movementline.view");
            if (denial != null) return denial;

            var item = MovementlineGestor.ObtenerPorId(id);
            if (item == null)
                return NotFound();

            return Ok(item);
        }

        [Authorize]
        [HttpPost(Routes.v1.Movementline.Crear)]
        public IActionResult Crear([FromBody] MovementlineCreateRequest request)
        {
            var denial = RequirePermission("ops.movementline.create");
            if (denial != null) return denial;

            var actor = UsuarioToken().Usuario;
            var result = MovementlineGestor.Crear(request, actor);
            if (!result.Ok)
                return BadRequest(result.Error);

            return Ok();
        }

        [Authorize]
        [HttpPut(Routes.v1.Movementline.Editar)]
        public IActionResult Editar(int id, [FromBody] MovementlineUpdateRequest request)
        {
            var denial = RequirePermission("ops.movementline.update");
            if (denial != null) return denial;

            var actor = UsuarioToken().Usuario;
            var result = MovementlineGestor.Editar(id, request, actor);
            if (!result.Ok)
                return BadRequest(result.Error);

            return Ok();
        }

        [Authorize]
        [HttpDelete(Routes.v1.Movementline.Eliminar)]
        public IActionResult Eliminar(int id)
        {
            var denial = RequirePermission("ops.movementline.delete");
            if (denial != null) return denial;

            var actor = UsuarioToken().Usuario;
            var result = MovementlineGestor.Eliminar(id, actor);
            if (!result.Ok)
            {
                if (string.Equals(result.Error, "No encontrado", StringComparison.OrdinalIgnoreCase))
                    return NotFound();
                return BadRequest(result.Error);
            }

            return Ok();
        }
    }
}
