using Backend.Models.Operationaudit;
using Backend.Negocio.Gestores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    public class OperationauditController : AppController
    {
        [Authorize]
        [HttpGet(Routes.v1.Operationaudit.Obtener)]
        public IActionResult Obtener()
        {
            var denial = RequirePermission("ops.operationaudit.view");
            if (denial != null) return denial;

            var items = OperationauditGestor.ObtenerTodos(null, null, null);
            return Ok(items);
        }

        [Authorize]
        [HttpGet(Routes.v1.Operationaudit.ObtenerPorId)]
        public IActionResult ObtenerPorId(int id)
        {
            var denial = RequirePermission("ops.operationaudit.view");
            if (denial != null) return denial;

            var item = OperationauditGestor.ObtenerPorId(id);
            if (item == null)
                return NotFound();

            return Ok(item);
        }

        [Authorize]
        [HttpGet(Routes.v1.Operationaudit.TimelineByResourceInstance)]
        public IActionResult TimelineByResourceInstance(int resourceInstanceId)
        {
            var denial = RequirePermission("ops.operationaudit.timeline");
            if (denial != null) return denial;

            var items = OperationauditGestor.ObtenerTimelinePorResourceInstance(resourceInstanceId);
            return Ok(items);
        }

        [Authorize]
        [HttpPost(Routes.v1.Operationaudit.Crear)]
        public IActionResult Crear([FromBody] OperationauditCreateRequest request)
        {
            var denial = RequirePermission("ops.operationaudit.create");
            if (denial != null) return denial;

            var result = OperationauditGestor.Crear(request);
            if (!result.Ok)
                return BadRequest(result.Error);

            return Ok();
        }

        [Authorize]
        [HttpPut(Routes.v1.Operationaudit.Editar)]
        public IActionResult Editar(int id, [FromBody] OperationauditUpdateRequest request)
        {
            var denial = RequirePermission("ops.operationaudit.update");
            if (denial != null) return denial;

            var result = OperationauditGestor.Editar(id, request);
            if (!result.Ok)
                return BadRequest(result.Error);

            return Ok();
        }

        [Authorize]
        [HttpDelete(Routes.v1.Operationaudit.Eliminar)]
        public IActionResult Eliminar(int id)
        {
            var denial = RequirePermission("ops.operationaudit.delete");
            if (denial != null) return denial;

            var ok = OperationauditGestor.Eliminar(id);
            if (!ok)
                return NotFound();

            return Ok();
        }
    }
}
