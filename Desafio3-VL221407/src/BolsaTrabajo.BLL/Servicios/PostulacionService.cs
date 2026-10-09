using AutoMapper;
using BolsaTrabajo.BLL.Excepciones;
using BolsaTrabajo.BLL.Seguridad;
using BolsaTrabajo.DAL.Excepciones;
using BolsaTrabajo.DAL.Repositorios;
using BolsaTrabajo.DTOs.Postulaciones;
using BolsaTrabajo.Entities;
using BolsaTrabajo.Entities.Consultas;
using BolsaTrabajo.Entities.Enums;

namespace BolsaTrabajo.BLL.Servicios;

public interface IPostulacionService
{
    Task<PostulacionDto> PostularAsync(int idPlaza, string? idCandidato);
    Task<PostulacionDto> ObtenerAsync(int idPostulacion, UsuarioActual usuario);
    Task<List<PostulacionDto>> ListarPorCandidatoAsync(string idCandidato);
    Task<List<PostulacionDto>> ListarPorPlazaAsync(int idPlaza, UsuarioActual usuario);
    Task<List<PostulacionDto>> ListarRecibidasAsync(string idAgente);
    Task<List<PostulacionDto>> ListarTodasAsync();
    Task<PostulacionDto> EvaluarAsync(int idPostulacion, EstadoPostulacion resultado, string idAgente);
    Task RetirarAsync(int idPostulacion, string idCandidato);
}

public class PostulacionService : IPostulacionService
{
    private readonly IPostulacionRepository _postulaciones;
    private readonly IPlazaRepository _plazas;
    private readonly IMapper _mapper;

    public PostulacionService(IPostulacionRepository postulaciones, IPlazaRepository plazas, IMapper mapper)
    {
        _postulaciones = postulaciones;
        _plazas = plazas;
        _mapper = mapper;
    }

    public async Task<PostulacionDto> PostularAsync(int idPlaza, string? idCandidato)
    {
        if (string.IsNullOrWhiteSpace(idCandidato))
            throw new NoAutenticadoException("Debe iniciar sesión para postularse a una plaza.");

        var plaza = await _plazas.ObtenerPorIdAsync(idPlaza)
            ?? throw new NoEncontradoException($"No existe la plaza con código {idPlaza}.");

        if (!plaza.Publicada)
            throw new ReglaNegocioException("La plaza todavía no ha sido publicada.");
        if (plaza.Estado != EstadoPlaza.Abierta)
            throw new ReglaNegocioException("La plaza ya no está recibiendo postulaciones.");
        if (DateTime.Today < plaza.FechaPublicacion.Date)
            throw new ReglaNegocioException("El período de postulación aún no ha comenzado.");
        if (DateTime.Today > plaza.FechaCierre.Date)
            throw new ReglaNegocioException("El período de postulación para esta plaza ya finalizó.");

        if (await _postulaciones.ExisteAsync(idPlaza, idCandidato))
            throw new ReglaNegocioException("Usted ya se postuló a esta plaza.");

        var postulacion = new Postulacion
        {
            IdPlaza = idPlaza,
            IdCandidato = idCandidato,
            FechaPostulacion = DateTime.Now,
            Estado = EstadoPostulacion.EnRevision
        };

        try
        {
            await _postulaciones.CrearAsync(postulacion);
        }
        catch (RegistroDuplicadoException)
        {
            throw new ReglaNegocioException("Usted ya se postuló a esta plaza.");
        }

        return _mapper.Map<PostulacionDto>(await BuscarAsync(postulacion.IdPostulacion));
    }

    /// <summary>Admin: cualquiera. Agente: las de sus plazas. Candidato: las suyas.</summary>
    public async Task<PostulacionDto> ObtenerAsync(int idPostulacion, UsuarioActual usuario)
    {
        var postulacion = await BuscarAsync(idPostulacion);

        var puedeVerla = usuario.EsAdministrador
            || (usuario.EsAgente && postulacion.IdAgente == usuario.Id)
            || (usuario.EsCandidato && postulacion.IdCandidato == usuario.Id);
        if (!puedeVerla)
            throw new AccesoDenegadoException("No tiene permiso para ver esta postulación.");

        return _mapper.Map<PostulacionDto>(postulacion);
    }

    public async Task<List<PostulacionDto>> ListarPorCandidatoAsync(string idCandidato) =>
        _mapper.Map<List<PostulacionDto>>(await _postulaciones.ListarPorCandidatoAsync(idCandidato));

    public async Task<List<PostulacionDto>> ListarPorPlazaAsync(int idPlaza, UsuarioActual usuario)
    {
        var plaza = await _plazas.ObtenerPorIdAsync(idPlaza)
            ?? throw new NoEncontradoException($"No existe la plaza con código {idPlaza}.");

        if (!usuario.EsAdministrador && plaza.IdAgente != usuario.Id)
            throw new AccesoDenegadoException("Solo puede ver las postulaciones de las plazas que usted creó.");

        return _mapper.Map<List<PostulacionDto>>(await _postulaciones.ListarPorPlazaAsync(idPlaza));
    }

    /// <summary>Postulaciones recibidas en todas las plazas del agente.</summary>
    public async Task<List<PostulacionDto>> ListarRecibidasAsync(string idAgente) =>
        _mapper.Map<List<PostulacionDto>>(await _postulaciones.ListarPorAgenteAsync(idAgente));

    public async Task<List<PostulacionDto>> ListarTodasAsync() =>
        _mapper.Map<List<PostulacionDto>>(await _postulaciones.ListarTodasAsync());

    public async Task<PostulacionDto> EvaluarAsync(int idPostulacion, EstadoPostulacion resultado, string idAgente)
    {
        if (resultado is not (EstadoPostulacion.Aprobada or EstadoPostulacion.Rechazada))
            throw new ReglaNegocioException("El resultado de la evaluación debe ser Aprobada o Rechazada.");

        var postulacion = await BuscarAsync(idPostulacion);

        if (postulacion.IdAgente != idAgente)
            throw new AccesoDenegadoException("Solo puede evaluar postulaciones de las plazas que usted creó.");
        if (postulacion.EstadoPlaza == EstadoPlaza.Cerrada)
            throw new ReglaNegocioException("No se pueden evaluar postulaciones de una plaza cerrada.");

        await _postulaciones.ActualizarEstadoAsync(idPostulacion, resultado);
        return _mapper.Map<PostulacionDto>(await BuscarAsync(idPostulacion));
    }

    /// <summary>El candidato retira su postulación mientras siga en revisión y la plaza esté abierta.</summary>
    public async Task RetirarAsync(int idPostulacion, string idCandidato)
    {
        var postulacion = await BuscarAsync(idPostulacion);

        if (postulacion.IdCandidato != idCandidato)
            throw new AccesoDenegadoException("Solo puede retirar sus propias postulaciones.");
        if (postulacion.Estado != EstadoPostulacion.EnRevision)
            throw new ReglaNegocioException("Solo puede retirar postulaciones que siguen en revisión.");
        if (postulacion.EstadoPlaza != EstadoPlaza.Abierta)
            throw new ReglaNegocioException("La plaza ya no está abierta; no es posible retirar la postulación.");

        await _postulaciones.EliminarAsync(idPostulacion);
    }

    private async Task<PostulacionDetalle> BuscarAsync(int idPostulacion) =>
        await _postulaciones.ObtenerDetalleAsync(idPostulacion)
        ?? throw new NoEncontradoException($"No existe la postulación con código {idPostulacion}.");
}
