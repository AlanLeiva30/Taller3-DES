using BolsaTrabajo.DTOs.Plazas;
using BolsaTrabajo.Entities;
using BolsaTrabajo.Entities.Enums;
using BolsaTrabajo.Web.Infraestructura;
using BolsaTrabajo.Web.Models;
using BolsaTrabajo.Web.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BolsaTrabajo.Web.Controllers;

/// <summary>
/// Área del Agente de Selección: sus plazas, su gestión y la evaluación de postulaciones.
/// Las reglas (fechas, permisos, estados) las aplica la API; aquí solo se muestran sus respuestas.
/// </summary>
[Authorize(Roles = Roles.AgenteSeleccion)]
public class AgenteController : Controller
{
    private readonly BolsaApiClient _api;

    public AgenteController(BolsaApiClient api) => _api = api;

    public async Task<IActionResult> Index() => View(new AgentePanelViewModel
    {
        Plazas = await _api.ListarPlazasAsync(),
        Recibidas = await _api.ListarPostulacionesRecibidasAsync()
    });

    public async Task<IActionResult> Plazas(string? estado)
    {
        var todas = await _api.ListarPlazasAsync();
        return View(new ListadoPlazasViewModel
        {
            Todas = todas,
            Plazas = todas.Where(p => FiltroPlaza.Cumple(p, estado)).ToList(),
            Filtro = estado
        });
    }

    // ---------------- Crear y editar ----------------

    [HttpGet]
    public IActionResult CrearPlaza() => View(new GuardarPlazaDto());

    [HttpPost]
    public async Task<IActionResult> CrearPlaza(GuardarPlazaDto modelo)
    {
        if (!ModelState.IsValid)
            return View(modelo);

        var resultado = await _api.CrearPlazaAsync(modelo);
        if (!resultado.Exito)
        {
            ModelState.AgregarErroresApi(resultado);
            return View(modelo);
        }

        TempData["Exito"] = $"La plaza «{resultado.Datos!.Titulo}» se guardó como borrador. Publíquela cuando esté lista para recibir postulaciones.";
        return RedirectToAction(nameof(Plaza), new { id = resultado.Datos.IdPlaza });
    }

    [HttpGet]
    public async Task<IActionResult> EditarPlaza(int id)
    {
        var plaza = await ObtenerPlazaPropiaAsync(id);
        if (plaza is null)
            return PlazaNoEncontrada();

        return View(new EditarPlazaViewModel
        {
            IdPlaza = id,
            Datos = new GuardarPlazaDto
            {
                Titulo = plaza.Titulo,
                Descripcion = plaza.Descripcion,
                Institucion = plaza.Institucion,
                FechaPublicacion = plaza.FechaPublicacion,
                FechaCierre = plaza.FechaCierre
            }
        });
    }

    [HttpPost]
    public async Task<IActionResult> EditarPlaza(int id, [Bind(Prefix = nameof(EditarPlazaViewModel.Datos))] GuardarPlazaDto datos)
    {
        var modelo = new EditarPlazaViewModel { IdPlaza = id, Datos = datos };
        if (!ModelState.IsValid)
            return View(modelo);

        var resultado = await _api.EditarPlazaAsync(id, datos);
        if (!resultado.Exito)
        {
            foreach (var (campo, mensajes) in resultado.ErroresCampos)
                foreach (var mensaje in mensajes)
                    ModelState.AddModelError($"{nameof(EditarPlazaViewModel.Datos)}.{campo}", mensaje);
            if (resultado.Mensaje is not null)
                ModelState.AddModelError(string.Empty, resultado.Mensaje);
            return View(modelo);
        }

        TempData["Exito"] = "Los cambios de la plaza se guardaron correctamente.";
        return RedirectToAction(nameof(Plaza), new { id });
    }

    // ---------------- Gestión de una plaza ----------------

    /// <summary>Ficha de la plaza con todas sus acciones y los candidatos postulados.</summary>
    public async Task<IActionResult> Plaza(int id)
    {
        var plaza = await ObtenerPlazaPropiaAsync(id);
        if (plaza is null)
            return PlazaNoEncontrada();

        return View(new GestionPlazaViewModel
        {
            Plaza = plaza,
            Postulaciones = await _api.ListarPostulacionesDePlazaAsync(id)
        });
    }

    [HttpPost]
    public async Task<IActionResult> Publicar(int id)
    {
        TempData.Notificar(await _api.PublicarPlazaAsync(id),
            "La plaza fue publicada. Los candidatos ya pueden verla y postularse.");
        return RedirectToAction(nameof(Plaza), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> CambiarEstado(int id, EstadoPlaza estado)
    {
        TempData.Notificar(await _api.CambiarEstadoPlazaAsync(id, estado),
            $"El estado de la plaza cambió a «{Presentacion.Texto(estado)}».");
        return RedirectToAction(nameof(Plaza), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> EliminarPlaza(int id)
    {
        if (TempData.Notificar(await _api.EliminarPlazaAsync(id), "La plaza fue eliminada."))
            return RedirectToAction(nameof(Plazas));

        return RedirectToAction(nameof(Plaza), new { id });
    }

    // ---------------- Postulaciones ----------------

    public async Task<IActionResult> Postulaciones(int? plaza, EstadoPostulacion? estado)
    {
        var plazas = await _api.ListarPlazasAsync();
        var recibidas = await _api.ListarPostulacionesRecibidasAsync();
        var dePlaza = plaza is null ? recibidas : recibidas.Where(p => p.IdPlaza == plaza).ToList();

        return View(new ListadoPostulacionesViewModel
        {
            Todas = dePlaza,
            Postulaciones = estado is null ? dePlaza : dePlaza.Where(p => p.Estado == estado).ToList(),
            EstadoFiltro = estado,
            IdPlazaFiltro = plaza,
            PlazasParaFiltro = plazas.OrderBy(p => p.Titulo).ToList()
        });
    }

    /// <summary>Aprueba o rechaza una postulación y vuelve a la pantalla desde la que se evaluó.</summary>
    [HttpPost]
    public async Task<IActionResult> Evaluar(int id, EstadoPostulacion resultado, string? volverA)
    {
        var respuesta = await _api.EvaluarPostulacionAsync(id, resultado);
        TempData.Notificar(respuesta, respuesta.Exito
            ? $"La postulación de {respuesta.Datos!.NombreCandidato} quedó como «{Presentacion.Texto(resultado)}»."
            : string.Empty);

        return Url.IsLocalUrl(volverA) ? LocalRedirect(volverA) : RedirectToAction(nameof(Postulaciones));
    }

    /// <summary>Hoja de vida de un candidato que se postuló a una de sus plazas.</summary>
    public async Task<IActionResult> HojaDeVida(string id, int? plaza)
    {
        var postulaciones = await _api.ListarPostulacionesRecibidasAsync();
        var postulacion = postulaciones.FirstOrDefault(p => p.IdCandidato == id && (plaza is null || p.IdPlaza == plaza))
                          ?? postulaciones.FirstOrDefault(p => p.IdCandidato == id);
        if (postulacion is null)
            return RedirectToAction("AccesoDenegado", "Cuenta");

        return View(new HojaDeCandidatoViewModel
        {
            IdCandidato = id,
            NombreCandidato = postulacion.NombreCandidato,
            EmailCandidato = postulacion.EmailCandidato,
            HojaDeVida = await _api.ObtenerHojaDeVidaDeCandidatoAsync(id),
            IdPlaza = postulacion.IdPlaza,
            TituloPlaza = postulacion.TituloPlaza
        });
    }

    public async Task<IActionResult> DescargarCv(string id)
    {
        var hoja = await _api.ObtenerHojaDeVidaDeCandidatoAsync(id);
        var contenido = await _api.DescargarCvDeCandidatoAsync(id);
        if (hoja is null || contenido is null)
        {
            TempData["Error"] = "El candidato no ha subido su CV en PDF.";
            return RedirectToAction(nameof(HojaDeVida), new { id });
        }

        return File(contenido, "application/pdf", hoja.NombreArchivoCV ?? $"CV {hoja.NombreCandidato}.pdf");
    }

    // ---------------- Auxiliares ----------------

    /// <summary>La plaza solo se gestiona si pertenece al agente conectado.</summary>
    private async Task<PlazaDto?> ObtenerPlazaPropiaAsync(int id)
    {
        var plaza = await _api.ObtenerPlazaAsync(id);
        return plaza is not null && plaza.IdAgente == User.IdUsuario() ? plaza : null;
    }

    private ViewResult PlazaNoEncontrada()
    {
        Response.StatusCode = StatusCodes.Status404NotFound;
        ViewData["Mensaje"] = "La plaza no existe o no está a su cargo.";
        return View("NoEncontrado");
    }
}
