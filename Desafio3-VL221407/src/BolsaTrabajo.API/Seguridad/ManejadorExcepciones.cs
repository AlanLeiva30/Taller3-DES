using BolsaTrabajo.BLL.Excepciones;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace BolsaTrabajo.API.Seguridad;

/// <summary>Convierte las excepciones de negocio en respuestas HTTP con un mensaje claro en español.</summary>
public class ManejadorExcepciones : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var (status, titulo) = exception switch
        {
            ReglaNegocioException => (StatusCodes.Status400BadRequest, "Regla de negocio no cumplida"),
            NoEncontradoException => (StatusCodes.Status404NotFound, "No encontrado"),
            NoAutenticadoException => (StatusCodes.Status401Unauthorized, "No autenticado"),
            AccesoDenegadoException => (StatusCodes.Status403Forbidden, "Acceso denegado"),
            _ => (0, string.Empty)
        };

        if (status == 0)
            return false; // error inesperado: lo maneja ASP.NET Core como 500

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status,
            Title = titulo,
            Detail = exception.Message
        }, cancellationToken);
        return true;
    }
}
