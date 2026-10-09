using BolsaTrabajo.DAL.Data;
using BolsaTrabajo.DAL.Excepciones;
using BolsaTrabajo.Entities;
using BolsaTrabajo.Entities.Consultas;
using Dapper;
using Microsoft.Data.SqlClient;

namespace BolsaTrabajo.DAL.Repositorios;

public interface IUsuarioRepository
{
    Task<IEnumerable<Usuario>> ListarAsync(string? rol = null);
    Task<Usuario?> ObtenerPorIdAsync(string idUsuario);
    Task<IEnumerable<RolResumen>> ListarRolesAsync();
    Task<bool> ActualizarDatosAsync(string idUsuario, string nombreCompleto, string? telefono);
    Task<bool> EliminarAsync(string idUsuario);
}

/// <summary>
/// Lectura, actualización y eliminación de usuarios con Dapper.
/// La creación de usuarios y la asignación de roles se hacen con ASP.NET Core Identity (UserManager),
/// porque Identity es quien cifra la contraseña y mantiene sus tablas internas.
/// </summary>
public class UsuarioRepository : IUsuarioRepository
{
    private const string SelectUsuarios =
        "SELECT Id, NombreCompleto, Email, UserName, PhoneNumber, Rol FROM AspNetUsers";

    private readonly IDbConnectionFactory _connectionFactory;

    public UsuarioRepository(IDbConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public async Task<IEnumerable<Usuario>> ListarAsync(string? rol = null)
    {
        using var conexion = _connectionFactory.CreateConnection();
        return rol is null
            ? await conexion.QueryAsync<Usuario>($"{SelectUsuarios} ORDER BY NombreCompleto")
            : await conexion.QueryAsync<Usuario>($"{SelectUsuarios} WHERE Rol = @Rol ORDER BY NombreCompleto", new { Rol = rol });
    }

    public async Task<Usuario?> ObtenerPorIdAsync(string idUsuario)
    {
        using var conexion = _connectionFactory.CreateConnection();
        return await conexion.QuerySingleOrDefaultAsync<Usuario>($"{SelectUsuarios} WHERE Id = @Id", new { Id = idUsuario });
    }

    public async Task<IEnumerable<RolResumen>> ListarRolesAsync()
    {
        const string sql = """
            SELECT r.Name AS Nombre, COUNT(ur.UserId) AS TotalUsuarios
            FROM AspNetRoles r
            LEFT JOIN AspNetUserRoles ur ON ur.RoleId = r.Id
            GROUP BY r.Name
            ORDER BY r.Name
            """;
        using var conexion = _connectionFactory.CreateConnection();
        return await conexion.QueryAsync<RolResumen>(sql);
    }

    public async Task<bool> ActualizarDatosAsync(string idUsuario, string nombreCompleto, string? telefono)
    {
        // ConcurrencyStamp cambia en cada modificación, igual que lo hace Identity.
        const string sql = """
            UPDATE AspNetUsers
            SET NombreCompleto = @NombreCompleto, PhoneNumber = @Telefono,
                ConcurrencyStamp = CONVERT(nvarchar(36), NEWID())
            WHERE Id = @Id
            """;
        using var conexion = _connectionFactory.CreateConnection();
        return await conexion.ExecuteAsync(sql, new { Id = idUsuario, NombreCompleto = nombreCompleto, Telefono = telefono }) > 0;
    }

    /// <summary>
    /// Elimina el usuario. La base de datos borra en cascada sus roles, claims, tokens y hoja de vida,
    /// pero impide borrarlo si tiene plazas o postulaciones (llaves foráneas RESTRICT).
    /// </summary>
    public async Task<bool> EliminarAsync(string idUsuario)
    {
        using var conexion = _connectionFactory.CreateConnection();
        try
        {
            return await conexion.ExecuteAsync("DELETE FROM AspNetUsers WHERE Id = @Id", new { Id = idUsuario }) > 0;
        }
        catch (SqlException ex) when (ErroresSql.EsLlaveForanea(ex))
        {
            throw new RegistroEnUsoException("El usuario tiene plazas o postulaciones registradas.", ex);
        }
    }
}
