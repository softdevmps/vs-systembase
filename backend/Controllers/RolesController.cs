using Backend.Models.Requests.Roles;
using Backend.Negocio.Gestores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Authorize]
    public class RolesController : AppController
    {
        [HttpGet(Routes.v1.Roles.Obtener)]
        public IActionResult ObtenerRoles()
        {
            var roles = RolesGestor.ObtenerTodos();
            return Ok(roles);
        }

        [HttpGet(Routes.v1.Roles.ObtenerPorId)]
        public IActionResult ObtenerPorId(int id)
        {
            var rol = RolesGestor.ObtenerPorId(id);

            if (rol == null)
                return NotFound();

            return Ok(rol);
        }


        [HttpPost(Routes.v1.Roles.Crear)]
        public IActionResult Crear([FromBody] RolCreateRequest request)
        {
            RolesGestor.Crear(request);
            return Ok();
        }

        [HttpPut(Routes.v1.Roles.Editar)]
        public IActionResult Editar(int id, [FromBody] RolUpdateRequest request)
        {
            var ok = RolesGestor.Editar(id, request);
            if (!ok)
                return NotFound();

            return Ok();
        }

        [HttpPut(Routes.v1.Roles.Estado)]
        public IActionResult CambiarEstado(int id, [FromQuery] bool activo)
        {
            var ok = RolesGestor.CambiarEstado(id, activo);
            if (!ok)
                return NotFound();

            return Ok();
        }

        [HttpPut(Routes.v1.Roles.AsignarMenus)]
        public IActionResult AsignarMenus(int id, [FromBody] RolMenusRequest request)
        {
            var ok = RolesGestor.AsignarMenus(id, request.MenusIds);
            if (!ok)
                return NotFound();

            return Ok();
        }
    }
}