namespace BolsaTrabajo.Web.Servicios;

/// <summary>Resultado de una operación de escritura en la API (crear, actualizar, postular...).</summary>
public class ResultadoApi<T>
{
    public bool Exito { get; init; }
    public T? Datos { get; init; }
    public int CodigoEstado { get; init; }

    /// <summary>Mensaje general para mostrar al usuario (por ejemplo, una regla de negocio no cumplida).</summary>
    public string? Mensaje { get; init; }

    /// <summary>Errores de validación por campo, tal como los devuelve la API.</summary>
    public Dictionary<string, string[]> ErroresCampos { get; init; } = new();

    public static ResultadoApi<T> Correcto(T? datos, int codigo) => new() { Exito = true, Datos = datos, CodigoEstado = codigo };
}

/// <summary>La API respondió con un error que la pantalla no puede resolver por sí misma.</summary>
public class ApiException : Exception
{
    public int CodigoEstado { get; }

    public ApiException(int codigoEstado, string mensaje) : base(mensaje) => CodigoEstado = codigoEstado;
}

/// <summary>El token de la sesión venció o dejó de ser válido.</summary>
public class SesionExpiradaException : Exception
{
    public SesionExpiradaException() : base("Su sesión expiró. Inicie sesión nuevamente.") { }
}

/// <summary>No fue posible comunicarse con la API (apagada, sin red o sin respuesta a tiempo).</summary>
public class ApiNoDisponibleException : Exception
{
    public ApiNoDisponibleException(Exception interna)
        : base("No fue posible conectar con el servicio de la Bolsa de Trabajo.", interna) { }
}
