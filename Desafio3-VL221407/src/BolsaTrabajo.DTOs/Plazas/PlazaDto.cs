using BolsaTrabajo.Entities.Enums;

namespace BolsaTrabajo.DTOs.Plazas;

/// <summary>Datos de una plaza que se muestran al usuario.</summary>
public class PlazaDto
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
    public string? NombreAgente { get; set; }
    public int TotalPostulaciones { get; set; }
}
