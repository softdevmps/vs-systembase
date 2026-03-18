using Backend.Models.Auth;
using Backend.Negocio.Gestores;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Backend.Controllers
{
    public class AppController : ControllerBase
    {
        protected UsuarioToken UsuarioToken()
        {
            var usuarioId = User.FindFirst("usuarioId")?.Value;
            var usuario = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return new UsuarioToken
            {
                UsuarioId = usuarioId != null ? int.Parse(usuarioId) : 0,
                Usuario = usuario
            };
        }

        protected IActionResult? RequirePermission(string permissionCode)
        {
            var usuario = UsuarioToken().Usuario;
            if (string.IsNullOrWhiteSpace(usuario))
            {
                return Unauthorized(new { message = "Usuario no autenticado." });
            }

            if (SecurityGestor.HasPermission(usuario, permissionCode))
            {
                return null;
            }

            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = "Permiso insuficiente.",
                permission = permissionCode
            });
        }
    }
}
