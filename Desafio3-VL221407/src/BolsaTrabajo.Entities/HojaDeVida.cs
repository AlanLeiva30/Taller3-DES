namespace BolsaTrabajo.Entities;

/// <summary>Currículum del candidato. Cada usuario tiene como máximo una hoja de vida.</summary>
public class HojaDeVida
{
    public int IdCV { get; set; }

    public string IdUsuario { get; set; } = string.Empty;
    public Usuario? Usuario { get; set; }

    public string FormacionAcademica { get; set; } = string.Empty;
    public string ExperienciaLaboral { get; set; } = string.Empty;
    public string Competencias { get; set; } = string.Empty;

    /// <summary>Nombre con el que se guardó el PDF en la carpeta de CV del servidor.</summary>
    public string? ArchivoCV { get; set; }

    /// <summary>Nombre original del archivo que subió el candidato (por ejemplo: "CV Juan Pérez.pdf").</summary>
    public string? NombreArchivoCV { get; set; }
}
