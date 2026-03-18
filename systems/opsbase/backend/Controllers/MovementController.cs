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
            var denial = RequirePermission("ops.movement.view");
            if (denial != null) return denial;

            var items = MovementGestor.ObtenerTodos(null, null, null);
            return Ok(items);
        }

        [Authorize]
        [HttpGet(Routes.v1.Movement.ObtenerPorId)]
        public IActionResult ObtenerPorId(int id)
        {
            var denial = RequirePermission("ops.movement.view");
            if (denial != null) return denial;

            var item = MovementGestor.ObtenerPorId(id);
            if (item == null)
                return NotFound();

            return Ok(item);
        }

        [Authorize]
        [HttpPost(Routes.v1.Movement.Crear)]
        public IActionResult Crear([FromBody] MovementCreateRequest request)
        {
            var denial = RequirePermission("ops.movement.create");
            if (denial != null) return denial;

            var actor = UsuarioToken().Usuario;
            var result = MovementGestor.Crear(request, actor);
            if (!result.Ok)
                return BadRequest(result.Error);

            return Ok();
        }

        [Authorize]
        [HttpPut(Routes.v1.Movement.Editar)]
        public IActionResult Editar(int id, [FromBody] MovementUpdateRequest request)
        {
            var status = (request.Status ?? string.Empty).Trim();
            var permission = string.Equals(status, "confirmado", StringComparison.OrdinalIgnoreCase)
                ? "ops.movement.confirm"
                : "ops.movement.update";

            var denial = RequirePermission(permission);
            if (denial != null) return denial;

            var actor = UsuarioToken().Usuario;
            var result = MovementGestor.Editar(id, request, actor);
            if (!result.Ok)
                return BadRequest(result.Error);

            return Ok();
        }

        [Authorize]
        [HttpDelete(Routes.v1.Movement.Eliminar)]
        public IActionResult Eliminar(int id)
        {
            var denial = RequirePermission("ops.movement.delete");
            if (denial != null) return denial;

            var actor = UsuarioToken().Usuario;
            var result = MovementGestor.Eliminar(id, actor);
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
