using AutoMapper;
using BolsaTrabajo.BLL.Archivos;
using BolsaTrabajo.BLL.Excepciones;
using BolsaTrabajo.BLL.Seguridad;
using BolsaTrabajo.DAL.Excepciones;
using BolsaTrabajo.DAL.Repositorios;
using BolsaTrabajo.DTOs.HojasDeVida;
using BolsaTrabajo.Entities;

namespace BolsaTrabajo.BLL.Servicios;

public interface IHojaDeVidaService
{
    Task<List<HojaDeVidaDto>> ListarAsync(UsuarioActual usuario);
    Task<HojaDeVidaDto> ObtenerMiaAsync(string idUsuario);
    Task<HojaDeVidaDto> ObtenerDeCandidatoAsync(string idCandidato, UsuarioActual usuario);
    Task<HojaDeVidaDto> CrearAsync(string? idUsuario, GuardarHojaDeVidaDto dto);
    Task<HojaDeVidaDto> ActualizarAsync(string idUsuario, GuardarHojaDeVidaDto dto);
    Task EliminarAsync(string idUsuario);
    Task<HojaDeVidaDto> SubirArchivoAsync(string idUsuario, string nombreArchivo, Stream contenido, long tamano);
    Task<string> ObtenerRutaArchivoAsync(string idCandidato, UsuarioActual usuario);
}

public class HojaDeVidaService : IHojaDeVidaService
{
    private static readonly byte[] FirmaPdf = "%PDF"u8.ToArray();

    private readonly IHojaDeVidaRepository _hojas;
    private readonly IPostulacionRepository _postulaciones;
    private readonly IAlmacenArchivosCV _almacen;
    private readonly ConfiguracionArchivos _configuracion;
    private readonly IMapper _mapper;

    public HojaDeVidaService(
        IHojaDeVidaRepository hojas,
        IPostulacionRepository postulaciones,
        IAlmacenArchivosCV almacen,
        ConfiguracionArchivos configuracion,
        IMapper mapper)
    {
        _hojas = hojas;
        _postulaciones = postulaciones;
        _almacen = almacen;
        _configuracion = configuracion;
        _mapper = mapper;
    }

    /// <summary>Administrador: todas. Agente: solo las de candidatos que se postularon a sus plazas.</summary>
    public async Task<List<HojaDeVidaDto>> ListarAsync(UsuarioActual usuario)
    {
        if (usuario.EsAdministrador)
            return _mapper.Map<List<HojaDeVidaDto>>(await _hojas.ListarAsync());
        if (usuario.EsAgente)
            return _mapper.Map<List<HojaDeVidaDto>>(await _hojas.ListarAsync(soloPostuladosConAgente: usuario.Id));

        throw new AccesoDenegadoException("Solo el Administrador y el Agente de Selección pueden ver este listado.");
    }

    public async Task<HojaDeVidaDto> ObtenerMiaAsync(string idUsuario) =>
        _mapper.Map<HojaDeVidaDto>(await _hojas.ObtenerPorUsuarioAsync(idUsuario)
            ?? throw new NoEncontradoException("Todavía no ha registrado su hoja de vida."));

    public async Task<HojaDeVidaDto> ObtenerDeCandidatoAsync(string idCandidato, UsuarioActual usuario)
    {
        await VerificarAccesoAsync(idCandidato, usuario);
        return _mapper.Map<HojaDeVidaDto>(await _hojas.ObtenerPorUsuarioAsync(idCandidato)
            ?? throw new NoEncontradoException("El candidato todavía no ha registrado su hoja de vida."));
    }

    public async Task<HojaDeVidaDto> CrearAsync(string? idUsuario, GuardarHojaDeVidaDto dto)
    {
        if (string.IsNullOrWhiteSpace(idUsuario))
            throw new NoAutenticadoException();
        ValidarTextos(dto);

        if (await _hojas.ObtenerPorUsuarioAsync(idUsuario) is not null)
            throw new ReglaNegocioException("Ya tiene una hoja de vida registrada. Use la opción de actualizar.");

        var hoja = _mapper.Map<HojaDeVida>(dto);
        hoja.IdUsuario = idUsuario;
        Limpiar(hoja);

        try
        {
            await _hojas.CrearAsync(hoja);
        }
        catch (RegistroDuplicadoException)
        {
            throw new ReglaNegocioException("Ya tiene una hoja de vida registrada. Use la opción de actualizar.");
        }

        return await ObtenerMiaAsync(idUsuario);
    }

    public async Task<HojaDeVidaDto> ActualizarAsync(string idUsuario, GuardarHojaDeVidaDto dto)
    {
        ValidarTextos(dto);
        var hoja = await _hojas.ObtenerPorUsuarioAsync(idUsuario)
            ?? throw new NoEncontradoException("Todavía no ha registrado su hoja de vida. Créela primero.");

        _mapper.Map(dto, hoja);
        Limpiar(hoja);
        await _hojas.ActualizarAsync(hoja);
        return await ObtenerMiaAsync(idUsuario);
    }

    /// <summary>Elimina la hoja de vida y su archivo PDF.</summary>
    public async Task EliminarAsync(string idUsuario)
    {
        var hoja = await _hojas.ObtenerPorUsuarioAsync(idUsuario)
            ?? throw new NoEncontradoException("No tiene una hoja de vida registrada.");

        await _hojas.EliminarAsync(idUsuario);
        _almacen.Eliminar(hoja.ArchivoCV);
    }

    public async Task<HojaDeVidaDto> SubirArchivoAsync(string idUsuario, string nombreArchivo, Stream contenido, long tamano)
    {
        var hoja = await _hojas.ObtenerPorUsuarioAsync(idUsuario)
            ?? throw new ReglaNegocioException(
                "Primero registre su hoja de vida (formación, experiencia y competencias) y luego suba el PDF.");

        if (tamano <= 0)
            throw new ReglaNegocioException("El archivo está vacío.");
        if (tamano > _configuracion.TamanoMaximoBytes)
            throw new ReglaNegocioException(
                $"El archivo supera el tamaño máximo de {_configuracion.TamanoMaximoBytes / (1024 * 1024)} MB.");
        if (!string.Equals(Path.GetExtension(nombreArchivo), ".pdf", StringComparison.OrdinalIgnoreCase))
            throw new ReglaNegocioException("Solo se permiten archivos PDF.");

        // Se copia a memoria (máximo 5 MB) para revisar el contenido antes de guardarlo.
        using var buffer = new MemoryStream();
        await contenido.CopyToAsync(buffer);
        if (!EsPdf(buffer.GetBuffer().AsSpan(0, (int)buffer.Length)))
            throw new ReglaNegocioException("El archivo no es un PDF válido.");

        buffer.Position = 0;
        var nombreGuardado = await _almacen.GuardarAsync(idUsuario, buffer);
        await _hojas.ActualizarArchivoAsync(idUsuario, nombreGuardado, NombreOriginalSeguro(nombreArchivo));

        // Se borra el PDF anterior para no acumular archivos huérfanos.
        _almacen.Eliminar(hoja.ArchivoCV);

        return await ObtenerMiaAsync(idUsuario);
    }

    public async Task<string> ObtenerRutaArchivoAsync(string idCandidato, UsuarioActual usuario)
    {
        await VerificarAccesoAsync(idCandidato, usuario);

        var hoja = await _hojas.ObtenerPorUsuarioAsync(idCandidato);
        return _almacen.ObtenerRuta(hoja?.ArchivoCV)
            ?? throw new NoEncontradoException("No se ha subido un CV en PDF.");
    }

    /// <summary>Solo el nombre del archivo (sin carpetas), con un máximo de 255 caracteres.</summary>
    private static string NombreOriginalSeguro(string nombreArchivo)
    {
        var nombre = Path.GetFileName(nombreArchivo.Replace('\\', '/').Split('/')[^1]).Trim();
        if (nombre.Length == 0)
            nombre = "CV.pdf";
        return nombre.Length <= 255 ? nombre : nombre[..251] + ".pdf";
    }

    /// <summary>Los PDF reales empiezan con los bytes "%PDF".</summary>
    public static bool EsPdf(ReadOnlySpan<byte> contenido) => contenido.StartsWith(FirmaPdf);

    /// <summary>
    /// El propio candidato y el Administrador pueden ver la hoja de vida.
    /// El Agente solo la de candidatos que se postularon a alguna de sus plazas.
    /// </summary>
    private async Task VerificarAccesoAsync(string idCandidato, UsuarioActual usuario)
    {
        if (usuario.Id == idCandidato || usuario.EsAdministrador)
            return;

        if (usuario.EsAgente && await _postulaciones.CandidatoSePostuloConAgenteAsync(idCandidato, usuario.Id))
            return;

        throw new AccesoDenegadoException("Solo puede ver la hoja de vida de candidatos que se postularon a sus plazas.");
    }

    private static void ValidarTextos(GuardarHojaDeVidaDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FormacionAcademica))
            throw new ReglaNegocioException("Indique su formación académica.");
    }

    private static void Limpiar(HojaDeVida hoja)
    {
        hoja.FormacionAcademica = hoja.FormacionAcademica.Trim();
        hoja.ExperienciaLaboral = hoja.ExperienciaLaboral?.Trim() ?? string.Empty;
        hoja.Competencias = hoja.Competencias?.Trim() ?? string.Empty;
    }
}
