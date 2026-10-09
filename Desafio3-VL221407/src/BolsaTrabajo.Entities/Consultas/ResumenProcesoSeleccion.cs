using BolsaTrabajo.Entities.Enums;

namespace BolsaTrabajo.Entities.Consultas;

/// <summary>Fila del reporte de monitoreo: una plaza con el conteo de sus postulaciones por estado.</summary>
public class ResumenProcesoSeleccion
{
    public int IdPlaza { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Institucion { get; set; } = string.Empty;
    public EstadoPlaza Estado { get; set; }
    public bool Publicada { get; set; }
    public DateTime FechaPublicacion { get; set; }
    public DateTime FechaCierre { get; set; }
    public string NombreAgente { get; set; } = string.Empty;
    public int TotalPostulaciones { get; set; }
    public int EnRevision { get; set; }
    public int Aprobadas { get; set; }
    public int Rechazadas { get; set; }
}
