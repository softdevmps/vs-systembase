using Backend.Negocio.Gestores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    public class SecurityController : AppController
    {
        [Authorize]
        [HttpGet(Routes.v1.Security.MyPermissions)]
        public IActionResult MyPermissions()
        {
            var usuario = UsuarioToken().Usuario;
            var context = SecurityGestor.GetUserPermissions(usuario);

            return Ok(new
            {
                Usuario = usuario,
                context.Enabled,
                context.IsAdmin,
                context.Permissions,
                PermissionCount = context.Permissions.Count
            });
        }

        [Authorize]
        [HttpGet(Routes.v1.Security.Can)]
        public IActionResult Can(string permissionCode)
        {
            var usuario = UsuarioToken().Usuario;
            var allowed = SecurityGestor.HasPermission(usuario, permissionCode);

            return Ok(new
            {
                Usuario = usuario,
                Permission = permissionCode,
                Allowed = allowed
            });
        }
    }
}
