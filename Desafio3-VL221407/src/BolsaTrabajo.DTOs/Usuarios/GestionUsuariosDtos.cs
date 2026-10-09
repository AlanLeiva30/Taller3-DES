using System.ComponentModel.DataAnnotations;

namespace BolsaTrabajo.DTOs.Usuarios;

/// <summary>Datos que usa el administrador para crear un usuario con cualquier rol.</summary>
public class CrearUsuarioDto
{
    [Required(ErrorMessage = "El nombre completo es obligatorio.")]
    [StringLength(150)]
    [Display(Name = "Nombre completo")]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    [Display(Name = "Correo electrónico")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Seleccione un rol.")]
    [Display(Name = "Rol")]
    public string Rol { get; set; } = string.Empty;
}

/// <summary>Nuevo rol que el administrador asigna a un usuario.</summary>
public class CambiarRolDto
{
    [Required(ErrorMessage = "Seleccione un rol.")]
    [Display(Name = "Rol")]
    public string Rol { get; set; } = string.Empty;
}

/// <summary>Datos personales que el propio usuario puede modificar.</summary>
public class ActualizarPerfilDto
{
    [Required(ErrorMessage = "El nombre completo es obligatorio.")]
    [StringLength(150)]
    [Display(Name = "Nombre completo")]
    public string NombreCompleto { get; set; } = string.Empty;

    [Phone(ErrorMessage = "El teléfono no tiene un formato válido.")]
    [Display(Name = "Teléfono")]
    public string? Telefono { get; set; }
}
