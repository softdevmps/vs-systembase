using Backend.Models.Templates;
using Backend.Negocio.Gestores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    public class TemplatesController : AppController
    {
        [Authorize]
        [HttpGet(Routes.v1.Templates.Obtener)]
        public IActionResult Obtener()
        {
            var items = TemplatesGestor.ObtenerTodos(null, null, null);
            return Ok(items);
        }

        [Authorize]
        [HttpGet(Routes.v1.Templates.ObtenerPorId)]
        public IActionResult ObtenerPorId(int id)
        {
            var item = TemplatesGestor.ObtenerPorId(id);
            if (item == null)
                return NotFound();

            return Ok(item);
        }

        [Authorize]
        [HttpPost(Routes.v1.Templates.Crear)]
        public IActionResult Crear([FromBody] TemplatesCreateRequest request)
        {
            var result = TemplatesGestor.Crear(request);
            if (!result.Ok)
                return BadRequest(result.Error);

            return Ok();
        }

        [Authorize]
        [HttpPut(Routes.v1.Templates.Editar)]
        public IActionResult Editar(int id, [FromBody] TemplatesUpdateRequest request)
        {
            var result = TemplatesGestor.Editar(id, request);
            if (!result.Ok)
                return BadRequest(result.Error);

            return Ok();
        }

        [Authorize]
        [HttpDelete(Routes.v1.Templates.Eliminar)]
        public IActionResult Eliminar(int id)
        {
            var ok = TemplatesGestor.Eliminar(id);
            if (!ok)
                return NotFound();

            return Ok();
        }
    }
}
