using BolsaTrabajo.DTOs.Usuarios;
using BolsaTrabajo.Entities;
using BolsaTrabajo.Entities.Enums;
using BolsaTrabajo.Web.Infraestructura;
using BolsaTrabajo.Web.Models;
using BolsaTrabajo.Web.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BolsaTrabajo.Web.Controllers;

/// <summary>Área del Administrador: resumen general, usuarios y roles, plazas y postulaciones.</summary>
[Authorize(Roles = Roles.Administrador)]
public class AdminController : Controller
{
    private readonly BolsaApiClient _api;

    public AdminController(BolsaApiClient api) => _api = api;

    public async Task<IActionResult> Index()
    {
        var postulaciones = await _api.ListarTodasLasPostulacionesAsync();
        return View(new AdminPanelViewModel
        {
            Estado = await _api.ObtenerEstadoAsync(),
            Procesos = await _api.ObtenerMonitoreoAsync(),
            Roles = await _api.ListarRolesAsync(),
            PostulacionesRecientes = postulaciones.Take(5).ToList()
        });
    }

    // ---------------- Usuarios y roles ----------------

    public async Task<IActionResult> Usuarios(string? rol) => View(new AdminUsuariosViewModel
    {
        Usuarios = await _api.ListarUsuariosAsync(rol),
        Roles = await _api.ListarRolesAsync(),
        RolFiltro = rol
    });

    [HttpPost]
    public async Task<IActionResult> CambiarRol(string id, string rol, string? filtro)
    {
        var resultado = await _api.CambiarRolAsync(id, rol);
        TempData.Notificar(resultado, resultado.Exito ? $"{resultado.Datos!.NombreCompleto} ahora tiene el rol «{rol}»." : string.Empty);
        return RedirectToAction(nameof(Usuarios), new { rol = filtro });
    }

    [HttpPost]
    public async Task<IActionResult> EliminarUsuario(string id, string? filtro)
    {
        TempData.Notificar(await _api.EliminarUsuarioAsync(id), "El usuario fue eliminado.");
        return RedirectToAction(nameof(Usuarios), new { rol = filtro });
    }

    [HttpGet]
    public IActionResult CrearUsuario() => View(new CrearUsuarioDto { Rol = Roles.AgenteSeleccion });

    [HttpPost]
    public async Task<IActionResult> CrearUsuario(CrearUsuarioDto modelo)
    {
        if (!ModelState.IsValid)
            return View(modelo);

        var resultado = await _api.CrearUsuarioAsync(modelo);
        if (!resultado.Exito)
        {
            ModelState.AgregarErroresApi(resultado);
            return View(modelo);
        }

        TempData["Exito"] = $"Se creó la cuenta de {resultado.Datos!.NombreCompleto} con el rol «{resultado.Datos.Rol}».";
        return RedirectToAction(nameof(Usuarios));
    }

    // ---------------- Plazas y postulaciones ----------------

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

    /// <summary>El administrador puede cerrar o cambiar el estado de cualquier proceso de selección.</summary>
    [HttpPost]
    public async Task<IActionResult> CambiarEstadoPlaza(int id, EstadoPlaza estado, string? filtro)
    {
        TempData.Notificar(await _api.CambiarEstadoPlazaAsync(id, estado),
            $"El estado del proceso cambió a «{Presentacion.Texto(estado)}».");
        return RedirectToAction(nameof(Plazas), new { estado = filtro });
    }

    public async Task<IActionResult> Postulaciones(int? plaza, EstadoPostulacion? estado)
    {
        var todas = await _api.ListarTodasLasPostulacionesAsync();
        var dePlaza = plaza is null ? todas : todas.Where(p => p.IdPlaza == plaza).ToList();

        return View(new ListadoPostulacionesViewModel
        {
            Todas = dePlaza,
            Postulaciones = estado is null ? dePlaza : dePlaza.Where(p => p.Estado == estado).ToList(),
            EstadoFiltro = estado,
            IdPlazaFiltro = plaza,
            PlazasParaFiltro = (await _api.ListarPlazasAsync()).OrderBy(p => p.Titulo).ToList()
        });
    }
}
