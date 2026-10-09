using BolsaTrabajo.Entities.Enums;

namespace BolsaTrabajo.Entities;

/// <summary>Solicitud de un candidato para optar a una plaza.</summary>
public class Postulacion
{
    public int IdPostulacion { get; set; }

    public int IdPlaza { get; set; }
    public Plaza? Plaza { get; set; }

    public string IdCandidato { get; set; } = string.Empty;
    public Usuario? Candidato { get; set; }

    public DateTime FechaPostulacion { get; set; } = DateTime.Now;
    public EstadoPostulacion Estado { get; set; } = EstadoPostulacion.EnRevision;
}
