using BolsaTrabajo.Entities;

namespace BolsaTrabajo.BLL.Seguridad;

/// <summary>
/// Quién está haciendo la petición. Lo crea la capa de presentación (API o MVC) a partir del token o la cookie,
/// y los servicios lo usan para aplicar las reglas de permisos.
/// </summary>
public sealed record UsuarioActual(string Id, string Rol)
{
    public bool EsAdministrador => Rol == Roles.Administrador;
    public bool EsAgente => Rol == Roles.AgenteSeleccion;
    public bool EsCandidato => Rol == Roles.Candidato;
}
