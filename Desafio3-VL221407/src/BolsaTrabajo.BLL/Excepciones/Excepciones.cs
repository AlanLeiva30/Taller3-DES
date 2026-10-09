namespace BolsaTrabajo.BLL.Excepciones;

/// <summary>Se incumplió una regla de negocio (por ejemplo, postularse dos veces). Equivale a HTTP 400.</summary>
public class ReglaNegocioException : Exception
{
    public ReglaNegocioException(string mensaje) : base(mensaje) { }
}

/// <summary>El registro solicitado no existe. Equivale a HTTP 404.</summary>
public class NoEncontradoException : Exception
{
    public NoEncontradoException(string mensaje) : base(mensaje) { }
}

/// <summary>No hay un usuario autenticado. Equivale a HTTP 401.</summary>
public class NoAutenticadoException : Exception
{
    public NoAutenticadoException(string mensaje = "Debe iniciar sesión para realizar esta acción.") : base(mensaje) { }
}

/// <summary>El usuario está autenticado pero no tiene permiso sobre ese registro. Equivale a HTTP 403.</summary>
public class AccesoDenegadoException : Exception
{
    public AccesoDenegadoException(string mensaje = "No tiene permiso para realizar esta acción.") : base(mensaje) { }
}
