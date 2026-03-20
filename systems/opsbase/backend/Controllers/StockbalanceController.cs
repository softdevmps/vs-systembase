using Backend.Models.Stockbalance;
using Backend.Negocio.Gestores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    public class StockbalanceController : AppController
    {
        [Authorize]
        [HttpGet(Routes.v1.Stockbalance.Obtener)]
        public IActionResult Obtener()
        {
            var denial = RequirePermission("ops.stockbalance.view");
            if (denial != null) return denial;

            var items = StockbalanceGestor.ObtenerTodos(null, null, null);
            return Ok(items);
        }

        [Authorize]
        [HttpGet(Routes.v1.Stockbalance.ObtenerPorId)]
        public IActionResult ObtenerPorId(int id)
        {
            var denial = RequirePermission("ops.stockbalance.view");
            if (denial != null) return denial;

            var item = StockbalanceGestor.ObtenerPorId(id);
            if (item == null)
                return NotFound();

            return Ok(item);
        }

        [Authorize]
        [HttpPost(Routes.v1.Stockbalance.Crear)]
        public IActionResult Crear([FromBody] StockbalanceCreateRequest request)
        {
            var denial = RequirePermission("ops.stockbalance.create");
            if (denial != null) return denial;

            var result = StockbalanceGestor.Crear(request);
            if (!result.Ok)
                return BadRequest(result.Error);

            return Ok();
        }

        [Authorize]
        [HttpPut(Routes.v1.Stockbalance.Editar)]
        public IActionResult Editar(int id, [FromBody] StockbalanceUpdateRequest request)
        {
            var denial = RequirePermission("ops.stockbalance.update");
            if (denial != null) return denial;

            var result = StockbalanceGestor.Editar(id, request);
            if (!result.Ok)
                return BadRequest(result.Error);

            return Ok();
        }

        [Authorize]
        [HttpDelete(Routes.v1.Stockbalance.Eliminar)]
        public IActionResult Eliminar(int id)
        {
            var denial = RequirePermission("ops.stockbalance.delete");
            if (denial != null) return denial;

            var ok = StockbalanceGestor.Eliminar(id);
            if (!ok)
                return NotFound();

            return Ok();
        }
    }
}
