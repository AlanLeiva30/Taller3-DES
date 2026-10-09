using System.Diagnostics;
using BolsaTrabajo.Web.Models;
using BolsaTrabajo.Web.Servicios;
using Microsoft.AspNetCore.Mvc;

namespace BolsaTrabajo.Web.Controllers;

public class HomeController : Controller
{
    private readonly BolsaApiClient _api;

    public HomeController(BolsaApiClient api) => _api = api;

    /// <summary>Portada pública. Si el usuario ya inició sesión, va directo a su panel.</summary>
    public async Task<IActionResult> Index()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Panel");

        var modelo = new InicioViewModel();
        try
        {
            var plazas = await _api.ListarPlazasDisponiblesAsync();
            modelo.TotalPlazasAbiertas = plazas.Count;
            modelo.TotalInstituciones = plazas.Select(p => p.Institucion).Distinct(StringComparer.OrdinalIgnoreCase).Count();
            modelo.PlazasDestacadas = plazas.OrderBy(p => p.FechaCierre).Take(3).ToList();
        }
        catch (ApiNoDisponibleException)
        {
            // La portada se muestra igual, con un aviso en lugar de las plazas.
            modelo.ServicioDisponible = false;
        }

        return View(modelo);
    }

    /// <summary>Páginas de error para direcciones inexistentes (404) u otros códigos.</summary>
    [Route("Home/Estado/{codigo:int}")]
    public IActionResult Estado(int codigo)
    {
        if (codigo == StatusCodes.Status404NotFound)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            ViewData["Mensaje"] = "La página que busca no existe o fue movida.";
            return View("NoEncontrado");
        }

        return RedirectToAction(nameof(Error));
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
