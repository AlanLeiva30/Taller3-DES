using BolsaTrabajo.Entities.Enums;

namespace BolsaTrabajo.Entities.Consultas;

/// <summary>
/// Plaza con datos calculados en la misma consulta SQL (nombre del agente y total de postulaciones).
/// Se llena con Dapper; no es una tabla.
/// </summary>
public class PlazaDetalle
{
    public int IdPlaza { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public DateTime FechaPublicacion { get; set; }
    public DateTime FechaCierre { get; set; }
    public string Institucion { get; set; } = string.Empty;
    public EstadoPlaza Estado { get; set; }
    public bool Publicada { get; set; }
    public string IdAgente { get; set; } = string.Empty;
    public string NombreAgente { get; set; } = string.Empty;
    public int TotalPostulaciones { get; set; }
}
