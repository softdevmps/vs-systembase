using Backend.Models.Attributevalue;
using Backend.Negocio.Gestores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    public class AttributevalueController : AppController
    {
        [Authorize]
        [HttpGet(Routes.v1.Attributevalue.Obtener)]
        public IActionResult Obtener()
        {
            var items = AttributevalueGestor.ObtenerTodos(null, null, null);
            return Ok(items);
        }

        [Authorize]
        [HttpGet(Routes.v1.Attributevalue.ObtenerPorId)]
        public IActionResult ObtenerPorId(int id)
        {
            var item = AttributevalueGestor.ObtenerPorId(id);
            if (item == null)
                return NotFound();

            return Ok(item);
        }

        [Authorize]
        [HttpPost(Routes.v1.Attributevalue.Crear)]
        public IActionResult Crear([FromBody] AttributevalueCreateRequest request)
        {
            var result = AttributevalueGestor.Crear(request);
            if (!result.Ok)
                return BadRequest(result.Error);

            return Ok();
        }

        [Authorize]
        [HttpPut(Routes.v1.Attributevalue.Editar)]
        public IActionResult Editar(int id, [FromBody] AttributevalueUpdateRequest request)
        {
            var result = AttributevalueGestor.Editar(id, request);
            if (!result.Ok)
                return BadRequest(result.Error);

            return Ok();
        }

        [Authorize]
        [HttpDelete(Routes.v1.Attributevalue.Eliminar)]
        public IActionResult Eliminar(int id)
        {
            var ok = AttributevalueGestor.Eliminar(id);
            if (!ok)
                return NotFound();

            return Ok();
        }
    }
}
