using BolsaTrabajo.DTOs.HojasDeVida;
using BolsaTrabajo.DTOs.Usuarios;
using BolsaTrabajo.Entities;
using BolsaTrabajo.Entities.Enums;
using BolsaTrabajo.Web.Infraestructura;
using BolsaTrabajo.Web.Models;
using BolsaTrabajo.Web.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BolsaTrabajo.Web.Controllers;

/// <summary>Área del candidato: panel, postulaciones, datos personales, hoja de vida y CV.</summary>
[Authorize(Roles = Roles.Candidato)]
public class CandidatoController : Controller
{
    private readonly BolsaApiClient _api;

    public CandidatoController(BolsaApiClient api) => _api = api;

    public async Task<IActionResult> Index()
    {
        var postulaciones = await _api.ListarMisPostulacionesAsync();
        var hoja = await _api.ObtenerMiHojaDeVidaAsync();
        var disponibles = await _api.ListarPlazasDisponiblesAsync();

        var yaPostulado = postulaciones.Select(p => p.IdPlaza).ToHashSet();
        return View(new CandidatoPanelViewModel
        {
            Postulaciones = postulaciones,
            HojaDeVida = hoja,
            PlazasSugeridas = disponibles.Where(p => !yaPostulado.Contains(p.IdPlaza)).OrderBy(p => p.FechaCierre).Take(3).ToList()
        });
    }

    public async Task<IActionResult> MisPostulaciones(EstadoPostulacion? estado)
    {
        var todas = await _api.ListarMisPostulacionesAsync();
        return View(new ListadoPostulacionesViewModel
        {
            Todas = todas,
            Postulaciones = estado is null ? todas : todas.Where(p => p.Estado == estado).ToList(),
            EstadoFiltro = estado
        });
    }

    // ---------------- Perfil ----------------

    [HttpGet]
    public async Task<IActionResult> MiPerfil() => View(await ConstruirPerfilAsync());

    /// <summary>Guarda nombre y teléfono.</summary>
    [HttpPost]
    public async Task<IActionResult> MiPerfil([Bind(Prefix = nameof(MiPerfilViewModel.Formulario))] ActualizarPerfilDto formulario)
    {
        if (ModelState.IsValid)
        {
            var resultado = await _api.ActualizarMiPerfilAsync(formulario);
            if (resultado.Exito)
            {
                await SesionUsuario.ActualizarNombreAsync(HttpContext, resultado.Datos!.NombreCompleto);
                TempData["Exito"] = "Sus datos personales se actualizaron correctamente.";
                return RedirectToAction(nameof(MiPerfil));
            }
            AgregarErrores(ModelState, nameof(MiPerfilViewModel.Formulario), resultado);
        }

        return View(nameof(MiPerfil), await ConstruirPerfilAsync(formulario: formulario));
    }

    /// <summary>Registra la hoja de vida la primera vez y la actualiza las siguientes.</summary>
    [HttpPost]
    public async Task<IActionResult> GuardarHojaDeVida([Bind(Prefix = nameof(MiPerfilViewModel.FormularioHoja))] GuardarHojaDeVidaDto formularioHoja)
    {
        if (ModelState.IsValid)
        {
            var existe = await _api.ObtenerMiHojaDeVidaAsync() is not null;
            var resultado = existe
                ? await _api.ActualizarMiHojaDeVidaAsync(formularioHoja)
                : await _api.CrearMiHojaDeVidaAsync(formularioHoja);

            if (resultado.Exito)
            {
                TempData["Exito"] = existe
                    ? "Su hoja de vida se actualizó correctamente."
                    : "Su hoja de vida quedó registrada. Ahora puede adjuntar su CV en PDF.";
                return RedirectToAction(nameof(MiPerfil), null, "hoja-de-vida");
            }
            AgregarErrores(ModelState, nameof(MiPerfilViewModel.FormularioHoja), resultado);
        }

        return View(nameof(MiPerfil), await ConstruirPerfilAsync(formularioHoja: formularioHoja));
    }

    /// <summary>Envía el PDF a la API, que comprueba que sea un PDF real y que no supere 5 MB.</summary>
    [HttpPost]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> SubirCv(IFormFile? archivo)
    {
        if (archivo is null || archivo.Length == 0)
        {
            TempData["Error"] = "Seleccione el archivo PDF de su CV antes de presionar «Subir CV».";
            return RedirectToAction(nameof(MiPerfil), null, "cv");
        }

        await using var contenido = archivo.OpenReadStream();
        TempData.Notificar(await _api.SubirMiCvAsync(contenido, archivo.FileName, archivo.ContentType),
            $"Su CV «{Path.GetFileName(archivo.FileName)}» se subió correctamente.");
        return RedirectToAction(nameof(MiPerfil), null, "cv");
    }

    public async Task<IActionResult> DescargarCv()
    {
        var hoja = await _api.ObtenerMiHojaDeVidaAsync();
        var contenido = await _api.DescargarMiCvAsync();
        if (hoja is null || contenido is null)
        {
            TempData["Error"] = "Todavía no ha subido su CV en PDF.";
            return RedirectToAction(nameof(MiPerfil));
        }

        return File(contenido, "application/pdf", hoja.NombreArchivoCV ?? "MiCV.pdf");
    }

    // ---------------- Auxiliares ----------------

    private async Task<MiPerfilViewModel> ConstruirPerfilAsync(
        ActualizarPerfilDto? formulario = null, GuardarHojaDeVidaDto? formularioHoja = null)
    {
        var usuario = await _api.ObtenerMiPerfilAsync();
        var hoja = await _api.ObtenerMiHojaDeVidaAsync();
        return new MiPerfilViewModel
        {
            Usuario = usuario,
            HojaDeVida = hoja,
            Formulario = formulario ?? new ActualizarPerfilDto { NombreCompleto = usuario.NombreCompleto, Telefono = usuario.Telefono },
            FormularioHoja = formularioHoja ?? new GuardarHojaDeVidaDto
            {
                FormacionAcademica = hoja?.FormacionAcademica ?? string.Empty,
                ExperienciaLaboral = hoja?.ExperienciaLaboral ?? string.Empty,
                Competencias = hoja?.Competencias ?? string.Empty
            }
        };
    }

    private static void AgregarErrores<T>(ModelStateDictionary modelState, string prefijo, ResultadoApi<T> resultado)
    {
        foreach (var (campo, mensajes) in resultado.ErroresCampos)
            foreach (var mensaje in mensajes)
                modelState.AddModelError($"{prefijo}.{campo}", mensaje);
        if (resultado.Mensaje is not null)
            modelState.AddModelError(string.Empty, resultado.Mensaje);
    }
}
