using BolsaTrabajo.BLL.Archivos;
using BolsaTrabajo.DAL.Excepciones;
using BolsaTrabajo.DAL.Repositorios;
using BolsaTrabajo.Entities;
using BolsaTrabajo.Entities.Consultas;
using BolsaTrabajo.Entities.Enums;

namespace BolsaTrabajo.Tests.Fakes;

/// <summary>
/// "Base de datos" en memoria compartida por los repositorios falsos.
/// Igual que SQL Server: entrega copias de los registros (los cambios solo se guardan si el servicio
/// llama a Actualizar) y rechaza duplicados como lo hace el índice único de Postulaciones.
/// </summary>
public class DatosEnMemoria
{
    public List<Usuario> Usuarios { get; } = new();
    public List<Plaza> Plazas { get; } = new();
    public List<Postulacion> Postulaciones { get; } = new();
    public List<HojaDeVida> HojasDeVida { get; } = new();

    private int _siguientePlaza = 1, _siguientePostulacion = 1, _siguienteHoja = 1;
    public int NuevoIdPlaza() => _siguientePlaza++;
    public int NuevoIdPostulacion() => _siguientePostulacion++;
    public int NuevoIdHoja() => _siguienteHoja++;

    public Usuario AgregarUsuario(string nombre, string rol)
    {
        var usuario = new Usuario { NombreCompleto = nombre, Email = $"{Guid.NewGuid():N}@prueba.com", Rol = rol };
        Usuarios.Add(usuario);
        return usuario;
    }

    public Plaza AgregarPlaza(string idAgente, bool publicada = true, EstadoPlaza estado = EstadoPlaza.Abierta,
        int diasDesdePublicacion = -2, int diasHastaCierre = 15)
    {
        var plaza = new Plaza
        {
            IdPlaza = NuevoIdPlaza(),
            Titulo = "Plaza de prueba",
            Descripcion = "Descripción",
            Institucion = "Institución",
            FechaPublicacion = DateTime.Today.AddDays(diasDesdePublicacion),
            FechaCierre = DateTime.Today.AddDays(diasHastaCierre),
            Estado = estado,
            Publicada = publicada,
            IdAgente = idAgente
        };
        Plazas.Add(plaza);
        return plaza;
    }

    public static Plaza Copia(Plaza p) => new()
    {
        IdPlaza = p.IdPlaza, Titulo = p.Titulo, Descripcion = p.Descripcion, Institucion = p.Institucion,
        FechaPublicacion = p.FechaPublicacion, FechaCierre = p.FechaCierre, Estado = p.Estado,
        Publicada = p.Publicada, IdAgente = p.IdAgente
    };

    public static HojaDeVida Copia(HojaDeVida h) => new()
    {
        IdCV = h.IdCV, IdUsuario = h.IdUsuario, FormacionAcademica = h.FormacionAcademica,
        ExperienciaLaboral = h.ExperienciaLaboral, Competencias = h.Competencias,
        ArchivoCV = h.ArchivoCV, NombreArchivoCV = h.NombreArchivoCV, Usuario = h.Usuario
    };
}

public class PlazaRepositoryEnMemoria : IPlazaRepository
{
    private readonly DatosEnMemoria _db;
    public PlazaRepositoryEnMemoria(DatosEnMemoria db) => _db = db;

    private PlazaDetalle Detalle(Plaza p) => new()
    {
        IdPlaza = p.IdPlaza, Titulo = p.Titulo, Descripcion = p.Descripcion, Institucion = p.Institucion,
        FechaPublicacion = p.FechaPublicacion, FechaCierre = p.FechaCierre, Estado = p.Estado, Publicada = p.Publicada,
        IdAgente = p.IdAgente,
        NombreAgente = _db.Usuarios.FirstOrDefault(u => u.Id == p.IdAgente)?.NombreCompleto ?? string.Empty,
        TotalPostulaciones = _db.Postulaciones.Count(x => x.IdPlaza == p.IdPlaza)
    };

    public Task<Plaza?> ObtenerPorIdAsync(int idPlaza) =>
        Task.FromResult(_db.Plazas.Where(p => p.IdPlaza == idPlaza).Select(DatosEnMemoria.Copia).FirstOrDefault());
    public Task<PlazaDetalle?> ObtenerDetalleAsync(int idPlaza) =>
        Task.FromResult(_db.Plazas.Where(p => p.IdPlaza == idPlaza).Select(Detalle).FirstOrDefault());
    public Task<IEnumerable<PlazaDetalle>> ListarTodasAsync() => Task.FromResult(_db.Plazas.Select(Detalle).ToList().AsEnumerable());
    public Task<IEnumerable<PlazaDetalle>> ListarPorAgenteAsync(string idAgente) =>
        Task.FromResult(_db.Plazas.Where(p => p.IdAgente == idAgente).Select(Detalle).ToList().AsEnumerable());
    public Task<IEnumerable<PlazaDetalle>> ListarDisponiblesAsync(DateTime hoy) =>
        Task.FromResult(_db.Plazas.Where(p => p.Publicada && p.Estado == EstadoPlaza.Abierta
            && p.FechaPublicacion.Date <= hoy.Date && p.FechaCierre.Date >= hoy.Date).Select(Detalle).ToList().AsEnumerable());
    public Task<int> ContarAsync() => Task.FromResult(_db.Plazas.Count);
    public Task<int> ContarPorAgenteAsync(string idAgente) => Task.FromResult(_db.Plazas.Count(p => p.IdAgente == idAgente));

    public Task<int> CrearAsync(Plaza plaza)
    {
        // Misma restricción CHECK que la base de datos real.
        if (plaza.FechaCierre <= plaza.FechaPublicacion)
            throw new InvalidOperationException("CHECK CK_Plaza_Fechas violado");
        plaza.IdPlaza = _db.NuevoIdPlaza();
        _db.Plazas.Add(DatosEnMemoria.Copia(plaza));
        return Task.FromResult(plaza.IdPlaza);
    }

    public Task<bool> ActualizarAsync(Plaza plaza)
    {
        var i = _db.Plazas.FindIndex(p => p.IdPlaza == plaza.IdPlaza);
        if (i < 0) return Task.FromResult(false);
        _db.Plazas[i] = DatosEnMemoria.Copia(plaza);
        return Task.FromResult(true);
    }

    public Task<bool> EliminarAsync(int idPlaza)
    {
        // Igual que la llave foránea RESTRICT: no se borra una plaza con postulaciones.
        if (_db.Postulaciones.Any(p => p.IdPlaza == idPlaza))
            throw new RegistroEnUsoException("La plaza tiene postulaciones.", new Exception());
        return Task.FromResult(_db.Plazas.RemoveAll(p => p.IdPlaza == idPlaza) > 0);
    }
}

public class PostulacionRepositoryEnMemoria : IPostulacionRepository
{
    private readonly DatosEnMemoria _db;
    public PostulacionRepositoryEnMemoria(DatosEnMemoria db) => _db = db;

    private PostulacionDetalle Detalle(Postulacion po)
    {
        var plaza = _db.Plazas.First(p => p.IdPlaza == po.IdPlaza);
        var candidato = _db.Usuarios.FirstOrDefault(u => u.Id == po.IdCandidato);
        return new PostulacionDetalle
        {
            IdPostulacion = po.IdPostulacion, IdPlaza = po.IdPlaza, TituloPlaza = plaza.Titulo, Institucion = plaza.Institucion,
            EstadoPlaza = plaza.Estado, IdAgente = plaza.IdAgente, IdCandidato = po.IdCandidato,
            NombreCandidato = candidato?.NombreCompleto ?? string.Empty, EmailCandidato = candidato?.Email ?? string.Empty,
            FechaPostulacion = po.FechaPostulacion, Estado = po.Estado
        };
    }

    private Task<IEnumerable<PostulacionDetalle>> Lista(Func<Postulacion, bool> filtro) =>
        Task.FromResult(_db.Postulaciones.Where(filtro).Select(Detalle).ToList().AsEnumerable());

    public Task<PostulacionDetalle?> ObtenerDetalleAsync(int id) =>
        Task.FromResult(_db.Postulaciones.Where(p => p.IdPostulacion == id).Select(Detalle).FirstOrDefault());
    public Task<bool> ExisteAsync(int idPlaza, string idCandidato) =>
        Task.FromResult(_db.Postulaciones.Any(p => p.IdPlaza == idPlaza && p.IdCandidato == idCandidato));
    public Task<IEnumerable<PostulacionDetalle>> ListarTodasAsync() => Lista(_ => true);
    public Task<IEnumerable<PostulacionDetalle>> ListarPorPlazaAsync(int idPlaza) => Lista(p => p.IdPlaza == idPlaza);
    public Task<IEnumerable<PostulacionDetalle>> ListarPorCandidatoAsync(string id) => Lista(p => p.IdCandidato == id);
    public Task<IEnumerable<PostulacionDetalle>> ListarPorAgenteAsync(string idAgente) =>
        Lista(p => _db.Plazas.Any(pl => pl.IdPlaza == p.IdPlaza && pl.IdAgente == idAgente));
    public Task<int> ContarPorPlazaAsync(int idPlaza) => Task.FromResult(_db.Postulaciones.Count(p => p.IdPlaza == idPlaza));
    public Task<int> ContarPorCandidatoAsync(string id) => Task.FromResult(_db.Postulaciones.Count(p => p.IdCandidato == id));
    public Task<bool> CandidatoSePostuloConAgenteAsync(string idCandidato, string idAgente) =>
        Task.FromResult(_db.Postulaciones.Any(p => p.IdCandidato == idCandidato
            && _db.Plazas.Any(pl => pl.IdPlaza == p.IdPlaza && pl.IdAgente == idAgente)));

    public Task<int> CrearAsync(Postulacion postulacion)
    {
        // Igual que el índice único IX_Postulaciones_IdPlaza_IdCandidato.
        if (_db.Postulaciones.Any(p => p.IdPlaza == postulacion.IdPlaza && p.IdCandidato == postulacion.IdCandidato))
            throw new RegistroDuplicadoException("Duplicado", new Exception());
        postulacion.IdPostulacion = _db.NuevoIdPostulacion();
        _db.Postulaciones.Add(new Postulacion
        {
            IdPostulacion = postulacion.IdPostulacion, IdPlaza = postulacion.IdPlaza, IdCandidato = postulacion.IdCandidato,
            FechaPostulacion = postulacion.FechaPostulacion, Estado = postulacion.Estado
        });
        return Task.FromResult(postulacion.IdPostulacion);
    }

    public Task<bool> ActualizarEstadoAsync(int id, EstadoPostulacion estado)
    {
        var po = _db.Postulaciones.FirstOrDefault(p => p.IdPostulacion == id);
        if (po is null) return Task.FromResult(false);
        po.Estado = estado;
        return Task.FromResult(true);
    }

    public Task<bool> EliminarAsync(int id) => Task.FromResult(_db.Postulaciones.RemoveAll(p => p.IdPostulacion == id) > 0);
}

public class HojaDeVidaRepositoryEnMemoria : IHojaDeVidaRepository
{
    private readonly DatosEnMemoria _db;
    public HojaDeVidaRepositoryEnMemoria(DatosEnMemoria db) => _db = db;

    public Task<HojaDeVida?> ObtenerPorUsuarioAsync(string idUsuario) =>
        Task.FromResult(_db.HojasDeVida.Where(h => h.IdUsuario == idUsuario).Select(DatosEnMemoria.Copia).FirstOrDefault());
    public Task<IEnumerable<HojaDeVida>> ListarAsync(string? soloPostuladosConAgente = null) =>
        Task.FromResult(_db.HojasDeVida.Select(DatosEnMemoria.Copia).ToList().AsEnumerable());

    public Task<int> CrearAsync(HojaDeVida hoja)
    {
        if (_db.HojasDeVida.Any(h => h.IdUsuario == hoja.IdUsuario))
            throw new RegistroDuplicadoException("Duplicado", new Exception());
        hoja.IdCV = _db.NuevoIdHoja();
        _db.HojasDeVida.Add(DatosEnMemoria.Copia(hoja));
        return Task.FromResult(hoja.IdCV);
    }

    public Task<bool> ActualizarAsync(HojaDeVida hoja)
    {
        var actual = _db.HojasDeVida.FirstOrDefault(h => h.IdUsuario == hoja.IdUsuario);
        if (actual is null) return Task.FromResult(false);
        actual.FormacionAcademica = hoja.FormacionAcademica;
        actual.ExperienciaLaboral = hoja.ExperienciaLaboral;
        actual.Competencias = hoja.Competencias;
        return Task.FromResult(true);
    }

    public Task<bool> ActualizarArchivoAsync(string idUsuario, string? archivoCV, string? nombreOriginal)
    {
        var actual = _db.HojasDeVida.FirstOrDefault(h => h.IdUsuario == idUsuario);
        if (actual is null) return Task.FromResult(false);
        actual.ArchivoCV = archivoCV;
        actual.NombreArchivoCV = nombreOriginal;
        return Task.FromResult(true);
    }

    public Task<bool> EliminarAsync(string idUsuario) => Task.FromResult(_db.HojasDeVida.RemoveAll(h => h.IdUsuario == idUsuario) > 0);
}

public class ReporteRepositoryVacio : IReporteRepository
{
    public Task<IEnumerable<ResumenProcesoSeleccion>> ObtenerResumenProcesosAsync() =>
        Task.FromResult(Enumerable.Empty<ResumenProcesoSeleccion>());
}

/// <summary>Almacén de archivos en memoria: no escribe en disco.</summary>
public class AlmacenArchivosEnMemoria : IAlmacenArchivosCV
{
    public Dictionary<string, byte[]> Archivos { get; } = new();

    public async Task<string> GuardarAsync(string idUsuario, Stream contenido)
    {
        using var ms = new MemoryStream();
        await contenido.CopyToAsync(ms);
        var nombre = $"{idUsuario}_{Guid.NewGuid():N}.pdf";
        Archivos[nombre] = ms.ToArray();
        return nombre;
    }

    public string? ObtenerRuta(string? nombreArchivo) =>
        nombreArchivo is not null && Archivos.ContainsKey(nombreArchivo) ? nombreArchivo : null;

    public void Eliminar(string? nombreArchivo)
    {
        if (nombreArchivo is not null)
            Archivos.Remove(nombreArchivo);
    }
}
