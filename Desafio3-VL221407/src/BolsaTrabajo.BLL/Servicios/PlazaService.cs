using AutoMapper;
using BolsaTrabajo.BLL.Excepciones;
using BolsaTrabajo.BLL.Seguridad;
using BolsaTrabajo.DAL.Excepciones;
using BolsaTrabajo.DAL.Repositorios;
using BolsaTrabajo.DTOs.Plazas;
using BolsaTrabajo.Entities;
using BolsaTrabajo.Entities.Enums;

namespace BolsaTrabajo.BLL.Servicios;

public interface IPlazaService
{
    Task<List<PlazaDto>> ListarDisponiblesAsync();
    Task<List<PlazaDto>> ListarAsync(UsuarioActual usuario);
    Task<PlazaDto> ObtenerAsync(int idPlaza, UsuarioActual? usuario);
    Task<PlazaDto> CrearAsync(GuardarPlazaDto dto, string? idAgente);
    Task<PlazaDto> ActualizarAsync(int idPlaza, GuardarPlazaDto dto, string idAgente);
    Task EliminarAsync(int idPlaza, string idAgente);
    Task<PlazaDto> PublicarAsync(int idPlaza, string idAgente);
    Task<PlazaDto> CambiarEstadoAsync(int idPlaza, EstadoPlaza estado, UsuarioActual usuario);
    Task<List<ResumenProcesoDto>> ObtenerMonitoreoAsync();
}

public class PlazaService : IPlazaService
{
    private readonly IPlazaRepository _plazas;
    private readonly IPostulacionRepository _postulaciones;
    private readonly IReporteRepository _reportes;
    private readonly IMapper _mapper;

    public PlazaService(IPlazaRepository plazas, IPostulacionRepository postulaciones, IReporteRepository reportes, IMapper mapper)
    {
        _plazas = plazas;
        _postulaciones = postulaciones;
        _reportes = reportes;
        _mapper = mapper;
    }

    /// <summary>
    /// Regla de fechas: la fecha de cierre debe ser posterior a la de publicación
    /// y no puede estar en el pasado.
    /// </summary>
    public static void ValidarFechas(DateTime fechaPublicacion, DateTime fechaCierre)
    {
        if (fechaCierre.Date <= fechaPublicacion.Date)
            throw new ReglaNegocioException("La fecha de cierre debe ser posterior a la fecha de publicación.");

        if (fechaCierre.Date < DateTime.Today)
            throw new ReglaNegocioException("La fecha de cierre no puede estar en el pasado.");
    }

    public async Task<List<PlazaDto>> ListarDisponiblesAsync() =>
        _mapper.Map<List<PlazaDto>>(await _plazas.ListarDisponiblesAsync(DateTime.Today));

    /// <summary>Administrador: todas las plazas. Agente: solo las que él creó.</summary>
    public async Task<List<PlazaDto>> ListarAsync(UsuarioActual usuario)
    {
        if (usuario.EsAdministrador)
            return _mapper.Map<List<PlazaDto>>(await _plazas.ListarTodasAsync());
        if (usuario.EsAgente)
            return _mapper.Map<List<PlazaDto>>(await _plazas.ListarPorAgenteAsync(usuario.Id));

        throw new AccesoDenegadoException("Solo el Administrador y el Agente de Selección pueden ver este listado.");
    }

    /// <summary>
    /// Las plazas publicadas las ve cualquiera. Las no publicadas (borradores) solo las ve
    /// el Administrador o el Agente que las creó.
    /// </summary>
    public async Task<PlazaDto> ObtenerAsync(int idPlaza, UsuarioActual? usuario)
    {
        var plaza = await _plazas.ObtenerDetalleAsync(idPlaza);
        var puedeVerla = plaza is not null && (plaza.Publicada
            || usuario?.EsAdministrador == true
            || (usuario?.EsAgente == true && plaza.IdAgente == usuario.Id));

        if (!puedeVerla)
            throw new NoEncontradoException($"No existe la plaza con código {idPlaza}.");

        return _mapper.Map<PlazaDto>(plaza);
    }

    public async Task<PlazaDto> CrearAsync(GuardarPlazaDto dto, string? idAgente)
    {
        if (string.IsNullOrWhiteSpace(idAgente))
            throw new NoAutenticadoException();

        ValidarTextos(dto);
        ValidarFechas(dto.FechaPublicacion, dto.FechaCierre);

        var plaza = _mapper.Map<Plaza>(dto);
        plaza.IdAgente = idAgente;
        plaza.Estado = EstadoPlaza.Abierta;
        plaza.Publicada = false; // queda como borrador hasta que el agente la publique

        var idPlaza = await _plazas.CrearAsync(plaza);
        return await ObtenerDetalleAsync(idPlaza);
    }

    public async Task<PlazaDto> ActualizarAsync(int idPlaza, GuardarPlazaDto dto, string idAgente)
    {
        var plaza = await BuscarPropiaAsync(idPlaza, idAgente);
        if (plaza.Estado == EstadoPlaza.Cerrada)
            throw new ReglaNegocioException("No se puede editar una plaza cerrada.");

        ValidarTextos(dto);
        ValidarFechas(dto.FechaPublicacion, dto.FechaCierre);

        _mapper.Map(dto, plaza);
        await _plazas.ActualizarAsync(plaza);
        return await ObtenerDetalleAsync(idPlaza);
    }

    public async Task EliminarAsync(int idPlaza, string idAgente)
    {
        await BuscarPropiaAsync(idPlaza, idAgente);

        var totalPostulaciones = await _postulaciones.ContarPorPlazaAsync(idPlaza);
        if (totalPostulaciones > 0)
            throw new ReglaNegocioException(
                $"No se puede eliminar la plaza porque tiene {totalPostulaciones} postulación(es). Puede cambiar su estado a Cerrada.");

        try
        {
            await _plazas.EliminarAsync(idPlaza);
        }
        catch (RegistroEnUsoException)
        {
            throw new ReglaNegocioException("No se puede eliminar la plaza porque tiene postulaciones registradas.");
        }
    }

    public async Task<PlazaDto> PublicarAsync(int idPlaza, string idAgente)
    {
        var plaza = await BuscarPropiaAsync(idPlaza, idAgente);
        if (plaza.Publicada)
            throw new ReglaNegocioException("La plaza ya está publicada.");
        if (plaza.Estado != EstadoPlaza.Abierta)
            throw new ReglaNegocioException("Solo se pueden publicar plazas en estado Abierta.");
        if (plaza.FechaCierre.Date < DateTime.Today)
            throw new ReglaNegocioException("No se puede publicar una plaza cuya fecha de cierre ya pasó.");

        plaza.Publicada = true;
        await _plazas.ActualizarAsync(plaza);
        return await ObtenerDetalleAsync(idPlaza);
    }

    /// <summary>
    /// Cambia el estado del proceso. El Agente solo sobre sus plazas; el Administrador sobre cualquiera.
    /// Una plaza cerrada no se puede reabrir.
    /// </summary>
    public async Task<PlazaDto> CambiarEstadoAsync(int idPlaza, EstadoPlaza estado, UsuarioActual usuario)
    {
        if (!Enum.IsDefined(estado))
            throw new ReglaNegocioException("El estado indicado no es válido. Use Abierta, EnEvaluacion o Cerrada.");

        var plaza = await BuscarAsync(idPlaza);
        if (!usuario.EsAdministrador && plaza.IdAgente != usuario.Id)
            throw new AccesoDenegadoException("Solo puede cambiar el estado de las plazas que usted creó.");

        if (plaza.Estado == estado)
            throw new ReglaNegocioException("La plaza ya se encuentra en ese estado.");
        if (plaza.Estado == EstadoPlaza.Cerrada)
            throw new ReglaNegocioException("Una plaza cerrada no puede volver a abrirse ni pasar a evaluación.");
        if (estado == EstadoPlaza.Abierta && plaza.FechaCierre.Date < DateTime.Today)
            throw new ReglaNegocioException("No se puede reabrir la plaza porque su fecha de cierre ya pasó. Edite la fecha primero.");

        plaza.Estado = estado;
        await _plazas.ActualizarAsync(plaza);
        return await ObtenerDetalleAsync(idPlaza);
    }

    public async Task<List<ResumenProcesoDto>> ObtenerMonitoreoAsync() =>
        _mapper.Map<List<ResumenProcesoDto>>(await _reportes.ObtenerResumenProcesosAsync());

    private async Task<PlazaDto> ObtenerDetalleAsync(int idPlaza) =>
        _mapper.Map<PlazaDto>(await _plazas.ObtenerDetalleAsync(idPlaza)
            ?? throw new NoEncontradoException($"No existe la plaza con código {idPlaza}."));

    private async Task<Plaza> BuscarAsync(int idPlaza) =>
        await _plazas.ObtenerPorIdAsync(idPlaza)
        ?? throw new NoEncontradoException($"No existe la plaza con código {idPlaza}.");

    /// <summary>Un agente solo puede modificar las plazas que él mismo creó.</summary>
    private async Task<Plaza> BuscarPropiaAsync(int idPlaza, string idAgente)
    {
        var plaza = await BuscarAsync(idPlaza);
        if (plaza.IdAgente != idAgente)
            throw new AccesoDenegadoException("Solo puede modificar las plazas que usted creó.");
        return plaza;
    }

    private static void ValidarTextos(GuardarPlazaDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Titulo))
            throw new ReglaNegocioException("El título es obligatorio.");
        if (string.IsNullOrWhiteSpace(dto.Descripcion))
            throw new ReglaNegocioException("La descripción es obligatoria.");
        if (string.IsNullOrWhiteSpace(dto.Institucion))
            throw new ReglaNegocioException("La institución es obligatoria.");
    }
}
