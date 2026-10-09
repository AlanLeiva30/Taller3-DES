using BolsaTrabajo.API.Seguridad;
using BolsaTrabajo.BLL.Servicios;
using BolsaTrabajo.DTOs.Plazas;
using BolsaTrabajo.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BolsaTrabajo.API.Controllers;

/// <summary>Plazas de trabajo: consulta pública, gestión por el Agente y supervisión por el Administrador.</summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class PlazasController : ControllerBase
{
    private const string AdminOAgente = $"{Roles.Administrador},{Roles.AgenteSeleccion}";

    private readonly IPlazaService _plazas;

    public PlazasController(IPlazaService plazas) => _plazas = plazas;

    /// <summary>Plazas publicadas y abiertas a postulación.</summary>
    [HttpGet("disponibles")]
    [AllowAnonymous]
    [ProducesResponseType<List<PlazaDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Disponibles() => Ok(await _plazas.ListarDisponiblesAsync());

    /// <summary>Administrador: todas las plazas. Agente: solo las plazas que él creó.</summary>
    [HttpGet]
    [Authorize(Roles = AdminOAgente)]
    [ProducesResponseType<List<PlazaDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar() => Ok(await _plazas.ListarAsync(User.UsuarioActualRequerido()));

    /// <summary>Detalle de una plaza. Las no publicadas solo las ven el Administrador y el Agente que las creó.</summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType<PlazaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtener(int id) => Ok(await _plazas.ObtenerAsync(id, User.ObtenerUsuarioActual()));

    /// <summary>Crea una plaza. Queda como borrador (sin publicar) hasta usar "publicar".</summary>
    [HttpPost]
    [Authorize(Roles = Roles.AgenteSeleccion)]
    [ProducesResponseType<PlazaDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Crear(GuardarPlazaDto dto)
    {
        var plaza = await _plazas.CrearAsync(dto, User.ObtenerIdUsuario());
        return CreatedAtAction(nameof(Obtener), new { id = plaza.IdPlaza }, plaza);
    }

    /// <summary>Edita una plaza propia que no esté cerrada.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.AgenteSeleccion)]
    [ProducesResponseType<PlazaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Editar(int id, GuardarPlazaDto dto) =>
        Ok(await _plazas.ActualizarAsync(id, dto, User.UsuarioActualRequerido().Id));

    /// <summary>Elimina una plaza propia que aún no tenga postulaciones.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.AgenteSeleccion)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _plazas.EliminarAsync(id, User.UsuarioActualRequerido().Id);
        return NoContent();
    }

    /// <summary>Publica una plaza para que los candidatos puedan verla y postularse.</summary>
    [HttpPut("{id:int}/publicar")]
    [Authorize(Roles = Roles.AgenteSeleccion)]
    [ProducesResponseType<PlazaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Publicar(int id) =>
        Ok(await _plazas.PublicarAsync(id, User.UsuarioActualRequerido().Id));

    /// <summary>
    /// Cambia el estado del proceso: Abierta, EnEvaluacion o Cerrada.
    /// El Agente solo en sus plazas; el Administrador en cualquiera. Una plaza cerrada no se reabre.
    /// </summary>
    [HttpPut("{id:int}/estado")]
    [Authorize(Roles = AdminOAgente)]
    [ProducesResponseType<PlazaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CambiarEstado(int id, CambiarEstadoPlazaDto dto) =>
        Ok(await _plazas.CambiarEstadoAsync(id, dto.Estado, User.UsuarioActualRequerido()));

    /// <summary>Monitoreo de todos los procesos de selección con el conteo de postulaciones por estado.</summary>
    [HttpGet("monitoreo")]
    [Authorize(Roles = Roles.Administrador)]
    [ProducesResponseType<List<ResumenProcesoDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Monitoreo() => Ok(await _plazas.ObtenerMonitoreoAsync());
}
