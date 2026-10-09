namespace BolsaTrabajo.Entities.Consultas;

/// <summary>Resumen general de la base de datos para comprobar que el sistema funciona.</summary>
public class EstadoSistema
{
    public List<string> Roles { get; set; } = new();
    public int TotalUsuarios { get; set; }
    public int TotalPlazas { get; set; }
    public int TotalPostulaciones { get; set; }
    public int TotalHojasDeVida { get; set; }
}
