using BolsaTrabajo.DTOs.Auth;
using BolsaTrabajo.Web.Infraestructura;
using BolsaTrabajo.Web.Models;
using BolsaTrabajo.Web.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BolsaTrabajo.Web.Controllers;

/// <summary>Inicio de sesión, registro de candidatos y cierre de sesión (todo validado por la API).</summary>
public class CuentaController : Controller
{
    private readonly BolsaApiClient _api;

    public CuentaController(BolsaApiClient api) => _api = api;

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Panel");

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginViewModel modelo, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid)
            return View(modelo);

        var resultado = await _api.IniciarSesionAsync(new LoginDto { Email = modelo.Email.Trim(), Password = modelo.Password });
        if (!resultado.Exito)
        {
            ModelState.AgregarErroresApi(resultado);
            return View(modelo);
        }

        await SesionUsuario.IniciarAsync(HttpContext, resultado.Datos!, modelo.Recordarme);
        TempData["Exito"] = $"¡Bienvenido(a), {resultado.Datos!.Usuario.NombreCompleto}!";

        return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : RedirectToAction("Index", "Panel");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Registro()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Panel");

        return View(new RegistroCandidatoDto());
    }

    /// <summary>Crea la cuenta de candidato en la API e inicia sesión automáticamente.</summary>
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Registro(RegistroCandidatoDto modelo)
    {
        if (!ModelState.IsValid)
            return View(modelo);

        modelo.Email = modelo.Email.Trim();
        var resultado = await _api.RegistrarseAsync(modelo);
        if (!resultado.Exito)
        {
            ModelState.AgregarErroresApi(resultado);
            return View(modelo);
        }

        await SesionUsuario.IniciarAsync(HttpContext, resultado.Datos!, recordar: false);
        TempData["Exito"] = "Su cuenta fue creada. Ya puede buscar plazas y postularse.";
        return RedirectToAction("Index", "Panel");
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await SesionUsuario.CerrarAsync(HttpContext);
        TempData["Info"] = "Cerró sesión correctamente.";
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccesoDenegado()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View();
    }
}
