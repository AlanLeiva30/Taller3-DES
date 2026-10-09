using BolsaTrabajo.Entities;
using BolsaTrabajo.Entities.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BolsaTrabajo.DAL.Data;

/// <summary>Datos de una cuenta que se crea automáticamente (se leen de appsettings.json).</summary>
public class UsuarioSemilla
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
}

/// <summary>Aplica migraciones pendientes y crea los datos iniciales si no existen.</summary>
public static class DbInitializer
{
    public static async Task InicializarAsync(
        ApplicationDbContext context,
        UserManager<Usuario> userManager,
        string adminEmail,
        string adminPassword,
        string adminNombre)
    {
        await context.Database.MigrateAsync();

        await CrearUsuarioSiNoExisteAsync(userManager,
            new UsuarioSemilla { Email = adminEmail, Password = adminPassword, NombreCompleto = adminNombre },
            Roles.Administrador);
    }

    /// <summary>
    /// Crea un Agente, un Candidato, plazas de ejemplo y una postulación para poder probar el sistema.
    /// Las plazas solo se crean si la tabla está vacía.
    /// </summary>
    public static async Task SembrarDatosDePruebaAsync(
        ApplicationDbContext context,
        UserManager<Usuario> userManager,
        UsuarioSemilla agente,
        UsuarioSemilla candidato)
    {
        var usuarioAgente = await CrearUsuarioSiNoExisteAsync(userManager, agente, Roles.AgenteSeleccion);
        var usuarioCandidato = await CrearUsuarioSiNoExisteAsync(userManager, candidato, Roles.Candidato);

        if (await context.Plazas.AnyAsync())
            return;

        var hoy = DateTime.Today;
        var plazas = new List<Plaza>
        {
            new()
            {
                Titulo = "Analista de Sistemas",
                Descripcion = "Desarrollo y mantenimiento de sistemas institucionales. Requisitos: Ingeniería en Sistemas o carrera afín, 2 años de experiencia en .NET y SQL Server.",
                Institucion = "Ministerio de Hacienda",
                FechaPublicacion = hoy.AddDays(-5), FechaCierre = hoy.AddDays(25),
                Estado = EstadoPlaza.Abierta, Publicada = true, IdAgente = usuarioAgente.Id
            },
            new()
            {
                Titulo = "Enfermero(a) de Atención Primaria",
                Descripcion = "Atención de pacientes en unidad de salud comunitaria. Requisitos: Licenciatura en Enfermería e inscripción vigente en la junta de vigilancia.",
                Institucion = "Ministerio de Salud",
                FechaPublicacion = hoy.AddDays(-2), FechaCierre = hoy.AddDays(30),
                Estado = EstadoPlaza.Abierta, Publicada = true, IdAgente = usuarioAgente.Id
            },
            new()
            {
                Titulo = "Docente de Matemática - Tercer Ciclo",
                Descripcion = "Impartir la asignatura de Matemática en 7.º, 8.º y 9.º grado. Requisitos: Profesorado o Licenciatura en Matemática y escalafón docente.",
                Institucion = "Ministerio de Educación",
                FechaPublicacion = hoy.AddDays(-10), FechaCierre = hoy.AddDays(15),
                Estado = EstadoPlaza.Abierta, Publicada = true, IdAgente = usuarioAgente.Id
            },
            new()
            {
                Titulo = "Técnico en Gestión Documental",
                Descripcion = "Organización y digitalización del archivo institucional. (Borrador: aún no publicada)",
                Institucion = "Registro Nacional de las Personas Naturales",
                FechaPublicacion = hoy, FechaCierre = hoy.AddDays(20),
                Estado = EstadoPlaza.Abierta, Publicada = false, IdAgente = usuarioAgente.Id
            },
            new()
            {
                Titulo = "Asistente Administrativo",
                Descripcion = "Apoyo administrativo a la gerencia. El período de postulación terminó y la plaza está en evaluación.",
                Institucion = "Alcaldía Municipal",
                FechaPublicacion = hoy.AddDays(-40), FechaCierre = hoy.AddDays(-5),
                Estado = EstadoPlaza.EnEvaluacion, Publicada = true, IdAgente = usuarioAgente.Id
            }
        };
        context.Plazas.AddRange(plazas);
        await context.SaveChangesAsync();

        // Postulación de ejemplo para que el Agente pueda probar la evaluación.
        context.Postulaciones.Add(new Postulacion
        {
            IdPlaza = plazas[^1].IdPlaza,
            IdCandidato = usuarioCandidato.Id,
            FechaPostulacion = hoy.AddDays(-20),
            Estado = EstadoPostulacion.EnRevision
        });
        await context.SaveChangesAsync();
    }

    private static async Task<Usuario> CrearUsuarioSiNoExisteAsync(
        UserManager<Usuario> userManager, UsuarioSemilla datos, string rol)
    {
        var existente = await userManager.FindByEmailAsync(datos.Email);
        if (existente is not null)
            return existente;

        var usuario = new Usuario
        {
            UserName = datos.Email,
            Email = datos.Email,
            EmailConfirmed = true,
            NombreCompleto = datos.NombreCompleto,
            Rol = rol
        };

        var resultado = await userManager.CreateAsync(usuario, datos.Password);
        if (!resultado.Succeeded)
            throw new InvalidOperationException(
                $"No se pudo crear el usuario {datos.Email}: " + string.Join("; ", resultado.Errors.Select(e => e.Description)));

        await userManager.AddToRoleAsync(usuario, rol);
        return usuario;
    }
}
