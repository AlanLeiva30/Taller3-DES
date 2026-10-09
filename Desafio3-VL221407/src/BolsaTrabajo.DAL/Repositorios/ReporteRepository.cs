using BolsaTrabajo.DAL.Data;
using BolsaTrabajo.Entities.Consultas;
using Dapper;

namespace BolsaTrabajo.DAL.Repositorios;

/// <summary>Consultas de solo lectura para reportes, hechas con Dapper (SQL directo, más rápido).</summary>
public interface IReporteRepository
{
    Task<IEnumerable<ResumenProcesoSeleccion>> ObtenerResumenProcesosAsync();
}

public class ReporteRepository : IReporteRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ReporteRepository(IDbConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public async Task<IEnumerable<ResumenProcesoSeleccion>> ObtenerResumenProcesosAsync()
    {
        const string sql = """
            SELECT p.IdPlaza, p.Titulo, p.Institucion, p.Estado, p.Publicada,
                   p.FechaPublicacion, p.FechaCierre,
                   u.NombreCompleto AS NombreAgente,
                   COUNT(po.IdPostulacion) AS TotalPostulaciones,
                   SUM(CASE WHEN po.Estado = 'EnRevision' THEN 1 ELSE 0 END) AS EnRevision,
                   SUM(CASE WHEN po.Estado = 'Aprobada'   THEN 1 ELSE 0 END) AS Aprobadas,
                   SUM(CASE WHEN po.Estado = 'Rechazada'  THEN 1 ELSE 0 END) AS Rechazadas
            FROM Plazas p
            INNER JOIN AspNetUsers u ON u.Id = p.IdAgente
            LEFT JOIN Postulaciones po ON po.IdPlaza = p.IdPlaza
            GROUP BY p.IdPlaza, p.Titulo, p.Institucion, p.Estado, p.Publicada,
                     p.FechaPublicacion, p.FechaCierre, u.NombreCompleto
            ORDER BY p.FechaPublicacion DESC
            """;

        using var conexion = _connectionFactory.CreateConnection();
        return await conexion.QueryAsync<ResumenProcesoSeleccion>(sql);
    }
}
