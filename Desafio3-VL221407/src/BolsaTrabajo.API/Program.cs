using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using AutoMapper;
using BolsaTrabajo.API.Seguridad;
using BolsaTrabajo.BLL;
using BolsaTrabajo.DAL;
using BolsaTrabajo.DAL.Data;
using BolsaTrabajo.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Falta la cadena de conexión 'DefaultConnection'.");

// Capas de datos y de negocio
builder.Services.AddCapaDatos(connectionString);
// La carpeta de CV es compartida por la API y el MVC (por defecto: <solución>/ArchivosCV).
builder.Services.AddCapaNegocio(
    builder.Configuration["AutoMapper:LicenseKey"],
    Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath,
        builder.Configuration["Archivos:CarpetaCV"] ?? Path.Combine("..", "..", "ArchivosCV"))));

// Servicios propios de la API
builder.Services.AddSingleton<TokenService>();
builder.Services.AddExceptionHandler<ManejadorExcepciones>();
builder.Services.AddProblemDetails();

// Identity (sin cookies: la API usa tokens JWT)
builder.Services
    .AddIdentityCore<Usuario>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddErrorDescriber<ErroresIdentityEspanol>()
    .AddDefaultTokenProviders();

// Autenticación con JWT
var jwt = builder.Configuration.GetSection("Jwt");
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Conserva los nombres de los claims tal como se escriben en TokenService.
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!))
        };
    });
builder.Services.AddAuthorization();

// Los estados (Abierta, EnRevision...) se envían y reciben como texto, no como números.
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Swagger con botón "Authorize" para pegar el token
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Bolsa de Trabajo - API",
        Version = "v1",
        Description = $"""
            API REST para la contratación de servidores públicos.

            **[🌐 Ir al Portal]({builder.Configuration["Portal:Url"]})**

            **Cómo autenticarse**
            1. Ejecute `POST /api/Auth/login` con su correo y contraseña.
            2. Copie el valor `token` de la respuesta.
            3. Pulse **Authorize** (arriba a la derecha), pegue el token y confirme.

            Los endpoints con candado requieren sesión; la descripción de cada uno indica qué roles pueden usarlo.
            Roles: **Administrador**, **Agente de Selección** y **Candidato**.
            """
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Pegue aquí solo el token obtenido en /api/Auth/login (sin escribir \"Bearer\")."
    });

    // Candado y roles solo en los endpoints protegidos.
    options.OperationFilter<AutorizacionOperationFilter>();

    // Muestra en Swagger los comentarios /// <summary> de controladores, endpoints y DTOs.
    foreach (var ensamblado in new[] { Assembly.GetExecutingAssembly().GetName().Name, "BolsaTrabajo.DTOs" })
    {
        var xml = Path.Combine(AppContext.BaseDirectory, $"{ensamblado}.xml");
        if (File.Exists(xml))
            options.IncludeXmlComments(xml, includeControllerXmlComments: true);
    }

    // Orden lógico de las secciones en Swagger.
    var orden = new[] { "Auth", "Usuarios", "Plazas", "Postulaciones", "HojasDeVida", "Estado" };
    options.OrderActionsBy(api =>
    {
        var controlador = api.ActionDescriptor.RouteValues["controller"] ?? string.Empty;
        var posicion = Array.IndexOf(orden, controlador);
        return $"{(posicion < 0 ? 99 : posicion):D2}_{api.RelativePath}_{api.HttpMethod}";
    });
});

var app = builder.Build();

// Crea/actualiza la base de datos y el administrador inicial
using (var scope = app.Services.CreateScope())
{
    var admin = app.Configuration.GetSection("AdminInicial");
    await DbInitializer.InicializarAsync(
        scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(),
        scope.ServiceProvider.GetRequiredService<UserManager<Usuario>>(),
        admin["Email"]!, admin["Password"]!, admin["NombreCompleto"]!);

    // Agente, Candidato y plazas de ejemplo para probar en Swagger
    var prueba = app.Configuration.GetSection("DatosDePrueba");
    if (prueba.GetValue<bool>("Sembrar"))
    {
        await DbInitializer.SembrarDatosDePruebaAsync(
            scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(),
            scope.ServiceProvider.GetRequiredService<UserManager<Usuario>>(),
            prueba.GetSection("Agente").Get<UsuarioSemilla>()!,
            prueba.GetSection("Candidato").Get<UsuarioSemilla>()!);
    }
}

if (app.Environment.IsDevelopment())
{
    // Detiene el arranque si alguna regla de AutoMapper deja campos de un DTO sin mapear.
    app.Services.GetRequiredService<IMapper>().ConfigurationProvider.AssertConfigurationIsValid();

    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.DocumentTitle = "Bolsa de Trabajo - API";
        options.EnablePersistAuthorization(); // el token se conserva al recargar la página
        options.DisplayRequestDuration();
    });
}

app.UseExceptionHandler();
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
