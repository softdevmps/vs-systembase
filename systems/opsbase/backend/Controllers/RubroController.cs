using Backend.Models.Rubro;
using Backend.Negocio.Gestores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    public class RubroController : AppController
    {
        [Authorize]
        [HttpGet(Routes.v1.Rubro.Obtener)]
        public IActionResult Obtener()
        {
            var denial = RequirePermission("ops.resourcedefinition.view");
            if (denial != null) return denial;

            var items = RubroGestor.ObtenerTodos();
            return Ok(items);
        }

        [Authorize]
        [HttpGet(Routes.v1.Rubro.ObtenerPorId)]
        public IActionResult ObtenerPorId(int id)
        {
            var denial = RequirePermission("ops.resourcedefinition.view");
            if (denial != null) return denial;

            var item = RubroGestor.ObtenerPorId(id);
            if (item == null)
                return NotFound();

            return Ok(item);
        }

        [Authorize]
        [HttpPost(Routes.v1.Rubro.Crear)]
        public IActionResult Crear([FromBody] RubroCreateRequest request)
        {
            var denial = RequirePermission("ops.resourcedefinition.create");
            if (denial != null) return denial;

            var result = RubroGestor.Crear(request);
            if (!result.Ok)
                return BadRequest(result.Error);

            return Ok();
        }

        [Authorize]
        [HttpPut(Routes.v1.Rubro.Editar)]
        public IActionResult Editar(int id, [FromBody] RubroUpdateRequest request)
        {
            var denial = RequirePermission("ops.resourcedefinition.update");
            if (denial != null) return denial;

            var result = RubroGestor.Editar(id, request);
            if (!result.Ok)
                return BadRequest(result.Error);

            return Ok();
        }

        [Authorize]
        [HttpDelete(Routes.v1.Rubro.Eliminar)]
        public IActionResult Eliminar(int id)
        {
            var denial = RequirePermission("ops.resourcedefinition.delete");
            if (denial != null) return denial;

            var ok = RubroGestor.Eliminar(id);
            if (!ok)
                return NotFound();

            return Ok();
        }
    }
}
