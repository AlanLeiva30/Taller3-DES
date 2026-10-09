using BolsaTrabajo.DAL.Data;
using BolsaTrabajo.Entities.Consultas;
using Dapper;

namespace BolsaTrabajo.DAL.Repositorios;

public interface IEstadoRepository
{
    Task<EstadoSistema> ObtenerAsync();
}

/// <summary>Resumen del sistema con Dapper: dos consultas en un solo viaje a la base (QueryMultiple).</summary>
public class EstadoRepository : IEstadoRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public EstadoRepository(IDbConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public async Task<EstadoSistema> ObtenerAsync()
    {
        const string sql = """
            SELECT Name FROM AspNetRoles ORDER BY Name;
            SELECT (SELECT COUNT(*) FROM AspNetUsers)   AS TotalUsuarios,
                   (SELECT COUNT(*) FROM Plazas)        AS TotalPlazas,
                   (SELECT COUNT(*) FROM Postulaciones) AS TotalPostulaciones,
                   (SELECT COUNT(*) FROM HojasDeVida)   AS TotalHojasDeVida;
            """;

        using var conexion = _connectionFactory.CreateConnection();
        using var resultados = await conexion.QueryMultipleAsync(sql);

        var roles = (await resultados.ReadAsync<string>()).ToList();
        var estado = await resultados.ReadSingleAsync<EstadoSistema>();
        estado.Roles = roles;
        return estado;
    }
}
