using System.ComponentModel.DataAnnotations;
using BolsaTrabajo.API.Seguridad;
using BolsaTrabajo.BLL.Servicios;
using BolsaTrabajo.DTOs.HojasDeVida;
using BolsaTrabajo.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BolsaTrabajo.API.Controllers;

/// <summary>Hoja de vida del candidato y su CV en PDF.</summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class HojasDeVidaController : ControllerBase
{
    private const string AdminOAgente = $"{Roles.Administrador},{Roles.AgenteSeleccion}";

    private readonly IHojaDeVidaService _hojas;

    public HojasDeVidaController(IHojaDeVidaService hojas) => _hojas = hojas;

    /// <summary>Formulario para subir el CV (solo PDF, máximo 5 MB).</summary>
    public class SubirCvFormulario
    {
        /// <summary>Archivo PDF del currículum.</summary>
        [Required(ErrorMessage = "Seleccione un archivo PDF.")]
        public IFormFile Archivo { get; set; } = null!;
    }

    /// <summary>Administrador: todas las hojas de vida. Agente: las de candidatos que se postularon a sus plazas.</summary>
    [HttpGet]
    [Authorize(Roles = AdminOAgente)]
    [ProducesResponseType<List<HojaDeVidaDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar() => Ok(await _hojas.ListarAsync(User.UsuarioActualRequerido()));

    /// <summary>Hoja de vida del candidato que inició sesión.</summary>
    [HttpGet("mi-hoja")]
    [Authorize(Roles = Roles.Candidato)]
    [ProducesResponseType<HojaDeVidaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MiHoja() =>
        Ok(await _hojas.ObtenerMiaAsync(User.UsuarioActualRequerido().Id));

    /// <summary>Registra la hoja de vida (formación, experiencia y competencias) del candidato.</summary>
    [HttpPost("mi-hoja")]
    [Authorize(Roles = Roles.Candidato)]
    [ProducesResponseType<HojaDeVidaDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CrearMiHoja(GuardarHojaDeVidaDto dto)
    {
        var hoja = await _hojas.CrearAsync(User.ObtenerIdUsuario(), dto);
        return CreatedAtAction(nameof(MiHoja), null, hoja);
    }

    /// <summary>Actualiza la hoja de vida del candidato.</summary>
    [HttpPut("mi-hoja")]
    [Authorize(Roles = Roles.Candidato)]
    [ProducesResponseType<HojaDeVidaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActualizarMiHoja(GuardarHojaDeVidaDto dto) =>
        Ok(await _hojas.ActualizarAsync(User.UsuarioActualRequerido().Id, dto));

    /// <summary>Elimina la hoja de vida del candidato y su archivo PDF.</summary>
    [HttpDelete("mi-hoja")]
    [Authorize(Roles = Roles.Candidato)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EliminarMiHoja()
    {
        await _hojas.EliminarAsync(User.UsuarioActualRequerido().Id);
        return NoContent();
    }

    /// <summary>Sube (o reemplaza) el CV en PDF. Primero debe existir la hoja de vida.</summary>
    [HttpPost("mi-hoja/archivo")]
    [Authorize(Roles = Roles.Candidato)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [ProducesResponseType<HojaDeVidaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubirArchivo([FromForm] SubirCvFormulario formulario)
    {
        var archivo = formulario.Archivo;
        await using var contenido = archivo.OpenReadStream();
        return Ok(await _hojas.SubirArchivoAsync(
            User.UsuarioActualRequerido().Id, archivo.FileName, contenido, archivo.Length));
    }

    /// <summary>Descarga el CV en PDF del candidato que inició sesión.</summary>
    [HttpGet("mi-hoja/archivo")]
    [Authorize(Roles = Roles.Candidato)]
    [Produces("application/pdf", "application/json")]
    [ProducesResponseType<FileResult>(StatusCodes.Status200OK, "application/pdf")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DescargarMiArchivo()
    {
        var usuario = User.UsuarioActualRequerido();
        return PhysicalFile(await _hojas.ObtenerRutaArchivoAsync(usuario.Id, usuario), "application/pdf", "MiCV.pdf");
    }

    /// <summary>Hoja de vida de un candidato (el Agente solo si el candidato se postuló a una de sus plazas).</summary>
    [HttpGet("usuario/{idUsuario}")]
    [Authorize(Roles = AdminOAgente)]
    [ProducesResponseType<HojaDeVidaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PorUsuario(string idUsuario) =>
        Ok(await _hojas.ObtenerDeCandidatoAsync(idUsuario, User.UsuarioActualRequerido()));

    /// <summary>Descarga el CV en PDF de un candidato (mismas reglas de acceso).</summary>
    [HttpGet("usuario/{idUsuario}/archivo")]
    [Authorize(Roles = AdminOAgente)]
    [Produces("application/pdf", "application/json")]
    [ProducesResponseType<FileResult>(StatusCodes.Status200OK, "application/pdf")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DescargarArchivo(string idUsuario) =>
        PhysicalFile(await _hojas.ObtenerRutaArchivoAsync(idUsuario, User.UsuarioActualRequerido()),
                     "application/pdf", $"CV_{idUsuario}.pdf");
}
