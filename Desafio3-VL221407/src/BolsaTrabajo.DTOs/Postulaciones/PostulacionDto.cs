using BolsaTrabajo.Entities.Enums;

namespace BolsaTrabajo.DTOs.Postulaciones;

/// <summary>Datos de una postulación que se muestran al usuario.</summary>
public class PostulacionDto
{
    public int IdPostulacion { get; set; }
    public int IdPlaza { get; set; }
    public string TituloPlaza { get; set; } = string.Empty;
    public string Institucion { get; set; } = string.Empty;
    public string IdCandidato { get; set; } = string.Empty;
    public string NombreCandidato { get; set; } = string.Empty;
    public string EmailCandidato { get; set; } = string.Empty;
    public DateTime FechaPostulacion { get; set; }
    public EstadoPostulacion Estado { get; set; }
}
