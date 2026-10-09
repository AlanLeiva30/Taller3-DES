using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using BolsaTrabajo.Web.Infraestructura;
using BolsaTrabajo.Web.Servicios;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.WebEncoders;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------
// Cliente de la API: el MVC no accede a la base de datos, todo lo
// consulta y lo guarda a través de la API REST.
// ---------------------------------------------------------------
var urlApi = builder.Configuration["Api:BaseUrl"]
    ?? throw new InvalidOperationException("Falta la configuración 'Api:BaseUrl' en appsettings.json.");

builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<TokenApiHandler>();
builder.Services
    .AddHttpClient<BolsaApiClient>(cliente =>
    {
        cliente.BaseAddress = new Uri(urlApi);
        cliente.Timeout = TimeSpan.FromSeconds(builder.Configuration.GetValue("Api:TiempoEsperaSegundos", 30));
    })
    .AddHttpMessageHandler<TokenApiHandler>();

// ---------------------------------------------------------------
// Sesión del usuario: cookie cifrada que guarda el token JWT de la API.
// ---------------------------------------------------------------
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opciones =>
    {
        opciones.LoginPath = "/Cuenta/Login";
        opciones.LogoutPath = "/Cuenta/Logout";
        opciones.AccessDeniedPath = "/Cuenta/AccesoDenegado";
        opciones.Cookie.Name = "BolsaTrabajo.Sesion";
        opciones.Cookie.HttpOnly = true;
        opciones.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        opciones.Cookie.SameSite = SameSiteMode.Lax;
        opciones.ExpireTimeSpan = TimeSpan.FromMinutes(120);
        opciones.SlidingExpiration = false; // la sesión dura lo mismo que el token de la API
    });
builder.Services.AddAuthorization();

// Las vistas escriben "contraseña" tal cual, en lugar de "contrase&#xF1;a".
builder.Services.Configure<WebEncoderOptions>(opciones =>
    opciones.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));

builder.Services.AddControllersWithViews(opciones =>
{
    opciones.Filters.Add<ManejadorErroresApiFilter>();
    opciones.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()); // protege todos los formularios
});

var app = builder.Build();

// Fechas y números en formato de El Salvador
var cultura = new CultureInfo("es-SV");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(cultura),
    SupportedCultures = new[] { cultura },
    SupportedUICultures = new[] { cultura }
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/Home/Estado/{0}");
app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
