using System.Globalization;
using BolsaTrabajo.Entities;
using BolsaTrabajo.Web.Infraestructura;
using BolsaTrabajo.Web.Models;
using BolsaTrabajo.Web.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BolsaTrabajo.Web.Controllers;

/// <summary>Plazas publicadas: consulta pública, detalle y postulación del candidato.</summary>
public class PlazasController : Controller
{
    private static readonly CompareInfo Comparador = new CultureInfo("es-SV").CompareInfo;

    private readonly BolsaApiClient _api;

    public PlazasController(BolsaApiClient api) => _api = api;

    /// <summary>Plazas abiertas a postulación, con búsqueda por texto e institución.</summary>
    [AllowAnonymous]
    public async Task<IActionResult> Index(string? buscar, string? institucion)
    {
        var plazas = await _api.ListarPlazasDisponiblesAsync();

        var filtradas = plazas.Where(p =>
                (string.IsNullOrWhiteSpace(buscar) || Contiene(p.Titulo, buscar) || Contiene(p.Descripcion, buscar) || Contiene(p.Institucion, buscar))
                && (string.IsNullOrWhiteSpace(institucion) || string.Equals(p.Institucion, institucion, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        return View(new PlazasPublicasViewModel
        {
            Plazas = filtradas,
            TotalSinFiltro = plazas.Count,
            Instituciones = plazas.Select(p => p.Institucion).Distinct(StringComparer.OrdinalIgnoreCase).Order().ToList(),
            Buscar = buscar,
            Institucion = institucion
        });
    }

    [AllowAnonymous]
    public async Task<IActionResult> Detalle(int id)
    {
        var plaza = await _api.ObtenerPlazaAsync(id);
        if (plaza is null)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            ViewData["Mensaje"] = "La plaza que busca no existe o ya no está disponible.";
            return View("NoEncontrado");
        }

        var modelo = new PlazaDetalleViewModel
        {
            Plaza = plaza,
            EsDelAgenteActual = User.EsAgente() && plaza.IdAgente == User.IdUsuario()
        };

        if (User.EsCandidato())
            modelo.MiPostulacion = (await _api.ListarMisPostulacionesAsync()).FirstOrDefault(p => p.IdPlaza == id);

        return View(modelo);
    }

    /// <summary>El candidato se postula. La API valida que la plaza esté abierta y que no se postule dos veces.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Candidato)]
    public async Task<IActionResult> Postular(int id)
    {
        var resultado = await _api.PostularAsync(id);
        if (resultado.Exito)
            TempData["Exito"] = $"¡Listo! Su postulación a «{resultado.Datos!.TituloPlaza}» fue enviada y está en revisión.";
        else
            TempData["Error"] = resultado.Mensaje ?? "No fue posible enviar la postulación.";

        return RedirectToAction(nameof(Detalle), new { id });
    }

    /// <summary>Búsqueda sin distinguir mayúsculas ni tildes ("educacion" encuentra "Educación").</summary>
    private static bool Contiene(string texto, string busqueda) =>
        Comparador.IndexOf(texto, busqueda.Trim(), CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0;
}
