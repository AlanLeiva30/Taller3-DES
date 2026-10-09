namespace BolsaTrabajo.DTOs.Sistema;

/// <summary>Resumen que confirma que la API y la base de datos funcionan.</summary>
public class EstadoSistemaDto
{
    public string Mensaje { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public int TotalUsuarios { get; set; }
    public int TotalPlazas { get; set; }
    public int TotalPostulaciones { get; set; }
    public int TotalHojasDeVida { get; set; }
}
