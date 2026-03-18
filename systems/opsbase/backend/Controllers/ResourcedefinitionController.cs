using Backend.Models.Resourcedefinition;
using Backend.Negocio.Gestores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    public class ResourcedefinitionController : AppController
    {
        [Authorize]
        [HttpGet(Routes.v1.Resourcedefinition.Obtener)]
        public IActionResult Obtener()
        {
            var denial = RequirePermission("ops.resourcedefinition.view");
            if (denial != null) return denial;

            var items = ResourcedefinitionGestor.ObtenerTodos(null, null, null);
            return Ok(items);
        }

        [Authorize]
        [HttpGet(Routes.v1.Resourcedefinition.ObtenerPorId)]
        public IActionResult ObtenerPorId(int id)
        {
            var denial = RequirePermission("ops.resourcedefinition.view");
            if (denial != null) return denial;

            var item = ResourcedefinitionGestor.ObtenerPorId(id);
            if (item == null)
                return NotFound();

            return Ok(item);
        }

        [Authorize]
        [HttpPost(Routes.v1.Resourcedefinition.Crear)]
        public IActionResult Crear([FromBody] ResourcedefinitionCreateRequest request)
        {
            var denial = RequirePermission("ops.resourcedefinition.create");
            if (denial != null) return denial;

            var result = ResourcedefinitionGestor.Crear(request);
            if (!result.Ok)
                return BadRequest(result.Error);

            return Ok();
        }

        [Authorize]
        [HttpPut(Routes.v1.Resourcedefinition.Editar)]
        public IActionResult Editar(int id, [FromBody] ResourcedefinitionUpdateRequest request)
        {
            var denial = RequirePermission("ops.resourcedefinition.update");
            if (denial != null) return denial;

            var result = ResourcedefinitionGestor.Editar(id, request);
            if (!result.Ok)
                return BadRequest(result.Error);

            return Ok();
        }

        [Authorize]
        [HttpDelete(Routes.v1.Resourcedefinition.Eliminar)]
        public IActionResult Eliminar(int id)
        {
            var denial = RequirePermission("ops.resourcedefinition.delete");
            if (denial != null) return denial;

            var ok = ResourcedefinitionGestor.Eliminar(id);
            if (!ok)
                return NotFound();

            return Ok();
        }
    }
}
