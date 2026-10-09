using BolsaTrabajo.BLL.Servicios;
using BolsaTrabajo.DTOs.Sistema;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BolsaTrabajo.API.Controllers;

/// <summary>Comprobación rápida de que la API y la base de datos están funcionando.</summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[AllowAnonymous]
public class EstadoController : ControllerBase
{
    private readonly IEstadoService _estado;

    public EstadoController(IEstadoService estado) => _estado = estado;

    /// <summary>Roles registrados y totales de usuarios, plazas, postulaciones y hojas de vida.</summary>
    [HttpGet]
    [ProducesResponseType<EstadoSistemaDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get() => Ok(await _estado.ObtenerAsync());
}
