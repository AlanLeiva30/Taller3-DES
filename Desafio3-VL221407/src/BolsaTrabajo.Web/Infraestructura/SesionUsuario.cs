using System.Security.Claims;
using BolsaTrabajo.DTOs.Auth;
using BolsaTrabajo.Entities;
using BolsaTrabajo.Web.Servicios;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace BolsaTrabajo.Web.Infraestructura;

/// <summary>Crea, actualiza y cierra la sesión del usuario en el MVC.</summary>
public static class SesionUsuario
{
    /// <summary>
    /// Guarda en una cookie cifrada los datos del usuario y el token JWT que entregó la API.
    /// La cookie vence al mismo tiempo que el token.
    /// </summary>
    public static Task IniciarAsync(HttpContext contexto, RespuestaLoginDto login, bool recordar)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, login.Usuario.Id),
            new(ClaimTypes.Name, login.Usuario.NombreCompleto),
            new(ClaimTypes.Email, login.Usuario.Email),
            new(ClaimTypes.Role, login.Usuario.Rol)
        };

        var expira = login.Expira.Kind == DateTimeKind.Local ? login.Expira.ToUniversalTime() : login.Expira;
        var propiedades = new AuthenticationProperties
        {
            IsPersistent = recordar,
            ExpiresUtc = new DateTimeOffset(DateTime.SpecifyKind(expira, DateTimeKind.Utc)),
            AllowRefresh = false
        };
        propiedades.StoreTokens(new[] { new AuthenticationToken { Name = TokenApiHandler.NombreToken, Value = login.Token } });

        var identidad = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return contexto.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identidad), propiedades);
    }

    /// <summary>Actualiza el nombre mostrado sin pedir otra vez la contraseña (conserva el mismo token).</summary>
    public static async Task ActualizarNombreAsync(HttpContext contexto, string nuevoNombre)
    {
        var resultado = await contexto.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (!resultado.Succeeded || resultado.Principal.Identity is not ClaimsIdentity identidad)
            return;

        var anterior = identidad.FindFirst(ClaimTypes.Name);
        if (anterior is not null)
            identidad.RemoveClaim(anterior);
        identidad.AddClaim(new Claim(ClaimTypes.Name, nuevoNombre));

        await contexto.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, resultado.Principal, resultado.Properties);
    }

    public static Task CerrarAsync(HttpContext contexto) =>
        contexto.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

    // ---------- Lectura de datos del usuario conectado ----------

    public static string? IdUsuario(this ClaimsPrincipal usuario) => usuario.FindFirstValue(ClaimTypes.NameIdentifier);
    public static string NombreUsuario(this ClaimsPrincipal usuario) => usuario.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
    public static string Rol(this ClaimsPrincipal usuario) => usuario.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

    public static bool EsCandidato(this ClaimsPrincipal usuario) => usuario.IsInRole(Roles.Candidato);
    public static bool EsAgente(this ClaimsPrincipal usuario) => usuario.IsInRole(Roles.AgenteSeleccion);
    public static bool EsAdministrador(this ClaimsPrincipal usuario) => usuario.IsInRole(Roles.Administrador);

    /// <summary>Iniciales para el avatar (por ejemplo, "María Fernanda López" → "MF").</summary>
    public static string Iniciales(this ClaimsPrincipal usuario)
    {
        var partes = usuario.NombreUsuario().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return partes.Length switch
        {
            0 => "?",
            1 => partes[0][..1].ToUpperInvariant(),
            _ => string.Concat(partes[0][0], partes[1][0]).ToUpperInvariant()
        };
    }
}
