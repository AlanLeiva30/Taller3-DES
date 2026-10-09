using BolsaTrabajo.Entities;
using BolsaTrabajo.Entities.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
// Alias: "Roles" a secas se confunde con la tabla Roles que hereda IdentityDbContext.
using RolesSistema = BolsaTrabajo.Entities.Roles;

namespace BolsaTrabajo.DAL.Data;

/// <summary>Contexto de Entity Framework: incluye las tablas de Identity y las del negocio.</summary>
public class ApplicationDbContext : IdentityDbContext<Usuario>
{
    // Ids fijos para que las migraciones no cambien en cada ejecución.
    public const string RolAdministradorId = "6b1c1f8e-0b1e-4f6a-9a3e-1a0000000001";
    public const string RolAgenteId = "6b1c1f8e-0b1e-4f6a-9a3e-1a0000000002";
    public const string RolCandidatoId = "6b1c1f8e-0b1e-4f6a-9a3e-1a0000000003";

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Plaza> Plazas => Set<Plaza>();
    public DbSet<Postulacion> Postulaciones => Set<Postulacion>();
    public DbSet<HojaDeVida> HojasDeVida => Set<HojaDeVida>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Usuario>(e =>
        {
            e.Property(u => u.NombreCompleto).HasMaxLength(150).IsRequired();
            e.Property(u => u.Rol).HasMaxLength(50).IsRequired();
            e.ToTable(t => t.HasCheckConstraint("CK_AspNetUsers_Rol", $"[Rol] IN ({ListaSql(RolesSistema.Todos)})"));
        });

        builder.Entity<Plaza>(e =>
        {
            e.ToTable("Plazas");
            e.HasKey(p => p.IdPlaza);
            e.Property(p => p.Titulo).HasMaxLength(150).IsRequired();
            e.Property(p => p.Descripcion).HasMaxLength(4000).IsRequired();
            e.Property(p => p.Institucion).HasMaxLength(150).IsRequired();
            e.Property(p => p.Estado).HasConversion<string>().HasMaxLength(20);
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Plaza_Fechas", "[FechaCierre] > [FechaPublicacion]");
                t.HasCheckConstraint("CK_Plazas_Estado", $"[Estado] IN ({ListaSql(Enum.GetNames<EstadoPlaza>())})");
            });

            // Índice para la consulta de plazas disponibles (publicadas, abiertas y vigentes).
            e.HasIndex(p => new { p.Publicada, p.Estado, p.FechaCierre }).HasDatabaseName("IX_Plazas_Disponibles");

            e.HasOne(p => p.Agente)
             .WithMany(u => u.PlazasCreadas)
             .HasForeignKey(p => p.IdAgente)
             .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Postulacion>(e =>
        {
            e.ToTable("Postulaciones");
            e.HasKey(p => p.IdPostulacion);
            e.Property(p => p.Estado).HasConversion<string>().HasMaxLength(20);
            e.ToTable(t => t.HasCheckConstraint(
                "CK_Postulaciones_Estado", $"[Estado] IN ({ListaSql(Enum.GetNames<EstadoPostulacion>())})"));

            // Un candidato no puede postularse dos veces a la misma plaza.
            e.HasIndex(p => new { p.IdPlaza, p.IdCandidato }).IsUnique();

            // RESTRICT: una plaza con postulaciones no puede borrarse (regla de negocio).
            e.HasOne(p => p.Plaza)
             .WithMany(pl => pl.Postulaciones)
             .HasForeignKey(p => p.IdPlaza)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(p => p.Candidato)
             .WithMany(u => u.Postulaciones)
             .HasForeignKey(p => p.IdCandidato)
             .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<HojaDeVida>(e =>
        {
            e.ToTable("HojasDeVida");
            e.HasKey(h => h.IdCV);
            e.Property(h => h.FormacionAcademica).HasMaxLength(4000);
            e.Property(h => h.ExperienciaLaboral).HasMaxLength(4000);
            e.Property(h => h.Competencias).HasMaxLength(2000);
            e.Property(h => h.ArchivoCV).HasMaxLength(500);
            e.Property(h => h.NombreArchivoCV).HasMaxLength(255);

            e.HasIndex(h => h.IdUsuario).IsUnique();
            e.HasOne(h => h.Usuario)
             .WithOne(u => u.HojaDeVida)
             .HasForeignKey<HojaDeVida>(h => h.IdUsuario)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<IdentityRole>().HasData(
            new IdentityRole { Id = RolAdministradorId, Name = RolesSistema.Administrador, NormalizedName = RolesSistema.Administrador.ToUpperInvariant(), ConcurrencyStamp = RolAdministradorId },
            new IdentityRole { Id = RolAgenteId, Name = RolesSistema.AgenteSeleccion, NormalizedName = RolesSistema.AgenteSeleccion.ToUpperInvariant(), ConcurrencyStamp = RolAgenteId },
            new IdentityRole { Id = RolCandidatoId, Name = RolesSistema.Candidato, NormalizedName = RolesSistema.Candidato.ToUpperInvariant(), ConcurrencyStamp = RolCandidatoId });
    }

    /// <summary>Convierte valores en una lista SQL: N'Valor1', N'Valor2'.</summary>
    private static string ListaSql(IEnumerable<string> valores) =>
        string.Join(", ", valores.Select(v => $"N'{v}'"));
}
