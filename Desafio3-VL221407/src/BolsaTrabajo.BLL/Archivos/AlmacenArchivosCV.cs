namespace BolsaTrabajo.BLL.Archivos;

/// <summary>Configuración de la carpeta donde se guardan los PDF de las hojas de vida.</summary>
public class ConfiguracionArchivos
{
    public string CarpetaCV { get; set; } = Path.Combine(Directory.GetCurrentDirectory(), "ArchivosCV");
    public long TamanoMaximoBytes { get; set; } = 5 * 1024 * 1024; // 5 MB
}

/// <summary>Guarda, ubica y elimina los archivos PDF de los CV.</summary>
public interface IAlmacenArchivosCV
{
    /// <summary>Guarda el contenido y devuelve el nombre con el que quedó almacenado.</summary>
    Task<string> GuardarAsync(string idUsuario, Stream contenido);

    /// <summary>Ruta completa del archivo, o null si no existe.</summary>
    string? ObtenerRuta(string? nombreArchivo);

    void Eliminar(string? nombreArchivo);
}

/// <summary>Almacena los CV en una carpeta del servidor.</summary>
public class AlmacenArchivosCVLocal : IAlmacenArchivosCV
{
    private readonly ConfiguracionArchivos _configuracion;

    public AlmacenArchivosCVLocal(ConfiguracionArchivos configuracion) => _configuracion = configuracion;

    public async Task<string> GuardarAsync(string idUsuario, Stream contenido)
    {
        Directory.CreateDirectory(_configuracion.CarpetaCV);
        var nombre = $"{idUsuario}_{Guid.NewGuid():N}.pdf";

        await using var destino = File.Create(Path.Combine(_configuracion.CarpetaCV, nombre));
        await contenido.CopyToAsync(destino);
        return nombre;
    }

    public string? ObtenerRuta(string? nombreArchivo)
    {
        if (string.IsNullOrWhiteSpace(nombreArchivo))
            return null;

        // Path.GetFileName evita que un nombre manipulado salga de la carpeta (por ejemplo "..\..\archivo").
        var ruta = Path.Combine(_configuracion.CarpetaCV, Path.GetFileName(nombreArchivo));
        return File.Exists(ruta) ? ruta : null;
    }

    public void Eliminar(string? nombreArchivo)
    {
        var ruta = ObtenerRuta(nombreArchivo);
        if (ruta is not null)
            File.Delete(ruta);
    }
}
