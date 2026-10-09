using BolsaTrabajo.API.Seguridad;
using BolsaTrabajo.BLL.Servicios;
using BolsaTrabajo.DTOs.Postulaciones;
using BolsaTrabajo.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BolsaTrabajo.API.Controllers;

/// <summary>Postulaciones: el Candidato se postula o retira, el Agente evalúa y el Administrador supervisa.</summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class PostulacionesController : ControllerBase
{
    private const string AdminOAgente = $"{Roles.Administrador},{Roles.AgenteSeleccion}";

    private readonly IPostulacionService _postulaciones;

    public PostulacionesController(IPostulacionService postulaciones) => _postulaciones = postulaciones;

    /// <summary>El candidato se postula a una plaza. No se permite postularse dos veces a la misma.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Candidato)]
    [ProducesResponseType<PostulacionDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Postular(CrearPostulacionDto dto)
    {
        var postulacion = await _postulaciones.PostularAsync(dto.IdPlaza, User.ObtenerIdUsuario());
        return CreatedAtAction(nameof(Obtener), new { id = postulacion.IdPostulacion }, postulacion);
    }

    /// <summary>Todas las postulaciones del sistema.</summary>
    [HttpGet]
    [Authorize(Roles = Roles.Administrador)]
    [ProducesResponseType<List<PostulacionDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar() => Ok(await _postulaciones.ListarTodasAsync());

    /// <summary>Detalle de una postulación (Admin: cualquiera; Agente: de sus plazas; Candidato: las suyas).</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<PostulacionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtener(int id) =>
        Ok(await _postulaciones.ObtenerAsync(id, User.UsuarioActualRequerido()));

    /// <summary>Postulaciones del candidato que inició sesión.</summary>
    [HttpGet("mis-postulaciones")]
    [Authorize(Roles = Roles.Candidato)]
    [ProducesResponseType<List<PostulacionDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> MisPostulaciones() =>
        Ok(await _postulaciones.ListarPorCandidatoAsync(User.UsuarioActualRequerido().Id));

    /// <summary>Todas las postulaciones recibidas en las plazas del agente que inició sesión.</summary>
    [HttpGet("recibidas")]
    [Authorize(Roles = Roles.AgenteSeleccion)]
    [ProducesResponseType<List<PostulacionDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Recibidas() =>
        Ok(await _postulaciones.ListarRecibidasAsync(User.UsuarioActualRequerido().Id));

    /// <summary>Postulaciones recibidas en una plaza (el Agente solo ve las de sus plazas).</summary>
    [HttpGet("plaza/{idPlaza:int}")]
    [Authorize(Roles = AdminOAgente)]
    [ProducesResponseType<List<PostulacionDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PorPlaza(int idPlaza) =>
        Ok(await _postulaciones.ListarPorPlazaAsync(idPlaza, User.UsuarioActualRequerido()));

    /// <summary>Evalúa una postulación: Aprobada o Rechazada.</summary>
    [HttpPut("{id:int}/evaluar")]
    [Authorize(Roles = Roles.AgenteSeleccion)]
    [ProducesResponseType<PostulacionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Evaluar(int id, EvaluarPostulacionDto dto) =>
        Ok(await _postulaciones.EvaluarAsync(id, dto.Estado, User.UsuarioActualRequerido().Id));

    /// <summary>El candidato retira su postulación (solo si sigue en revisión y la plaza está abierta).</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Candidato)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Retirar(int id)
    {
        await _postulaciones.RetirarAsync(id, User.UsuarioActualRequerido().Id);
        return NoContent();
    }
}
