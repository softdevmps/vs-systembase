using Backend.Models.Attributedefinition;
using Backend.Negocio.Gestores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    public class AttributedefinitionController : AppController
    {
        [Authorize]
        [HttpGet(Routes.v1.Attributedefinition.Obtener)]
        public IActionResult Obtener()
        {
            var denial = RequirePermission("ops.attributedefinition.view");
            if (denial != null) return denial;

            var items = AttributedefinitionGestor.ObtenerTodos(null, null, null);
            return Ok(items);
        }

        [Authorize]
        [HttpGet(Routes.v1.Attributedefinition.ObtenerPorId)]
        public IActionResult ObtenerPorId(int id)
        {
            var denial = RequirePermission("ops.attributedefinition.view");
            if (denial != null) return denial;

            var item = AttributedefinitionGestor.ObtenerPorId(id);
            if (item == null)
                return NotFound();

            return Ok(item);
        }

        [Authorize]
        [HttpPost(Routes.v1.Attributedefinition.Crear)]
        public IActionResult Crear([FromBody] AttributedefinitionCreateRequest request)
        {
            var denial = RequirePermission("ops.attributedefinition.create");
            if (denial != null) return denial;

            var result = AttributedefinitionGestor.Crear(request);
            if (!result.Ok)
                return BadRequest(result.Error);

            return Ok();
        }

        [Authorize]
        [HttpPut(Routes.v1.Attributedefinition.Editar)]
        public IActionResult Editar(int id, [FromBody] AttributedefinitionUpdateRequest request)
        {
            var denial = RequirePermission("ops.attributedefinition.update");
            if (denial != null) return denial;

            var result = AttributedefinitionGestor.Editar(id, request);
            if (!result.Ok)
                return BadRequest(result.Error);

            return Ok();
        }

        [Authorize]
        [HttpDelete(Routes.v1.Attributedefinition.Eliminar)]
        public IActionResult Eliminar(int id)
        {
            var denial = RequirePermission("ops.attributedefinition.delete");
            if (denial != null) return denial;

            var ok = AttributedefinitionGestor.Eliminar(id);
            if (!ok)
                return NotFound();

            return Ok();
        }
    }
}
