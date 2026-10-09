using System.Security.Claims;
using BolsaTrabajo.BLL.Excepciones;
using BolsaTrabajo.BLL.Seguridad;

namespace BolsaTrabajo.API.Seguridad;

public static class ClaimsPrincipalExtensions
{
    /// <summary>Id del usuario que viene dentro del token, o null si no hay sesión.</summary>
    public static string? ObtenerIdUsuario(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier);

    /// <summary>Usuario del token (Id y rol), o null si la petición es anónima.</summary>
    public static UsuarioActual? ObtenerUsuarioActual(this ClaimsPrincipal user)
    {
        var id = user.ObtenerIdUsuario();
        return id is null ? null : new UsuarioActual(id, user.FindFirstValue(ClaimTypes.Role) ?? string.Empty);
    }

    /// <summary>Usuario del token; lanza 401 si no hay sesión.</summary>
    public static UsuarioActual UsuarioActualRequerido(this ClaimsPrincipal user) =>
        user.ObtenerUsuarioActual() ?? throw new NoAutenticadoException();
}
