using Backend.Models.Catalogohechos;
using Backend.Negocio.Gestores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    public class CatalogohechosController : AppController
    {
        [Authorize]
        [HttpGet(Routes.v1.Catalogohechos.Obtener)]
        public IActionResult Obtener()
        {
            var items = CatalogohechosGestor.ObtenerTodos(null, null, null);
            return Ok(items);
        }

        [Authorize]
        [HttpGet(Routes.v1.Catalogohechos.ObtenerPorId)]
        public IActionResult ObtenerPorId(int id)
        {
            var item = CatalogohechosGestor.ObtenerPorId(id);
            if (item == null)
                return NotFound();

            return Ok(item);
        }

        [Authorize]
        [HttpPost(Routes.v1.Catalogohechos.Crear)]
        public IActionResult Crear([FromBody] CatalogohechosCreateRequest request)
        {
            var result = CatalogohechosGestor.Crear(request);
            if (!result.Ok)
                return BadRequest(result.Error);

            return Ok();
        }

        [Authorize]
        [HttpPut(Routes.v1.Catalogohechos.Editar)]
        public IActionResult Editar(int id, [FromBody] CatalogohechosUpdateRequest request)
        {
            var result = CatalogohechosGestor.Editar(id, request);
            if (!result.Ok)
                return BadRequest(result.Error);

            return Ok();
        }

        [Authorize]
        [HttpDelete(Routes.v1.Catalogohechos.Eliminar)]
        public IActionResult Eliminar(int id)
        {
            var ok = CatalogohechosGestor.Eliminar(id);
            if (!ok)
                return NotFound();

            return Ok();
        }
    }
}
