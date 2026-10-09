using Microsoft.Data.SqlClient;

namespace BolsaTrabajo.DAL.Excepciones;

/// <summary>Se intentó insertar un registro que viola un índice único (por ejemplo, postularse dos veces).</summary>
public class RegistroDuplicadoException : Exception
{
    public RegistroDuplicadoException(string mensaje, Exception interna) : base(mensaje, interna) { }
}

/// <summary>Se intentó eliminar un registro que otros registros todavía usan (llave foránea).</summary>
public class RegistroEnUsoException : Exception
{
    public RegistroEnUsoException(string mensaje, Exception interna) : base(mensaje, interna) { }
}

/// <summary>Códigos de error de SQL Server que la capa de datos traduce a excepciones propias.</summary>
internal static class ErroresSql
{
    public static bool EsDuplicado(SqlException ex) => ex.Number is 2601 or 2627;
    public static bool EsLlaveForanea(SqlException ex) => ex.Number == 547;
}
