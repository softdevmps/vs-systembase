using Backend.Models.OpsDashboard;
using Backend.Negocio.Gestores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    public class OpsDashboardController : AppController
    {
        [Authorize]
        [HttpGet(Routes.v1.OpsDashboard.DepositosMapa)]
        public IActionResult ObtenerDepositosMapa([FromQuery] int? rubroId = null)
        {
            var denial = RequireOpsDashboardViewPermissions();
            if (denial != null) return denial;

            var data = OpsDashboardGestor.ObtenerDepositosMapa(rubroId);
            return Ok(data);
        }

        [Authorize]
        [HttpGet(Routes.v1.OpsDashboard.DepositoContexto)]
        public IActionResult ObtenerDepositoContexto(int locationId, [FromQuery] int limit = 40)
        {
            var denial = RequireOpsDashboardViewPermissions();
            if (denial != null) return denial;

            var result = OpsDashboardGestor.ObtenerDepositoContexto(locationId, limit);
            if (!result.Ok)
                return BadRequest(result.Error);

            return Ok(result.Data);
        }

        [Authorize]
        [HttpPost(Routes.v1.OpsDashboard.CrearDeposito)]
        public IActionResult CrearDeposito([FromBody] OpsDepositoCreateRequest request)
        {
            var denial = RequirePermission("ops.location.create");
            if (denial != null) return denial;

            var actor = UsuarioToken().Usuario;
            var result = OpsDashboardGestor.CrearDeposito(request, actor);
            if (!result.Ok)
                return BadRequest(result.Error);

            return Ok(result.Data);
        }

        [Authorize]
        [HttpPut(Routes.v1.OpsDashboard.EditarDeposito)]
        public IActionResult EditarDeposito(int locationId, [FromBody] OpsDepositoUpdateRequest request)
        {
            var denial = RequirePermission("ops.location.update");
            if (denial != null) return denial;

            var actor = UsuarioToken().Usuario;
            var result = OpsDashboardGestor.EditarDeposito(locationId, request, actor);
            if (!result.Ok)
                return BadRequest(result.Error);

            return Ok(result.Data);
        }

        [Authorize]
        [HttpDelete(Routes.v1.OpsDashboard.EliminarDeposito)]
        public IActionResult EliminarDeposito(int locationId)
        {
            var denial = RequirePermission("ops.location.delete");
            if (denial != null) return denial;

            var actor = UsuarioToken().Usuario;
            var result = OpsDashboardGestor.EliminarDeposito(locationId, actor);
            if (!result.Ok)
                return BadRequest(result.Error);

            return Ok(result.Data);
        }

        private IActionResult? RequireOpsDashboardViewPermissions()
        {
            var permissions = new[]
            {
                "ops.location.view",
                "ops.stockbalance.view",
                "ops.movement.view",
                "ops.operationaudit.view"
            };

            foreach (var permission in permissions)
            {
                var denial = RequirePermission(permission);
                if (denial != null) return denial;
            }

            return null;
        }
    }
}
