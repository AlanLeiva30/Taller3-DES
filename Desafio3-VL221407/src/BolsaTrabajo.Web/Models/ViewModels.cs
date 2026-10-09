using System.ComponentModel.DataAnnotations;
using BolsaTrabajo.DTOs.HojasDeVida;
using BolsaTrabajo.DTOs.Plazas;
using BolsaTrabajo.DTOs.Postulaciones;
using BolsaTrabajo.DTOs.Sistema;
using BolsaTrabajo.DTOs.Usuarios;
using BolsaTrabajo.Entities.Enums;

namespace BolsaTrabajo.Web.Models;

// ---------------- Público ----------------

public class InicioViewModel
{
    public List<PlazaDto> PlazasDestacadas { get; set; } = new();
    public int TotalPlazasAbiertas { get; set; }
    public int TotalInstituciones { get; set; }
    public bool ServicioDisponible { get; set; } = true;
}

public class PlazasPublicasViewModel
{
    public List<PlazaDto> Plazas { get; set; } = new();
    public List<string> Instituciones { get; set; } = new();
    public string? Buscar { get; set; }
    public string? Institucion { get; set; }
    public int TotalSinFiltro { get; set; }
    public bool HayFiltros => !string.IsNullOrWhiteSpace(Buscar) || !string.IsNullOrWhiteSpace(Institucion);
}

public class PlazaDetalleViewModel
{
    public PlazaDto Plaza { get; set; } = new();
    public PostulacionDto? MiPostulacion { get; set; }
    public bool EsDelAgenteActual { get; set; }

    /// <summary>Solo para decidir qué botón mostrar; la API vuelve a validar todo al postularse.</summary>
    public bool RecibePostulaciones =>
        Plaza.Publicada && Plaza.Estado == EstadoPlaza.Abierta && Plaza.FechaCierre.Date >= DateTime.Today;
}

// ---------------- Cuenta ----------------

public class LoginViewModel
{
    [Required(ErrorMessage = "Escriba su correo electrónico.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    [Display(Name = "Correo electrónico")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Escriba su contraseña.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Mantener la sesión iniciada")]
    public bool Recordarme { get; set; }
}

// ---------------- Candidato ----------------

public class CandidatoPanelViewModel
{
    public List<PostulacionDto> Postulaciones { get; set; } = new();
    public HojaDeVidaDto? HojaDeVida { get; set; }
    public List<PlazaDto> PlazasSugeridas { get; set; } = new();

    public int Total => Postulaciones.Count;
    public int EnRevision => Postulaciones.Count(p => p.Estado == EstadoPostulacion.EnRevision);
    public int Aprobadas => Postulaciones.Count(p => p.Estado == EstadoPostulacion.Aprobada);
    public int Rechazadas => Postulaciones.Count(p => p.Estado == EstadoPostulacion.Rechazada);
}

public class MiPerfilViewModel
{
    public UsuarioDto Usuario { get; set; } = new();
    public ActualizarPerfilDto Formulario { get; set; } = new();
    public HojaDeVidaDto? HojaDeVida { get; set; }
    public GuardarHojaDeVidaDto FormularioHoja { get; set; } = new();
}

// ---------------- Gestión del Agente ----------------

public class GestionPlazaViewModel
{
    public PlazaDto Plaza { get; set; } = new();
    public List<PostulacionDto> Postulaciones { get; set; } = new();
}

public class EditarPlazaViewModel
{
    public int IdPlaza { get; set; }
    public GuardarPlazaDto Datos { get; set; } = new();
}

public class HojaDeCandidatoViewModel
{
    public string IdCandidato { get; set; } = string.Empty;
    public string NombreCandidato { get; set; } = string.Empty;
    public string? EmailCandidato { get; set; }
    public HojaDeVidaDto? HojaDeVida { get; set; }
    public int? IdPlaza { get; set; }
    public string? TituloPlaza { get; set; }
}

// ---------------- Listados con filtros ----------------

public class ListadoPostulacionesViewModel
{
    public List<PostulacionDto> Todas { get; set; } = new();
    public List<PostulacionDto> Postulaciones { get; set; } = new();
    public EstadoPostulacion? EstadoFiltro { get; set; }
    public int? IdPlazaFiltro { get; set; }
    public List<PlazaDto> PlazasParaFiltro { get; set; } = new();

    public int Contar(EstadoPostulacion? estado) =>
        estado is null ? Todas.Count : Todas.Count(p => p.Estado == estado);
}

/// <summary>Filtros de plazas: activas, borradores, evaluacion, cerradas.</summary>
public static class FiltroPlaza
{
    public const string Activas = "activas";
    public const string Borradores = "borradores";
    public const string Evaluacion = "evaluacion";
    public const string Cerradas = "cerradas";

    public static readonly (string Clave, string Texto)[] Opciones =
    {
        (Activas, "Activas"), (Borradores, "Borradores"), (Evaluacion, "En evaluación"), (Cerradas, "Cerradas")
    };

    public static bool Cumple(PlazaDto plaza, string? filtro) => filtro switch
    {
        Activas => plaza.Publicada && plaza.Estado == EstadoPlaza.Abierta,
        Borradores => !plaza.Publicada,
        Evaluacion => plaza.Publicada && plaza.Estado == EstadoPlaza.EnEvaluacion,
        Cerradas => plaza.Publicada && plaza.Estado == EstadoPlaza.Cerrada,
        _ => true
    };
}

public class ListadoPlazasViewModel
{
    public List<PlazaDto> Todas { get; set; } = new();
    public List<PlazaDto> Plazas { get; set; } = new();
    public string? Filtro { get; set; }

    public int Contar(string? filtro) => Todas.Count(p => FiltroPlaza.Cumple(p, filtro));
}

// ---------------- Agente ----------------

public class AgentePanelViewModel
{
    public List<PlazaDto> Plazas { get; set; } = new();
    public List<PostulacionDto> Recibidas { get; set; } = new();

    public int Activas => Plazas.Count(p => FiltroPlaza.Cumple(p, FiltroPlaza.Activas));
    public int Borradores => Plazas.Count(p => FiltroPlaza.Cumple(p, FiltroPlaza.Borradores));
    public int EnEvaluacion => Plazas.Count(p => FiltroPlaza.Cumple(p, FiltroPlaza.Evaluacion));
    public int Cerradas => Plazas.Count(p => FiltroPlaza.Cumple(p, FiltroPlaza.Cerradas));
    public int PendientesDeRevisar => Recibidas.Count(p => p.Estado == EstadoPostulacion.EnRevision);
}

// ---------------- Administrador ----------------

public class AdminPanelViewModel
{
    public EstadoSistemaDto Estado { get; set; } = new();
    public List<ResumenProcesoDto> Procesos { get; set; } = new();
    public List<RolDto> Roles { get; set; } = new();
    public List<PostulacionDto> PostulacionesRecientes { get; set; } = new();

    public int ProcesosActivos => Procesos.Count(p => p.Publicada && p.Estado == EstadoPlaza.Abierta);
    public int ProcesosEnEvaluacion => Procesos.Count(p => p.Publicada && p.Estado == EstadoPlaza.EnEvaluacion);
    public int ProcesosCerrados => Procesos.Count(p => p.Publicada && p.Estado == EstadoPlaza.Cerrada);
    public int Borradores => Procesos.Count(p => !p.Publicada);
    public int PendientesDeRevisar => Procesos.Sum(p => p.EnRevision);
}

public class AdminUsuariosViewModel
{
    public List<UsuarioDto> Usuarios { get; set; } = new();
    public List<RolDto> Roles { get; set; } = new();
    public string? RolFiltro { get; set; }
}

// ---------------- Componentes ----------------

public record IndicadorViewModel(string Icono, string Valor, string Etiqueta, string Color, string? Detalle = null);

public record EstadoVacioViewModel(string Icono, string Titulo, string Mensaje, string? TextoAccion = null, string? UrlAccion = null);
