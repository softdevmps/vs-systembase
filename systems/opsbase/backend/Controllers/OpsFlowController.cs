using Backend.Models.OpsFlow;
using Backend.Negocio.Gestores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    public class OpsFlowController : AppController
    {
        [Authorize]
        [HttpPost(Routes.v1.OpsFlow.CrearRecepcion)]
        public IActionResult CrearRecepcion([FromBody] RecepcionCreateRequest request)
        {
            var denyCreateMovement = RequirePermission("ops.movement.create");
            if (denyCreateMovement != null) return denyCreateMovement;

            var denyCreateLine = RequirePermission("ops.movementline.create");
            if (denyCreateLine != null) return denyCreateLine;

            if (request.Confirmar)
            {
                var denyConfirm = RequirePermission("ops.movement.confirm");
                if (denyConfirm != null) return denyConfirm;
            }

            var actor = UsuarioToken().Usuario;
            var result = OpsFlowGestor.CrearRecepcion(request, actor);
            if (!result.Ok)
                return BadRequest(result.Error);

            return Ok(result.Data);
        }

        [Authorize]
        [HttpPost(Routes.v1.OpsFlow.CrearDespacho)]
        public IActionResult CrearDespacho([FromBody] DespachoCreateRequest request)
        {
            var denyCreateMovement = RequirePermission("ops.movement.create");
            if (denyCreateMovement != null) return denyCreateMovement;

            var denyCreateLine = RequirePermission("ops.movementline.create");
            if (denyCreateLine != null) return denyCreateLine;

            if (request.Confirmar)
            {
                var denyConfirm = RequirePermission("ops.movement.confirm");
                if (denyConfirm != null) return denyConfirm;
            }

            var actor = UsuarioToken().Usuario;
            var result = OpsFlowGestor.CrearDespacho(request, actor);
            if (!result.Ok)
                return BadRequest(result.Error);

            return Ok(result.Data);
        }
    }
}
