namespace BolsaTrabajo.DTOs.Usuarios;

/// <summary>Datos básicos de un usuario del sistema.</summary>
public class UsuarioDto
{
    public string Id { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string Rol { get; set; } = string.Empty;
}
