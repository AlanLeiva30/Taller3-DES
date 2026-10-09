using BolsaTrabajo.Entities.Enums;

namespace BolsaTrabajo.Entities;

/// <summary>Puesto de trabajo ofertado por una institución pública.</summary>
public class Plaza
{
    public int IdPlaza { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public DateTime FechaPublicacion { get; set; }
    public DateTime FechaCierre { get; set; }
    public string Institucion { get; set; } = string.Empty;
    public EstadoPlaza Estado { get; set; } = EstadoPlaza.Abierta;

    /// <summary>Indica si la plaza ya es visible para los candidatos.</summary>
    public bool Publicada { get; set; }

    /// <summary>Agente de selección que creó la plaza.</summary>
    public string IdAgente { get; set; } = string.Empty;
    public Usuario? Agente { get; set; }

    public ICollection<Postulacion> Postulaciones { get; set; } = new List<Postulacion>();
}
