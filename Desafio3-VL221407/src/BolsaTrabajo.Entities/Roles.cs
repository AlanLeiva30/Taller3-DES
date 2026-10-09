namespace BolsaTrabajo.Entities;

/// <summary>Nombres de los roles del sistema (se usan en [Authorize(Roles = ...)]).</summary>
public static class Roles
{
    public const string Administrador = "Administrador";
    public const string AgenteSeleccion = "Agente de Selección";
    public const string Candidato = "Candidato";

    public static readonly string[] Todos = { Administrador, AgenteSeleccion, Candidato };
}
