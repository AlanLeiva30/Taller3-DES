using BolsaTrabajo.Web.Servicios;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BolsaTrabajo.Web.Infraestructura;

public static class ModelStateExtensions
{
    /// <summary>
    /// Copia los errores que devolvió la API al formulario: los de cada campo junto al campo,
    /// y el mensaje general en el resumen superior.
    /// </summary>
    public static void AgregarErroresApi<T>(this ModelStateDictionary modelState, ResultadoApi<T> resultado)
    {
        foreach (var (campo, mensajes) in resultado.ErroresCampos)
        {
            var clave = NormalizarCampo(campo);
            foreach (var mensaje in mensajes)
                modelState.AddModelError(clave, mensaje);
        }

        if (!string.IsNullOrWhiteSpace(resultado.Mensaje))
            modelState.AddModelError(string.Empty, resultado.Mensaje);
    }

    /// <summary>"$.fechaCierre" o "fechaCierre" → "FechaCierre" (nombre de la propiedad en el formulario).</summary>
    private static string NormalizarCampo(string campo)
    {
        var limpio = campo.StartsWith("$.") ? campo[2..] : campo;
        return limpio.Length == 0 ? string.Empty : char.ToUpperInvariant(limpio[0]) + limpio[1..];
    }
}
