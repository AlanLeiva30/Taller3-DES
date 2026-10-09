using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using BolsaTrabajo.DTOs.Plazas;
using BolsaTrabajo.Entities;
using BolsaTrabajo.Entities.Enums;

namespace BolsaTrabajo.Web.Infraestructura;

/// <summary>Textos y colores que se muestran en pantalla para estados, fechas y roles.</summary>
public static class Presentacion
{
    private static readonly CultureInfo Cultura = new("es-SV");

    /// <summary>Texto en español definido con [Display] en el enum (por ejemplo, EnEvaluacion → "En evaluación").</summary>
    public static string Texto(Enum valor) =>
        valor.GetType().GetField(valor.ToString())?.GetCustomAttribute<DisplayAttribute>()?.GetName() ?? valor.ToString();

    // ---------- Plazas ----------

    /// <summary>Estado visible de una plaza: si no está publicada se muestra como "Borrador".</summary>
    public static string TextoEstadoPlaza(PlazaDto plaza) => plaza.Publicada ? Texto(plaza.Estado) : "Borrador";

    public static string ClaseEstadoPlaza(PlazaDto plaza) => !plaza.Publicada ? "estado-borrador" : ClaseEstadoPlaza(plaza.Estado);

    public static string TextoEstadoPlaza(EstadoPlaza estado, bool publicada) => publicada ? Texto(estado) : "Borrador";

    public static string ClaseEstadoPlaza(EstadoPlaza estado, bool publicada) => publicada ? ClaseEstadoPlaza(estado) : "estado-borrador";

    public static string ClaseEstadoPlaza(EstadoPlaza estado) => estado switch
    {
        EstadoPlaza.Abierta => "estado-abierta",
        EstadoPlaza.EnEvaluacion => "estado-evaluacion",
        _ => "estado-cerrada"
    };

    // ---------- Postulaciones ----------

    public static string ClaseEstadoPostulacion(EstadoPostulacion estado) => estado switch
    {
        EstadoPostulacion.Aprobada => "estado-aprobada",
        EstadoPostulacion.Rechazada => "estado-rechazada",
        _ => "estado-revision"
    };

    public static string IconoEstadoPostulacion(EstadoPostulacion estado) => estado switch
    {
        EstadoPostulacion.Aprobada => "bi-check-circle-fill",
        EstadoPostulacion.Rechazada => "bi-x-circle-fill",
        _ => "bi-hourglass-split"
    };

    // ---------- Fechas ----------

    public static string Fecha(DateTime fecha) => fecha.ToString("d 'de' MMMM 'de' yyyy", Cultura);
    public static string FechaCorta(DateTime fecha) => fecha.ToString("dd/MM/yyyy", Cultura);

    /// <summary>"Cierra hoy", "Cierra mañana", "Quedan 5 días" o "Cerrada".</summary>
    public static string TiempoRestante(DateTime fechaCierre)
    {
        var dias = (fechaCierre.Date - DateTime.Today).Days;
        return dias switch
        {
            < 0 => "Período finalizado",
            0 => "Cierra hoy",
            1 => "Cierra mañana",
            _ => $"Quedan {dias} días"
        };
    }

    public static string ClaseTiempoRestante(DateTime fechaCierre)
    {
        var dias = (fechaCierre.Date - DateTime.Today).Days;
        return dias < 0 ? "text-secondary" : dias <= 3 ? "text-danger" : dias <= 7 ? "text-warning-emphasis" : "text-success";
    }

    // ---------- Roles ----------

    public static string IconoRol(string rol) => rol switch
    {
        Roles.Administrador => "bi-shield-lock",
        Roles.AgenteSeleccion => "bi-person-badge",
        _ => "bi-person"
    };

    public static string ClaseRol(string rol) => rol switch
    {
        Roles.Administrador => "rol-admin",
        Roles.AgenteSeleccion => "rol-agente",
        _ => "rol-candidato"
    };

    /// <summary>Recorta textos largos para tarjetas y tablas.</summary>
    public static string Resumen(string? texto, int maximo = 140) =>
        string.IsNullOrWhiteSpace(texto) ? string.Empty
        : texto.Length <= maximo ? texto
        : texto[..maximo].TrimEnd() + "…";
}
