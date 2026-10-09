using BolsaTrabajo.API.Seguridad;
using BolsaTrabajo.BLL.Servicios;
using BolsaTrabajo.DTOs.Auth;
using BolsaTrabajo.DTOs.Usuarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BolsaTrabajo.API.Controllers;

/// <summary>Registro de candidatos e inicio de sesión (obtención del token JWT).</summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IUsuarioService _usuarios;
    private readonly TokenService _tokens;

    public AuthController(IUsuarioService usuarios, TokenService tokens)
    {
        _usuarios = usuarios;
        _tokens = tokens;
    }

    /// <summary>Crea una cuenta nueva con el rol Candidato y devuelve su token.</summary>
    [HttpPost("registro")]
    [AllowAnonymous]
    [ProducesResponseType<RespuestaLoginDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Registro(RegistroCandidatoDto dto)
    {
        var usuario = await _usuarios.RegistrarCandidatoAsync(dto);
        return CreatedAtAction(nameof(Perfil), null, _tokens.GenerarToken(usuario));
    }

    /// <summary>
    /// Inicia sesión y devuelve el token. Cópielo y péguelo en el botón "Authorize" de Swagger.
    /// Tras 5 intentos fallidos la cuenta se bloquea 5 minutos.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<RespuestaLoginDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginDto dto) =>
        Ok(_tokens.GenerarToken(await _usuarios.IniciarSesionAsync(dto.Email, dto.Password)));

    /// <summary>Datos del usuario que inició sesión.</summary>
    [HttpGet("perfil")]
    [Authorize]
    [ProducesResponseType<UsuarioDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Perfil() =>
        Ok(await _usuarios.ObtenerAsync(User.UsuarioActualRequerido().Id));
}
