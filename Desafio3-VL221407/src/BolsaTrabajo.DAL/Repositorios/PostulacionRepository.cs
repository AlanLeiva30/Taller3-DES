using BolsaTrabajo.DAL.Data;
using BolsaTrabajo.DAL.Excepciones;
using BolsaTrabajo.Entities;
using BolsaTrabajo.Entities.Consultas;
using BolsaTrabajo.Entities.Enums;
using Dapper;
using Microsoft.Data.SqlClient;

namespace BolsaTrabajo.DAL.Repositorios;

public interface IPostulacionRepository
{
    Task<PostulacionDetalle?> ObtenerDetalleAsync(int idPostulacion);
    Task<bool> ExisteAsync(int idPlaza, string idCandidato);
    Task<IEnumerable<PostulacionDetalle>> ListarTodasAsync();
    Task<IEnumerable<PostulacionDetalle>> ListarPorPlazaAsync(int idPlaza);
    Task<IEnumerable<PostulacionDetalle>> ListarPorCandidatoAsync(string idCandidato);
    Task<IEnumerable<PostulacionDetalle>> ListarPorAgenteAsync(string idAgente);
    Task<int> ContarPorPlazaAsync(int idPlaza);
    Task<int> ContarPorCandidatoAsync(string idCandidato);
    Task<bool> CandidatoSePostuloConAgenteAsync(string idCandidato, string idAgente);
    Task<int> CrearAsync(Postulacion postulacion);
    Task<bool> ActualizarEstadoAsync(int idPostulacion, EstadoPostulacion estado);
    Task<bool> EliminarAsync(int idPostulacion);
}

/// <summary>CRUD de postulaciones con Dapper. Los listados traen plaza y candidato con JOIN en una sola consulta.</summary>
public class PostulacionRepository : IPostulacionRepository
{
    private const string SelectDetalle = """
        SELECT po.IdPostulacion, po.IdPlaza,
               pl.Titulo AS TituloPlaza, pl.Institucion, pl.Estado AS EstadoPlaza, pl.IdAgente,
               po.IdCandidato, u.NombreCompleto AS NombreCandidato, u.Email AS EmailCandidato,
               po.FechaPostulacion, po.Estado
        FROM Postulaciones po
        INNER JOIN Plazas pl ON pl.IdPlaza = po.IdPlaza
        INNER JOIN AspNetUsers u ON u.Id = po.IdCandidato
        """;

    private readonly IDbConnectionFactory _connectionFactory;

    public PostulacionRepository(IDbConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public async Task<PostulacionDetalle?> ObtenerDetalleAsync(int idPostulacion)
    {
        using var conexion = _connectionFactory.CreateConnection();
        return await conexion.QuerySingleOrDefaultAsync<PostulacionDetalle>(
            $"{SelectDetalle}\nWHERE po.IdPostulacion = @IdPostulacion", new { IdPostulacion = idPostulacion });
    }

    /// <summary>Usa el índice único IX_Postulaciones_IdPlaza_IdCandidato.</summary>
    public async Task<bool> ExisteAsync(int idPlaza, string idCandidato)
    {
        const string sql = """
            SELECT CASE WHEN EXISTS (
                SELECT 1 FROM Postulaciones WHERE IdPlaza = @IdPlaza AND IdCandidato = @IdCandidato
            ) THEN 1 ELSE 0 END
            """;
        using var conexion = _connectionFactory.CreateConnection();
        return await conexion.ExecuteScalarAsync<bool>(sql, new { IdPlaza = idPlaza, IdCandidato = idCandidato });
    }

    public async Task<IEnumerable<PostulacionDetalle>> ListarTodasAsync()
    {
        using var conexion = _connectionFactory.CreateConnection();
        return await conexion.QueryAsync<PostulacionDetalle>($"{SelectDetalle}\nORDER BY po.FechaPostulacion DESC");
    }

    public async Task<IEnumerable<PostulacionDetalle>> ListarPorPlazaAsync(int idPlaza)
    {
        using var conexion = _connectionFactory.CreateConnection();
        return await conexion.QueryAsync<PostulacionDetalle>(
            $"{SelectDetalle}\nWHERE po.IdPlaza = @IdPlaza\nORDER BY po.FechaPostulacion", new { IdPlaza = idPlaza });
    }

    public async Task<IEnumerable<PostulacionDetalle>> ListarPorCandidatoAsync(string idCandidato)
    {
        using var conexion = _connectionFactory.CreateConnection();
        return await conexion.QueryAsync<PostulacionDetalle>(
            $"{SelectDetalle}\nWHERE po.IdCandidato = @IdCandidato\nORDER BY po.FechaPostulacion DESC",
            new { IdCandidato = idCandidato });
    }

    /// <summary>Todas las postulaciones recibidas en las plazas de un agente.</summary>
    public async Task<IEnumerable<PostulacionDetalle>> ListarPorAgenteAsync(string idAgente)
    {
        using var conexion = _connectionFactory.CreateConnection();
        return await conexion.QueryAsync<PostulacionDetalle>(
            $"{SelectDetalle}\nWHERE pl.IdAgente = @IdAgente\nORDER BY po.FechaPostulacion DESC",
            new { IdAgente = idAgente });
    }

    public async Task<int> ContarPorPlazaAsync(int idPlaza)
    {
        using var conexion = _connectionFactory.CreateConnection();
        return await conexion.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Postulaciones WHERE IdPlaza = @IdPlaza", new { IdPlaza = idPlaza });
    }

    public async Task<int> ContarPorCandidatoAsync(string idCandidato)
    {
        using var conexion = _connectionFactory.CreateConnection();
        return await conexion.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Postulaciones WHERE IdCandidato = @IdCandidato", new { IdCandidato = idCandidato });
    }

    /// <summary>¿El candidato se postuló a alguna plaza creada por este agente?</summary>
    public async Task<bool> CandidatoSePostuloConAgenteAsync(string idCandidato, string idAgente)
    {
        const string sql = """
            SELECT CASE WHEN EXISTS (
                SELECT 1 FROM Postulaciones po
                INNER JOIN Plazas pl ON pl.IdPlaza = po.IdPlaza
                WHERE po.IdCandidato = @IdCandidato AND pl.IdAgente = @IdAgente
            ) THEN 1 ELSE 0 END
            """;
        using var conexion = _connectionFactory.CreateConnection();
        return await conexion.ExecuteScalarAsync<bool>(sql, new { IdCandidato = idCandidato, IdAgente = idAgente });
    }

    public async Task<int> CrearAsync(Postulacion postulacion)
    {
        const string sql = """
            INSERT INTO Postulaciones (IdPlaza, IdCandidato, FechaPostulacion, Estado)
            OUTPUT INSERTED.IdPostulacion
            VALUES (@IdPlaza, @IdCandidato, @FechaPostulacion, @Estado)
            """;
        using var conexion = _connectionFactory.CreateConnection();
        try
        {
            postulacion.IdPostulacion = await conexion.ExecuteScalarAsync<int>(sql, new
            {
                postulacion.IdPlaza,
                postulacion.IdCandidato,
                postulacion.FechaPostulacion,
                Estado = postulacion.Estado.ToString()
            });
            return postulacion.IdPostulacion;
        }
        catch (SqlException ex) when (ErroresSql.EsDuplicado(ex))
        {
            // Protección extra: si dos solicitudes llegan al mismo tiempo, el índice único lo impide.
            throw new RegistroDuplicadoException("El candidato ya se postuló a esta plaza.", ex);
        }
    }

    public async Task<bool> ActualizarEstadoAsync(int idPostulacion, EstadoPostulacion estado)
    {
        using var conexion = _connectionFactory.CreateConnection();
        return await conexion.ExecuteAsync(
            "UPDATE Postulaciones SET Estado = @Estado WHERE IdPostulacion = @IdPostulacion",
            new { IdPostulacion = idPostulacion, Estado = estado.ToString() }) > 0;
    }

    public async Task<bool> EliminarAsync(int idPostulacion)
    {
        using var conexion = _connectionFactory.CreateConnection();
        return await conexion.ExecuteAsync(
            "DELETE FROM Postulaciones WHERE IdPostulacion = @IdPostulacion", new { IdPostulacion = idPostulacion }) > 0;
    }
}
