using Backend.Models.Runs;
using Backend.Negocio.Gestores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    public class RunsController : AppController
    {
        [Authorize]
        [HttpGet(Routes.v1.Runs.Obtener)]
        public IActionResult Obtener()
        {
            var items = RunsGestor.ObtenerTodos(null, null, null);
            return Ok(items);
        }

        [Authorize]
        [HttpGet(Routes.v1.Runs.ObtenerPorId)]
        public IActionResult ObtenerPorId(int id)
        {
            var item = RunsGestor.ObtenerPorId(id);
            if (item == null)
                return NotFound();

            return Ok(item);
        }

        [Authorize]
        [HttpPost(Routes.v1.Runs.Crear)]
        public IActionResult Crear([FromBody] RunsCreateRequest request)
        {
            var result = RunsGestor.Crear(request);
            if (!result.Ok)
                return BadRequest(result.Error);

            return Ok();
        }

        [Authorize]
        [HttpPut(Routes.v1.Runs.Editar)]
        public IActionResult Editar(int id, [FromBody] RunsUpdateRequest request)
        {
            var result = RunsGestor.Editar(id, request);
            if (!result.Ok)
                return BadRequest(result.Error);

            return Ok();
        }

        [Authorize]
        [HttpDelete(Routes.v1.Runs.Eliminar)]
        public IActionResult Eliminar(int id)
        {
            var ok = RunsGestor.Eliminar(id);
            if (!ok)
                return NotFound();

            return Ok();
        }
    }
}
