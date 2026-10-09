namespace BolsaTrabajo.Entities.Consultas;

/// <summary>Rol del sistema con la cantidad de usuarios que lo tienen.</summary>
public class RolResumen
{
    public string Nombre { get; set; } = string.Empty;
    public int TotalUsuarios { get; set; }
}
