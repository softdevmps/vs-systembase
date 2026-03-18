using Backend.Models.Resourceinstance;
using Backend.Negocio.Gestores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    public class ResourceinstanceController : AppController
    {
        [Authorize]
        [HttpGet(Routes.v1.Resourceinstance.Obtener)]
        public IActionResult Obtener()
        {
            var denial = RequirePermission("ops.resourceinstance.view");
            if (denial != null) return denial;

            var items = ResourceinstanceGestor.ObtenerTodos(null, null, null);
            return Ok(items);
        }

        [Authorize]
        [HttpGet(Routes.v1.Resourceinstance.ObtenerPorId)]
        public IActionResult ObtenerPorId(int id)
        {
            var denial = RequirePermission("ops.resourceinstance.view");
            if (denial != null) return denial;

            var item = ResourceinstanceGestor.ObtenerPorId(id);
            if (item == null)
                return NotFound();

            return Ok(item);
        }

        [Authorize]
        [HttpPost(Routes.v1.Resourceinstance.Crear)]
        public IActionResult Crear([FromBody] ResourceinstanceCreateRequest request)
        {
            var denial = RequirePermission("ops.resourceinstance.create");
            if (denial != null) return denial;

            var result = ResourceinstanceGestor.Crear(request);
            if (!result.Ok)
                return BadRequest(result.Error);

            return Ok();
        }

        [Authorize]
        [HttpPut(Routes.v1.Resourceinstance.Editar)]
        public IActionResult Editar(int id, [FromBody] ResourceinstanceUpdateRequest request)
        {
            var denial = RequirePermission("ops.resourceinstance.update");
            if (denial != null) return denial;

            var result = ResourceinstanceGestor.Editar(id, request);
            if (!result.Ok)
                return BadRequest(result.Error);

            return Ok();
        }

        [Authorize]
        [HttpDelete(Routes.v1.Resourceinstance.Eliminar)]
        public IActionResult Eliminar(int id)
        {
            var denial = RequirePermission("ops.resourceinstance.delete");
            if (denial != null) return denial;

            var ok = ResourceinstanceGestor.Eliminar(id);
            if (!ok)
                return NotFound();

            return Ok();
        }
    }
}
