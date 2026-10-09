using Microsoft.AspNetCore.Identity;

namespace BolsaTrabajo.Entities;

/// <summary>Persona que usa el sistema. Hereda usuario, correo y contraseña de ASP.NET Core Identity.</summary>
public class Usuario : IdentityUser
{
    public string NombreCompleto { get; set; } = string.Empty;

    /// <summary>Rol principal del usuario (Administrador, Agente de Selección o Candidato).</summary>
    public string Rol { get; set; } = Roles.Candidato;

    public ICollection<Plaza> PlazasCreadas { get; set; } = new List<Plaza>();
    public ICollection<Postulacion> Postulaciones { get; set; } = new List<Postulacion>();
    public HojaDeVida? HojaDeVida { get; set; }
}
