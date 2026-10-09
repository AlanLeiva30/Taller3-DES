using BolsaTrabajo.Web.Servicios;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace BolsaTrabajo.Web.Infraestructura;

public static class MensajesResultado
{
    /// <summary>Muestra el mensaje de éxito, o el error que explicó la API (por ejemplo, una regla de negocio).</summary>
    public static bool Notificar<T>(this ITempDataDictionary tempData, ResultadoApi<T> resultado, string mensajeExito)
    {
        if (resultado.Exito)
        {
            tempData["Exito"] = mensajeExito;
            return true;
        }

        tempData["Error"] = resultado.Mensaje
            ?? string.Join(" ", resultado.ErroresCampos.SelectMany(e => e.Value))
            ?? "No fue posible completar la operación.";
        if (string.IsNullOrWhiteSpace(tempData["Error"] as string))
            tempData["Error"] = "No fue posible completar la operación.";
        return false;
    }
}
