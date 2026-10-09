namespace BolsaTrabajo.DTOs.HojasDeVida;

/// <summary>Hoja de vida de un candidato tal como la devuelve el sistema.</summary>
public class HojaDeVidaDto
{
    public int IdCV { get; set; }
    public string IdUsuario { get; set; } = string.Empty;
    public string NombreCandidato { get; set; } = string.Empty;
    public string? EmailCandidato { get; set; }
    public string FormacionAcademica { get; set; } = string.Empty;
    public string ExperienciaLaboral { get; set; } = string.Empty;
    public string Competencias { get; set; } = string.Empty;

    /// <summary>Indica si el candidato ya subió su CV en PDF.</summary>
    public bool TieneArchivoCV { get; set; }

    /// <summary>Nombre original del PDF que subió el candidato.</summary>
    public string? NombreArchivoCV { get; set; }
}
