using BolsaTrabajo.DAL.Data;
using BolsaTrabajo.DAL.Excepciones;
using BolsaTrabajo.Entities;
using BolsaTrabajo.Entities.Consultas;
using Dapper;
using Microsoft.Data.SqlClient;

namespace BolsaTrabajo.DAL.Repositorios;

public interface IPlazaRepository
{
    Task<Plaza?> ObtenerPorIdAsync(int idPlaza);
    Task<PlazaDetalle?> ObtenerDetalleAsync(int idPlaza);
    Task<IEnumerable<PlazaDetalle>> ListarTodasAsync();
    Task<IEnumerable<PlazaDetalle>> ListarPorAgenteAsync(string idAgente);
    Task<IEnumerable<PlazaDetalle>> ListarDisponiblesAsync(DateTime fechaActual);
    Task<int> ContarAsync();
    Task<int> ContarPorAgenteAsync(string idAgente);
    Task<int> CrearAsync(Plaza plaza);
    Task<bool> ActualizarAsync(Plaza plaza);
    Task<bool> EliminarAsync(int idPlaza);
}

/// <summary>
/// CRUD de plazas con Dapper. Las consultas de listado resuelven el nombre del agente (JOIN)
/// y el total de postulaciones (COUNT) en una sola ida a la base de datos.
/// Los estados se guardan como texto ("Abierta", "EnEvaluacion", "Cerrada").
/// </summary>
public class PlazaRepository : IPlazaRepository
{
    private const string SelectDetalle = """
        SELECT p.IdPlaza, p.Titulo, p.Descripcion, p.FechaPublicacion, p.FechaCierre,
               p.Institucion, p.Estado, p.Publicada, p.IdAgente,
               u.NombreCompleto AS NombreAgente,
               (SELECT COUNT(*) FROM Postulaciones po WHERE po.IdPlaza = p.IdPlaza) AS TotalPostulaciones
        FROM Plazas p
        INNER JOIN AspNetUsers u ON u.Id = p.IdAgente
        """;

    private readonly IDbConnectionFactory _connectionFactory;

    public PlazaRepository(IDbConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public async Task<Plaza?> ObtenerPorIdAsync(int idPlaza)
    {
        const string sql = """
            SELECT IdPlaza, Titulo, Descripcion, FechaPublicacion, FechaCierre, Institucion, Estado, Publicada, IdAgente
            FROM Plazas WHERE IdPlaza = @IdPlaza
            """;
        using var conexion = _connectionFactory.CreateConnection();
        return await conexion.QuerySingleOrDefaultAsync<Plaza>(sql, new { IdPlaza = idPlaza });
    }

    public async Task<PlazaDetalle?> ObtenerDetalleAsync(int idPlaza)
    {
        using var conexion = _connectionFactory.CreateConnection();
        return await conexion.QuerySingleOrDefaultAsync<PlazaDetalle>(
            $"{SelectDetalle}\nWHERE p.IdPlaza = @IdPlaza", new { IdPlaza = idPlaza });
    }

    public async Task<IEnumerable<PlazaDetalle>> ListarTodasAsync()
    {
        using var conexion = _connectionFactory.CreateConnection();
        return await conexion.QueryAsync<PlazaDetalle>($"{SelectDetalle}\nORDER BY p.FechaPublicacion DESC");
    }

    public async Task<IEnumerable<PlazaDetalle>> ListarPorAgenteAsync(string idAgente)
    {
        using var conexion = _connectionFactory.CreateConnection();
        return await conexion.QueryAsync<PlazaDetalle>(
            $"{SelectDetalle}\nWHERE p.IdAgente = @IdAgente\nORDER BY p.FechaPublicacion DESC",
            new { IdAgente = idAgente });
    }

    /// <summary>Plazas publicadas, abiertas y dentro del período de postulación (usa el índice IX_Plazas_Disponibles).</summary>
    public async Task<IEnumerable<PlazaDetalle>> ListarDisponiblesAsync(DateTime fechaActual)
    {
        const string filtro = """
            WHERE p.Publicada = 1
              AND p.Estado = 'Abierta'
              AND p.FechaCierre >= @Hoy
              AND p.FechaPublicacion < @Manana
            ORDER BY p.FechaCierre
            """;
        using var conexion = _connectionFactory.CreateConnection();
        return await conexion.QueryAsync<PlazaDetalle>(
            $"{SelectDetalle}\n{filtro}",
            new { Hoy = fechaActual.Date, Manana = fechaActual.Date.AddDays(1) });
    }

    public async Task<int> ContarAsync()
    {
        using var conexion = _connectionFactory.CreateConnection();
        return await conexion.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Plazas");
    }

    public async Task<int> ContarPorAgenteAsync(string idAgente)
    {
        using var conexion = _connectionFactory.CreateConnection();
        return await conexion.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Plazas WHERE IdAgente = @IdAgente", new { IdAgente = idAgente });
    }

    public async Task<int> CrearAsync(Plaza plaza)
    {
        const string sql = """
            INSERT INTO Plazas (Titulo, Descripcion, FechaPublicacion, FechaCierre, Institucion, Estado, Publicada, IdAgente)
            OUTPUT INSERTED.IdPlaza
            VALUES (@Titulo, @Descripcion, @FechaPublicacion, @FechaCierre, @Institucion, @Estado, @Publicada, @IdAgente)
            """;
        using var conexion = _connectionFactory.CreateConnection();
        plaza.IdPlaza = await conexion.ExecuteScalarAsync<int>(sql, Parametros(plaza));
        return plaza.IdPlaza;
    }

    public async Task<bool> ActualizarAsync(Plaza plaza)
    {
        const string sql = """
            UPDATE Plazas
            SET Titulo = @Titulo, Descripcion = @Descripcion,
                FechaPublicacion = @FechaPublicacion, FechaCierre = @FechaCierre,
                Institucion = @Institucion, Estado = @Estado, Publicada = @Publicada
            WHERE IdPlaza = @IdPlaza
            """;
        using var conexion = _connectionFactory.CreateConnection();
        return await conexion.ExecuteAsync(sql, Parametros(plaza)) > 0;
    }

    public async Task<bool> EliminarAsync(int idPlaza)
    {
        using var conexion = _connectionFactory.CreateConnection();
        try
        {
            return await conexion.ExecuteAsync("DELETE FROM Plazas WHERE IdPlaza = @IdPlaza", new { IdPlaza = idPlaza }) > 0;
        }
        catch (SqlException ex) when (ErroresSql.EsLlaveForanea(ex))
        {
            throw new RegistroEnUsoException("La plaza tiene postulaciones registradas.", ex);
        }
    }

    /// <summary>El estado se envía como texto para coincidir con lo que guarda EF Core.</summary>
    private static object Parametros(Plaza plaza) => new
    {
        plaza.IdPlaza,
        plaza.Titulo,
        plaza.Descripcion,
        plaza.FechaPublicacion,
        plaza.FechaCierre,
        plaza.Institucion,
        Estado = plaza.Estado.ToString(),
        plaza.Publicada,
        plaza.IdAgente
    };
}
