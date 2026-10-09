using AutoMapper;
using BolsaTrabajo.BLL.Archivos;
using BolsaTrabajo.BLL.Mapping;
using BolsaTrabajo.BLL.Seguridad;
using BolsaTrabajo.BLL.Servicios;
using BolsaTrabajo.Entities;
using Microsoft.Extensions.Logging.Abstractions;

namespace BolsaTrabajo.Tests.Fakes;

/// <summary>Arma los servicios reales de la BLL sobre la base de datos en memoria.</summary>
public class Escenario
{
    public DatosEnMemoria Db { get; } = new();
    public AlmacenArchivosEnMemoria Almacen { get; } = new();
    public IMapper Mapper { get; }

    public PlazaService Plazas { get; }
    public PostulacionService Postulaciones { get; }
    public HojaDeVidaService HojasDeVida { get; }

    public Usuario Agente { get; }
    public Usuario OtroAgente { get; }
    public Usuario Candidato { get; }

    public Escenario()
    {
        Mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>(), NullLoggerFactory.Instance).CreateMapper();

        var plazaRepo = new PlazaRepositoryEnMemoria(Db);
        var postulacionRepo = new PostulacionRepositoryEnMemoria(Db);
        Plazas = new PlazaService(plazaRepo, postulacionRepo, new ReporteRepositoryVacio(), Mapper);
        Postulaciones = new PostulacionService(postulacionRepo, plazaRepo, Mapper);
        HojasDeVida = new HojaDeVidaService(new HojaDeVidaRepositoryEnMemoria(Db), postulacionRepo, Almacen, new ConfiguracionArchivos(), Mapper);

        Agente = Db.AgregarUsuario("Agente Uno", Roles.AgenteSeleccion);
        OtroAgente = Db.AgregarUsuario("Agente Dos", Roles.AgenteSeleccion);
        Candidato = Db.AgregarUsuario("Candidato Uno", Roles.Candidato);
    }

    public UsuarioActual ComoAgente() => new(Agente.Id, Roles.AgenteSeleccion);
    public UsuarioActual ComoOtroAgente() => new(OtroAgente.Id, Roles.AgenteSeleccion);
    public UsuarioActual ComoCandidato() => new(Candidato.Id, Roles.Candidato);
    public static UsuarioActual ComoAdministrador() => new("admin-id", Roles.Administrador);
}
