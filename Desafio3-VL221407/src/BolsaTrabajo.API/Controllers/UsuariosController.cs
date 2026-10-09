using BolsaTrabajo.API.Seguridad;
using BolsaTrabajo.BLL.Servicios;
using BolsaTrabajo.DTOs.Usuarios;
using BolsaTrabajo.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BolsaTrabajo.API.Controllers;

/// <summary>Gestión de usuarios y roles (Administrador) y actualización del perfil propio (cualquier rol).</summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class UsuariosController : ControllerBase
{
    private readonly IUsuarioService _usuarios;

    public UsuariosController(IUsuarioService usuarios) => _usuarios = usuarios;

    /// <summary>Lista los usuarios. Opcionalmente filtra por rol (Administrador, Agente de Selección o Candidato).</summary>
    [HttpGet]
    [Authorize(Roles = Roles.Administrador)]
    [ProducesResponseType<List<UsuarioDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar([FromQuery] string? rol) => Ok(await _usuarios.ListarAsync(rol));

    /// <summary>Lista los roles del sistema con la cantidad de usuarios de cada uno.</summary>
    [HttpGet("roles")]
    [Authorize(Roles = Roles.Administrador)]
    [ProducesResponseType<List<RolDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarRoles() => Ok(await _usuarios.ListarRolesAsync());

    /// <summary>Obtiene un usuario por su Id.</summary>
    [HttpGet("{id}")]
    [Authorize(Roles = Roles.Administrador)]
    [ProducesResponseType<UsuarioDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtener(string id) => Ok(await _usuarios.ObtenerAsync(id));

    /// <summary>Crea un usuario con cualquier rol (por ejemplo, un Agente de Selección).</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Administrador)]
    [ProducesResponseType<UsuarioDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Crear(CrearUsuarioDto dto)
    {
        var usuario = await _usuarios.CrearAsync(dto);
        return CreatedAtAction(nameof(Obtener), new { id = usuario.Id }, usuario);
    }

    /// <summary>Actualiza el nombre y teléfono de cualquier usuario.</summary>
    [HttpPut("{id}")]
    [Authorize(Roles = Roles.Administrador)]
    [ProducesResponseType<UsuarioDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Actualizar(string id, ActualizarPerfilDto dto) =>
        Ok(await _usuarios.ActualizarDatosAsync(id, dto));

    /// <summary>Cambia el rol de un usuario.</summary>
    [HttpPut("{id}/rol")]
    [Authorize(Roles = Roles.Administrador)]
    [ProducesResponseType<UsuarioDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CambiarRol(string id, CambiarRolDto dto) =>
        Ok(await _usuarios.CambiarRolAsync(id, dto.Rol, User.UsuarioActualRequerido().Id));

    /// <summary>Elimina un usuario (no se permite si tiene plazas o postulaciones).</summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = Roles.Administrador)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Eliminar(string id)
    {
        await _usuarios.EliminarAsync(id, User.UsuarioActualRequerido().Id);
        return NoContent();
    }

    /// <summary>Actualiza el nombre y teléfono del usuario que inició sesión.</summary>
    [HttpPut("mi-perfil")]
    [ProducesResponseType<UsuarioDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ActualizarMiPerfil(ActualizarPerfilDto dto) =>
        Ok(await _usuarios.ActualizarDatosAsync(User.UsuarioActualRequerido().Id, dto));
}
