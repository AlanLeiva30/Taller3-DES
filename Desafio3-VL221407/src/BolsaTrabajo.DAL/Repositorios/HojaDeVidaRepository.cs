using BolsaTrabajo.DAL.Data;
using BolsaTrabajo.DAL.Excepciones;
using BolsaTrabajo.Entities;
using Dapper;
using Microsoft.Data.SqlClient;

namespace BolsaTrabajo.DAL.Repositorios;

public interface IHojaDeVidaRepository
{
    Task<HojaDeVida?> ObtenerPorUsuarioAsync(string idUsuario);
    Task<IEnumerable<HojaDeVida>> ListarAsync(string? soloPostuladosConAgente = null);
    Task<int> CrearAsync(HojaDeVida hojaDeVida);
    Task<bool> ActualizarAsync(HojaDeVida hojaDeVida);
    Task<bool> ActualizarArchivoAsync(string idUsuario, string? archivoCV, string? nombreOriginal);
    Task<bool> EliminarAsync(string idUsuario);
}

/// <summary>CRUD de hojas de vida con Dapper. Usa multi-mapping para traer también los datos del candidato.</summary>
public class HojaDeVidaRepository : IHojaDeVidaRepository
{
    private const string SelectConUsuario = """
        SELECT h.IdCV, h.IdUsuario, h.FormacionAcademica, h.ExperienciaLaboral, h.Competencias, h.ArchivoCV, h.NombreArchivoCV,
               u.Id, u.NombreCompleto, u.Email
        FROM HojasDeVida h
        INNER JOIN AspNetUsers u ON u.Id = h.IdUsuario
        """;

    private readonly IDbConnectionFactory _connectionFactory;

    public HojaDeVidaRepository(IDbConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public async Task<HojaDeVida?> ObtenerPorUsuarioAsync(string idUsuario) =>
        (await ConsultarAsync("WHERE h.IdUsuario = @IdUsuario", new { IdUsuario = idUsuario })).SingleOrDefault();

    /// <param name="soloPostuladosConAgente">
    /// Si se indica un agente, devuelve solo las hojas de vida de candidatos que se postularon a sus plazas.
    /// </param>
    public Task<IEnumerable<HojaDeVida>> ListarAsync(string? soloPostuladosConAgente = null) =>
        soloPostuladosConAgente is null
            ? ConsultarAsync("ORDER BY u.NombreCompleto")
            : ConsultarAsync("""
                WHERE EXISTS (
                    SELECT 1 FROM Postulaciones po
                    INNER JOIN Plazas pl ON pl.IdPlaza = po.IdPlaza
                    WHERE po.IdCandidato = h.IdUsuario AND pl.IdAgente = @IdAgente)
                ORDER BY u.NombreCompleto
                """, new { IdAgente = soloPostuladosConAgente });

    public async Task<int> CrearAsync(HojaDeVida hojaDeVida)
    {
        const string sql = """
            INSERT INTO HojasDeVida (IdUsuario, FormacionAcademica, ExperienciaLaboral, Competencias, ArchivoCV)
            OUTPUT INSERTED.IdCV
            VALUES (@IdUsuario, @FormacionAcademica, @ExperienciaLaboral, @Competencias, @ArchivoCV)
            """;
        using var conexion = _connectionFactory.CreateConnection();
        try
        {
            hojaDeVida.IdCV = await conexion.ExecuteScalarAsync<int>(sql, new
            {
                hojaDeVida.IdUsuario,
                hojaDeVida.FormacionAcademica,
                hojaDeVida.ExperienciaLaboral,
                hojaDeVida.Competencias,
                hojaDeVida.ArchivoCV
            });
            return hojaDeVida.IdCV;
        }
        catch (SqlException ex) when (ErroresSql.EsDuplicado(ex))
        {
            throw new RegistroDuplicadoException("El usuario ya tiene una hoja de vida.", ex);
        }
    }

    public async Task<bool> ActualizarAsync(HojaDeVida hojaDeVida)
    {
        const string sql = """
            UPDATE HojasDeVida
            SET FormacionAcademica = @FormacionAcademica,
                ExperienciaLaboral = @ExperienciaLaboral,
                Competencias = @Competencias
            WHERE IdUsuario = @IdUsuario
            """;
        using var conexion = _connectionFactory.CreateConnection();
        return await conexion.ExecuteAsync(sql, new
        {
            hojaDeVida.IdUsuario,
            hojaDeVida.FormacionAcademica,
            hojaDeVida.ExperienciaLaboral,
            hojaDeVida.Competencias
        }) > 0;
    }

    public async Task<bool> ActualizarArchivoAsync(string idUsuario, string? archivoCV, string? nombreOriginal)
    {
        using var conexion = _connectionFactory.CreateConnection();
        return await conexion.ExecuteAsync(
            "UPDATE HojasDeVida SET ArchivoCV = @ArchivoCV, NombreArchivoCV = @NombreArchivoCV WHERE IdUsuario = @IdUsuario",
            new { IdUsuario = idUsuario, ArchivoCV = archivoCV, NombreArchivoCV = nombreOriginal }) > 0;
    }

    public async Task<bool> EliminarAsync(string idUsuario)
    {
        using var conexion = _connectionFactory.CreateConnection();
        return await conexion.ExecuteAsync(
            "DELETE FROM HojasDeVida WHERE IdUsuario = @IdUsuario", new { IdUsuario = idUsuario }) > 0;
    }

    private async Task<IEnumerable<HojaDeVida>> ConsultarAsync(string filtro, object? parametros = null)
    {
        using var conexion = _connectionFactory.CreateConnection();
        return await conexion.QueryAsync<HojaDeVida, Usuario, HojaDeVida>(
            $"{SelectConUsuario}\n{filtro}",
            (hoja, usuario) => { hoja.Usuario = usuario; return hoja; },
            parametros,
            splitOn: "Id");
    }
}
