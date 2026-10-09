using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;

namespace BolsaTrabajo.Web.Servicios;

/// <summary>
/// Agrega automáticamente el token JWT del usuario (guardado en su cookie de sesión)
/// a cada petición que el MVC hace a la API.
/// </summary>
public class TokenApiHandler : DelegatingHandler
{
    public const string NombreToken = "access_token";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public TokenApiHandler(IHttpContextAccessor httpContextAccessor) => _httpContextAccessor = httpContextAccessor;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var contexto = _httpContextAccessor.HttpContext;
        if (contexto?.User.Identity?.IsAuthenticated == true)
        {
            var token = await contexto.GetTokenAsync(NombreToken);
            if (!string.IsNullOrEmpty(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
