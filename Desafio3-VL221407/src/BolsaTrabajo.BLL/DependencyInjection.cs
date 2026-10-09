using BolsaTrabajo.BLL.Archivos;
using BolsaTrabajo.BLL.Mapping;
using BolsaTrabajo.BLL.Servicios;
using Microsoft.Extensions.DependencyInjection;

namespace BolsaTrabajo.BLL;

/// <summary>Registra los servicios de la capa de lógica de negocio.</summary>
public static class DependencyInjection
{
    /// <param name="autoMapperLicenseKey">
    /// Clave de AutoMapper (gratuita para uso educativo en luckypennysoftware.com).
    /// Si está vacía, AutoMapper funciona igual pero escribe un aviso en el registro.
    /// </param>
    /// <param name="carpetaCV">Carpeta donde se guardan los PDF. Si es null se usa "ArchivosCV" en la carpeta de ejecución.</param>
    public static IServiceCollection AddCapaNegocio(
        this IServiceCollection services, string? autoMapperLicenseKey = null, string? carpetaCV = null)
    {
        services.AddAutoMapper(cfg =>
        {
            if (!string.IsNullOrWhiteSpace(autoMapperLicenseKey))
                cfg.LicenseKey = autoMapperLicenseKey;
            cfg.AddProfile<MappingProfile>();
        });

        var archivos = new ConfiguracionArchivos();
        if (!string.IsNullOrWhiteSpace(carpetaCV))
            archivos.CarpetaCV = carpetaCV;
        services.AddSingleton(archivos);
        services.AddSingleton<IAlmacenArchivosCV, AlmacenArchivosCVLocal>();

        services.AddScoped<IPlazaService, PlazaService>();
        services.AddScoped<IPostulacionService, PostulacionService>();
        services.AddScoped<IHojaDeVidaService, HojaDeVidaService>();
        services.AddScoped<IUsuarioService, UsuarioService>();
        services.AddScoped<IEstadoService, EstadoService>();

        return services;
    }
}
