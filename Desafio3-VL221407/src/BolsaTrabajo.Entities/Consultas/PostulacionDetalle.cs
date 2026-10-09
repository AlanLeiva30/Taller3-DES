using BolsaTrabajo.Entities.Enums;

namespace BolsaTrabajo.Entities.Consultas;

/// <summary>
/// Postulación con los datos de su plaza y de su candidato, obtenidos con un JOIN en una sola consulta.
/// Se llena con Dapper; no es una tabla.
/// </summary>
public class PostulacionDetalle
{
    public int IdPostulacion { get; set; }
    public int IdPlaza { get; set; }
    public string TituloPlaza { get; set; } = string.Empty;
    public string Institucion { get; set; } = string.Empty;
    public EstadoPlaza EstadoPlaza { get; set; }

    /// <summary>Agente dueño de la plaza (se usa para validar permisos).</summary>
    public string IdAgente { get; set; } = string.Empty;

    public string IdCandidato { get; set; } = string.Empty;
    public string NombreCandidato { get; set; } = string.Empty;
    public string EmailCandidato { get; set; } = string.Empty;
    public DateTime FechaPostulacion { get; set; }
    public EstadoPostulacion Estado { get; set; }
}
