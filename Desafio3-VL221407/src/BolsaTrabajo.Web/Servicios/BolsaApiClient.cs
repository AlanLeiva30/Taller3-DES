using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BolsaTrabajo.DTOs.Auth;
using BolsaTrabajo.DTOs.HojasDeVida;
using BolsaTrabajo.DTOs.Plazas;
using BolsaTrabajo.DTOs.Postulaciones;
using BolsaTrabajo.DTOs.Sistema;
using BolsaTrabajo.DTOs.Usuarios;
using BolsaTrabajo.Entities.Enums;

namespace BolsaTrabajo.Web.Servicios;

/// <summary>
/// Cliente HTTP de la API de la Bolsa de Trabajo. El MVC no accede a la base de datos:
/// todo lo obtiene o lo envía a través de estos métodos.
/// </summary>
public class BolsaApiClient
{
    /// <summary>Mismo formato JSON que usa la API (camelCase y estados como texto).</summary>
    public static readonly JsonSerializerOptions OpcionesJson = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _http;
    private readonly ILogger<BolsaApiClient> _logger;

    public BolsaApiClient(HttpClient http, ILogger<BolsaApiClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    // ---------------- Autenticación y perfil ----------------

    public Task<ResultadoApi<RespuestaLoginDto>> IniciarSesionAsync(LoginDto datos) =>
        EnviarAsync<RespuestaLoginDto>(HttpMethod.Post, "api/Auth/login", datos);

    public Task<ResultadoApi<RespuestaLoginDto>> RegistrarseAsync(RegistroCandidatoDto datos) =>
        EnviarAsync<RespuestaLoginDto>(HttpMethod.Post, "api/Auth/registro", datos);

    public Task<UsuarioDto> ObtenerMiPerfilAsync() => ObtenerAsync<UsuarioDto>("api/Auth/perfil");

    public Task<ResultadoApi<UsuarioDto>> ActualizarMiPerfilAsync(ActualizarPerfilDto datos) =>
        EnviarAsync<UsuarioDto>(HttpMethod.Put, "api/Usuarios/mi-perfil", datos);

    // ---------------- Sistema ----------------

    public Task<EstadoSistemaDto> ObtenerEstadoAsync() => ObtenerAsync<EstadoSistemaDto>("api/Estado");

    // ---------------- Plazas ----------------

    public Task<List<PlazaDto>> ListarPlazasDisponiblesAsync() =>
        ObtenerAsync<List<PlazaDto>>("api/Plazas/disponibles");

    /// <summary>Devuelve null si la plaza no existe o el usuario no puede verla.</summary>
    public Task<PlazaDto?> ObtenerPlazaAsync(int idPlaza) =>
        ObtenerOpcionalAsync<PlazaDto>($"api/Plazas/{idPlaza}");

    /// <summary>Administrador: todas las plazas. Agente: solo las suyas.</summary>
    public Task<List<PlazaDto>> ListarPlazasAsync() => ObtenerAsync<List<PlazaDto>>("api/Plazas");

    public Task<List<ResumenProcesoDto>> ObtenerMonitoreoAsync() =>
        ObtenerAsync<List<ResumenProcesoDto>>("api/Plazas/monitoreo");

    public Task<ResultadoApi<PlazaDto>> CrearPlazaAsync(GuardarPlazaDto datos) =>
        EnviarAsync<PlazaDto>(HttpMethod.Post, "api/Plazas", datos);

    public Task<ResultadoApi<PlazaDto>> EditarPlazaAsync(int idPlaza, GuardarPlazaDto datos) =>
        EnviarAsync<PlazaDto>(HttpMethod.Put, $"api/Plazas/{idPlaza}", datos);

    public Task<ResultadoApi<object>> EliminarPlazaAsync(int idPlaza) =>
        EnviarAsync<object>(HttpMethod.Delete, $"api/Plazas/{idPlaza}");

    public Task<ResultadoApi<PlazaDto>> PublicarPlazaAsync(int idPlaza) =>
        EnviarAsync<PlazaDto>(HttpMethod.Put, $"api/Plazas/{idPlaza}/publicar");

    public Task<ResultadoApi<PlazaDto>> CambiarEstadoPlazaAsync(int idPlaza, EstadoPlaza estado) =>
        EnviarAsync<PlazaDto>(HttpMethod.Put, $"api/Plazas/{idPlaza}/estado", new CambiarEstadoPlazaDto { Estado = estado });

    // ---------------- Postulaciones ----------------

    public Task<ResultadoApi<PostulacionDto>> PostularAsync(int idPlaza) =>
        EnviarAsync<PostulacionDto>(HttpMethod.Post, "api/Postulaciones", new CrearPostulacionDto { IdPlaza = idPlaza });

    public Task<List<PostulacionDto>> ListarMisPostulacionesAsync() =>
        ObtenerAsync<List<PostulacionDto>>("api/Postulaciones/mis-postulaciones");

    public Task<List<PostulacionDto>> ListarPostulacionesRecibidasAsync() =>
        ObtenerAsync<List<PostulacionDto>>("api/Postulaciones/recibidas");

    public Task<List<PostulacionDto>> ListarTodasLasPostulacionesAsync() =>
        ObtenerAsync<List<PostulacionDto>>("api/Postulaciones");

    public Task<List<PostulacionDto>> ListarPostulacionesDePlazaAsync(int idPlaza) =>
        ObtenerAsync<List<PostulacionDto>>($"api/Postulaciones/plaza/{idPlaza}");

    public Task<ResultadoApi<PostulacionDto>> EvaluarPostulacionAsync(int idPostulacion, EstadoPostulacion resultado) =>
        EnviarAsync<PostulacionDto>(HttpMethod.Put, $"api/Postulaciones/{idPostulacion}/evaluar",
            new EvaluarPostulacionDto { Estado = resultado });

    // ---------------- Hojas de vida ----------------

    /// <summary>Devuelve null si el candidato todavía no registró su hoja de vida.</summary>
    public Task<HojaDeVidaDto?> ObtenerMiHojaDeVidaAsync() =>
        ObtenerOpcionalAsync<HojaDeVidaDto>("api/HojasDeVida/mi-hoja");

    public Task<ResultadoApi<HojaDeVidaDto>> CrearMiHojaDeVidaAsync(GuardarHojaDeVidaDto datos) =>
        EnviarAsync<HojaDeVidaDto>(HttpMethod.Post, "api/HojasDeVida/mi-hoja", datos);

    public Task<ResultadoApi<HojaDeVidaDto>> ActualizarMiHojaDeVidaAsync(GuardarHojaDeVidaDto datos) =>
        EnviarAsync<HojaDeVidaDto>(HttpMethod.Put, "api/HojasDeVida/mi-hoja", datos);

    /// <summary>Envía el PDF tal como lo subió el usuario; la API valida tipo, tamaño y contenido.</summary>
    public Task<ResultadoApi<HojaDeVidaDto>> SubirMiCvAsync(Stream contenido, string nombreArchivo, string? tipo)
    {
        var archivo = new StreamContent(contenido);
        archivo.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(tipo) ? "application/octet-stream" : tipo);
        var formulario = new MultipartFormDataContent { { archivo, "Archivo", nombreArchivo } };
        return EnviarContenidoAsync<HojaDeVidaDto>(HttpMethod.Post, "api/HojasDeVida/mi-hoja/archivo", formulario);
    }

    /// <summary>Hoja de vida de un candidato (para el agente o el administrador). Null si no la ha registrado.</summary>
    public Task<HojaDeVidaDto?> ObtenerHojaDeVidaDeCandidatoAsync(string idCandidato) =>
        ObtenerOpcionalAsync<HojaDeVidaDto>($"api/HojasDeVida/usuario/{Uri.EscapeDataString(idCandidato)}");

    public Task<byte[]?> DescargarMiCvAsync() => DescargarAsync("api/HojasDeVida/mi-hoja/archivo");

    public Task<byte[]?> DescargarCvDeCandidatoAsync(string idCandidato) =>
        DescargarAsync($"api/HojasDeVida/usuario/{Uri.EscapeDataString(idCandidato)}/archivo");

    // ---------------- Usuarios y roles ----------------

    public Task<List<UsuarioDto>> ListarUsuariosAsync(string? rol = null) =>
        ObtenerAsync<List<UsuarioDto>>(string.IsNullOrWhiteSpace(rol)
            ? "api/Usuarios"
            : $"api/Usuarios?rol={Uri.EscapeDataString(rol)}");

    public Task<List<RolDto>> ListarRolesAsync() => ObtenerAsync<List<RolDto>>("api/Usuarios/roles");

    public Task<ResultadoApi<UsuarioDto>> CrearUsuarioAsync(CrearUsuarioDto datos) =>
        EnviarAsync<UsuarioDto>(HttpMethod.Post, "api/Usuarios", datos);

    public Task<ResultadoApi<UsuarioDto>> CambiarRolAsync(string idUsuario, string rol) =>
        EnviarAsync<UsuarioDto>(HttpMethod.Put, $"api/Usuarios/{Uri.EscapeDataString(idUsuario)}/rol", new CambiarRolDto { Rol = rol });

    public Task<ResultadoApi<object>> EliminarUsuarioAsync(string idUsuario) =>
        EnviarAsync<object>(HttpMethod.Delete, $"api/Usuarios/{Uri.EscapeDataString(idUsuario)}");

    // =========================================================
    // Métodos genéricos
    // =========================================================

    /// <summary>GET que debe responder bien; cualquier error se convierte en excepción.</summary>
    private async Task<T> ObtenerAsync<T>(string ruta)
    {
        using var respuesta = await EjecutarAsync(new HttpRequestMessage(HttpMethod.Get, ruta));
        if (respuesta.IsSuccessStatusCode)
            return (await respuesta.Content.ReadFromJsonAsync<T>(OpcionesJson))!;

        throw await CrearExcepcionAsync(respuesta);
    }

    /// <summary>GET que puede no encontrar el recurso (404 → null).</summary>
    private async Task<T?> ObtenerOpcionalAsync<T>(string ruta) where T : class
    {
        using var respuesta = await EjecutarAsync(new HttpRequestMessage(HttpMethod.Get, ruta));
        if (respuesta.StatusCode == HttpStatusCode.NotFound)
            return null;
        if (respuesta.IsSuccessStatusCode)
            return await respuesta.Content.ReadFromJsonAsync<T>(OpcionesJson);

        throw await CrearExcepcionAsync(respuesta);
    }

    /// <summary>
    /// POST/PUT/DELETE. Los errores que el usuario puede corregir (400, 403, 404, 409, o 401 sin sesión)
    /// se devuelven como resultado con su mensaje; el resto se convierte en excepción.
    /// </summary>
    private Task<ResultadoApi<T>> EnviarAsync<T>(HttpMethod metodo, string ruta, object? cuerpo = null) =>
        EnviarContenidoAsync<T>(metodo, ruta,
            cuerpo is null ? null : JsonContent.Create(cuerpo, cuerpo.GetType(), options: OpcionesJson));

    /// <summary>GET de un archivo. Devuelve null si no existe (404).</summary>
    private async Task<byte[]?> DescargarAsync(string ruta)
    {
        using var respuesta = await EjecutarAsync(new HttpRequestMessage(HttpMethod.Get, ruta));
        if (respuesta.StatusCode == HttpStatusCode.NotFound)
            return null;
        if (respuesta.IsSuccessStatusCode)
            return await respuesta.Content.ReadAsByteArrayAsync();

        throw await CrearExcepcionAsync(respuesta);
    }

    private async Task<ResultadoApi<T>> EnviarContenidoAsync<T>(HttpMethod metodo, string ruta, HttpContent? contenido)
    {
        var solicitud = new HttpRequestMessage(metodo, ruta) { Content = contenido };

        using var respuesta = await EjecutarAsync(solicitud);

        if (respuesta.IsSuccessStatusCode)
        {
            var datos = respuesta.StatusCode == HttpStatusCode.NoContent
                ? default
                : await respuesta.Content.ReadFromJsonAsync<T>(OpcionesJson);
            return ResultadoApi<T>.Correcto(datos, (int)respuesta.StatusCode);
        }

        var codigo = (int)respuesta.StatusCode;
        var esErrorDelUsuario = codigo is 400 or 403 or 404 or 409
            || (codigo == 401 && solicitud.Headers.Authorization is null);
        if (!esErrorDelUsuario)
            throw await CrearExcepcionAsync(respuesta);

        var problema = await LeerProblemaAsync(respuesta);
        return new ResultadoApi<T>
        {
            Exito = false,
            CodigoEstado = codigo,
            ErroresCampos = problema?.Errors ?? new(),
            // Si hay errores por campo se muestran junto a cada campo; el título genérico no se muestra.
            Mensaje = problema?.Errors is { Count: > 0 } ? null : problema?.Detail ?? MensajePorDefecto(codigo)
        };
    }

    private async Task<HttpResponseMessage> EjecutarAsync(HttpRequestMessage solicitud)
    {
        try
        {
            return await _http.SendAsync(solicitud);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "No se pudo conectar con la API en {Url}", _http.BaseAddress);
            throw new ApiNoDisponibleException(ex);
        }
    }

    private async Task<Exception> CrearExcepcionAsync(HttpResponseMessage respuesta)
    {
        var codigo = (int)respuesta.StatusCode;

        // 401 con token enviado = el token venció o ya no es válido.
        if (codigo == 401 && respuesta.RequestMessage?.Headers.Authorization is not null)
            return new SesionExpiradaException();

        var problema = await LeerProblemaAsync(respuesta);
        if (codigo >= 500)
            _logger.LogError("La API respondió {Codigo} en {Ruta}: {Detalle}",
                codigo, respuesta.RequestMessage?.RequestUri, problema?.Detail);

        return new ApiException(codigo, problema?.Detail ?? MensajePorDefecto(codigo));
    }

    private static async Task<ProblemaApi?> LeerProblemaAsync(HttpResponseMessage respuesta)
    {
        try
        {
            return await respuesta.Content.ReadFromJsonAsync<ProblemaApi>(OpcionesJson);
        }
        catch (Exception)
        {
            return null; // la respuesta no traía un ProblemDetails
        }
    }

    private static string MensajePorDefecto(int codigo) => codigo switch
    {
        400 => "Los datos enviados no son válidos.",
        401 => "Debe iniciar sesión para continuar.",
        403 => "No tiene permiso para realizar esta acción.",
        404 => "No se encontró la información solicitada.",
        _ => "Ocurrió un problema inesperado. Intente de nuevo más tarde."
    };

    /// <summary>Formato de error estándar (ProblemDetails) que devuelve la API.</summary>
    private sealed class ProblemaApi
    {
        public string? Title { get; set; }
        public string? Detail { get; set; }
        public Dictionary<string, string[]>? Errors { get; set; }
    }
}
