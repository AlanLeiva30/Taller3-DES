using BolsaTrabajo.DAL.Data;
using BolsaTrabajo.DAL.Repositorios;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BolsaTrabajo.DAL;

/// <summary>
/// Registra los servicios de la capa de datos.
/// - EF Core: Identity, migraciones y datos iniciales.
/// - Dapper: CRUD de Plazas, Postulaciones, Hojas de vida y Usuarios, y reportes.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddCapaDatos(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
        services.AddSingleton<IDbConnectionFactory>(new SqlConnectionFactory(connectionString));

        services.AddScoped<IPlazaRepository, PlazaRepository>();
        services.AddScoped<IPostulacionRepository, PostulacionRepository>();
        services.AddScoped<IHojaDeVidaRepository, HojaDeVidaRepository>();
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IReporteRepository, ReporteRepository>();
        services.AddScoped<IEstadoRepository, EstadoRepository>();

        return services;
    }
}
