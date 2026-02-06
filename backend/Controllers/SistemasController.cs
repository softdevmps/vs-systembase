using Backend.Models.Sistemas;
using Backend.Negocio.Gestores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Authorize]
    public class SistemasController : AppController
    {
        [HttpGet(Routes.v1.Sistemas.Obtener)]
        public IActionResult Obtener()
        {
            var sistemas = SistemasGestor.ObtenerTodos();
            return Ok(sistemas);
        }

        [HttpGet(Routes.v1.Sistemas.ObtenerPorId)]
        public IActionResult ObtenerPorId(int id)
        {
            var sistema = SistemasGestor.ObtenerPorId(id);
            return sistema == null ? NotFound() : Ok(sistema);
        }

        [HttpGet(Routes.v1.Sistemas.ObtenerPorSlug)]
        public IActionResult ObtenerPorSlug(string slug)
        {
            var sistema = SistemasGestor.ObtenerPorSlug(slug);
            return sistema == null ? NotFound() : Ok(sistema);
        }

        [HttpPost(Routes.v1.Sistemas.Crear)]
        public IActionResult Crear([FromBody] SistemaCreateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest();

            var id = SistemasGestor.Crear(request);
            if (id == null)
                return Conflict("Slug invalido o ya existe.");

            return Ok(new { id });
        }

        [HttpPut(Routes.v1.Sistemas.Editar)]
        public IActionResult Editar(int id, [FromBody] SistemaUpdateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest();

            var ok = SistemasGestor.Editar(id, request);
            return ok ? Ok() : NotFound();
        }

        [HttpPost(Routes.v1.Sistemas.Publicar)]
        public IActionResult Publicar(int id)
        {
            var result = SistemasPublicador.Publicar(id);
            return result.Ok ? Ok(result) : BadRequest(result);
        }
    }
}
