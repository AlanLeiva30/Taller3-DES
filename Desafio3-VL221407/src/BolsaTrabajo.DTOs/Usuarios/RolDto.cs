namespace BolsaTrabajo.DTOs.Usuarios;

/// <summary>Rol del sistema y cuántos usuarios lo tienen asignado.</summary>
public class RolDto
{
    public string Nombre { get; set; } = string.Empty;
    public int TotalUsuarios { get; set; }
}
