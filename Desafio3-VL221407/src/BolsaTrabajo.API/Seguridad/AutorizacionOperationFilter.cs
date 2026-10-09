using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BolsaTrabajo.API.Seguridad;

/// <summary>
/// Documenta en Swagger quién puede usar cada endpoint:
/// - Pone el candado (token Bearer) solo en los endpoints protegidos, no en los públicos.
/// - Escribe los roles permitidos en la descripción.
/// - Agrega las respuestas 401 y 403.
/// </summary>
public class AutorizacionOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;
        var autorizaciones = metadata.OfType<IAuthorizeData>().ToList();

        if (metadata.OfType<IAllowAnonymous>().Any() || autorizaciones.Count == 0)
        {
            AgregarDescripcion(operation, "**Acceso:** público, no requiere iniciar sesión.");
            return;
        }

        // Cada [Authorize(Roles = ...)] debe cumplirse; dentro de uno, basta con tener cualquiera de los roles.
        var grupos = autorizaciones
            .Where(a => !string.IsNullOrWhiteSpace(a.Roles))
            .Select(a => string.Join(" o ", a.Roles!.Split(',').Select(r => r.Trim())))
            .ToList();

        AgregarDescripcion(operation, grupos.Count == 0
            ? "**Acceso:** requiere iniciar sesión (cualquier rol)."
            : $"**Acceso:** solo {string.Join(" y además ", grupos)}.");

        operation.Security ??= new List<OpenApiSecurityRequirement>();
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", context.Document)] = []
        });

        operation.Responses ??= new OpenApiResponses();
        operation.Responses.TryAdd("401", new OpenApiResponse { Description = "No ha iniciado sesión o el token no es válido." });
        operation.Responses.TryAdd("403", new OpenApiResponse { Description = "Su rol no tiene permiso para esta acción." });
    }

    private static void AgregarDescripcion(OpenApiOperation operation, string texto) =>
        operation.Description = string.IsNullOrWhiteSpace(operation.Description)
            ? texto
            : $"{texto}\n\n{operation.Description}";
}
