using BolsaTrabajo.Web.Servicios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace BolsaTrabajo.Web.Infraestructura;

/// <summary>
/// Convierte los problemas al comunicarse con la API en pantallas comprensibles para el usuario,
/// sin mostrar detalles técnicos.
/// </summary>
public class ManejadorErroresApiFilter : IAsyncExceptionFilter
{
    private readonly ITempDataDictionaryFactory _tempDataFactory;
    private readonly IModelMetadataProvider _metadataProvider;

    public ManejadorErroresApiFilter(ITempDataDictionaryFactory tempDataFactory, IModelMetadataProvider metadataProvider)
    {
        _tempDataFactory = tempDataFactory;
        _metadataProvider = metadataProvider;
    }

    public async Task OnExceptionAsync(ExceptionContext context)
    {
        var http = context.HttpContext;

        switch (context.Exception)
        {
            case SesionExpiradaException ex:
                await SesionUsuario.CerrarAsync(http);
                _tempDataFactory.GetTempData(http)["Info"] = ex.Message;
                context.Result = new RedirectToActionResult("Login", "Cuenta",
                    new { returnUrl = http.Request.Path + http.Request.QueryString });
                break;

            case ApiException { CodigoEstado: 401 }:
                context.Result = new RedirectToActionResult("Login", "Cuenta",
                    new { returnUrl = http.Request.Path + http.Request.QueryString });
                break;

            case ApiException { CodigoEstado: 403 }:
                context.Result = new RedirectToActionResult("AccesoDenegado", "Cuenta", null);
                break;

            case ApiException { CodigoEstado: 404 } ex:
                context.Result = Vista("NoEncontrado", ex.Message, StatusCodes.Status404NotFound);
                break;

            case ApiNoDisponibleException ex:
                context.Result = Vista("ServicioNoDisponible", ex.Message, StatusCodes.Status503ServiceUnavailable);
                break;

            case ApiException ex:
                context.Result = Vista("ErrorServicio", ex.Message, StatusCodes.Status500InternalServerError);
                break;

            default:
                return; // errores no relacionados con la API: los maneja ASP.NET Core
        }

        context.ExceptionHandled = true;
    }

    private ViewResult Vista(string nombre, string mensaje, int codigo) => new()
    {
        ViewName = nombre,
        StatusCode = codigo,
        ViewData = new ViewDataDictionary(_metadataProvider, new ModelStateDictionary())
        {
            ["Mensaje"] = mensaje
        }
    };
}
