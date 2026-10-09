using BolsaTrabajo.DAL.Data;
using BolsaTrabajo.Entities;
using BolsaTrabajo.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace BolsaTrabajo.Tests.Infraestructura;

/// <summary>
/// Comprueba la configuración de la capa de datos (EF Core): roles iniciales, valores por defecto
/// y las reglas de integridad que se crean en SQL Server con las migraciones.
/// </summary>
public class ContextoDatosTests
{
    private static ApplicationDbContext CrearContexto() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    // ---------- 5. Los roles del sistema se crean correctamente ----------

    [Fact]
    public async Task Contexto_CreaLosTresRolesDelSistema()
    {
        using var contexto = CrearContexto();
        await contexto.Database.EnsureCreatedAsync();

        var roles = await contexto.Roles.Select(r => r.Name).ToListAsync();

        Assert.Equal(3, roles.Count);
        Assert.Equal(Roles.Todos.OrderBy(r => r), roles.OrderBy(r => r));
    }

    [Fact]
    public async Task RolesIniciales_TienenNombreNormalizadoParaIdentity()
    {
        using var contexto = CrearContexto();
        await contexto.Database.EnsureCreatedAsync();

        var agente = await contexto.Roles.SingleAsync(r => r.Name == Roles.AgenteSeleccion);

        Assert.Equal("AGENTE DE SELECCIÓN", agente.NormalizedName);
    }

    // ---------- 6. Valores por defecto ----------

    [Fact]
    public async Task PlazaGuardada_SinIndicarEstado_QuedaAbierta()
    {
        using var contexto = CrearContexto();
        var agente = new Usuario { UserName = "agente@test.com", NombreCompleto = "Agente Prueba", Rol = Roles.AgenteSeleccion };
        contexto.Users.Add(agente);
        contexto.Plazas.Add(new Plaza
        {
            Titulo = "Técnico administrativo",
            Descripcion = "Apoyo en gestión documental",
            Institucion = "Ministerio de Prueba",
            FechaPublicacion = DateTime.Today,
            FechaCierre = DateTime.Today.AddDays(10),
            IdAgente = agente.Id
        });
        await contexto.SaveChangesAsync();

        var plaza = await contexto.Plazas.SingleAsync();
        Assert.Equal(EstadoPlaza.Abierta, plaza.Estado);
        Assert.False(plaza.Publicada);
    }

    [Fact]
    public void PostulacionNueva_QuedaEnRevision()
    {
        Assert.Equal(EstadoPostulacion.EnRevision, new Postulacion().Estado);
    }

    // ---------- Integridad que las migraciones crean en SQL Server ----------

    [Fact]
    public void Postulaciones_TienenIndiceUnicoPorPlazaYCandidato()
    {
        using var contexto = CrearContexto();
        var entidad = contexto.Model.FindEntityType(typeof(Postulacion))!;

        var indice = entidad.GetIndexes().Single(i =>
            i.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(Postulacion.IdPlaza), nameof(Postulacion.IdCandidato) }));

        Assert.True(indice.IsUnique);
    }

    [Fact]
    public void HojaDeVida_EsUnaPorUsuario()
    {
        using var contexto = CrearContexto();
        var indice = contexto.Model.FindEntityType(typeof(HojaDeVida))!.GetIndexes()
            .Single(i => i.Properties.Single().Name == nameof(HojaDeVida.IdUsuario));

        Assert.True(indice.IsUnique);
    }

    [Theory]
    [InlineData(typeof(Plaza), nameof(Plaza.IdAgente), DeleteBehavior.Restrict)]
    [InlineData(typeof(Postulacion), nameof(Postulacion.IdPlaza), DeleteBehavior.Restrict)]
    [InlineData(typeof(Postulacion), nameof(Postulacion.IdCandidato), DeleteBehavior.Restrict)]
    [InlineData(typeof(HojaDeVida), nameof(HojaDeVida.IdUsuario), DeleteBehavior.Cascade)]
    public void LlavesForaneas_TienenElComportamientoAlBorrarEsperado(Type entidad, string columna, DeleteBehavior esperado)
    {
        using var contexto = CrearContexto();
        var llave = contexto.Model.FindEntityType(entidad)!.GetForeignKeys()
            .Single(fk => fk.Properties.Single().Name == columna);

        Assert.Equal(esperado, llave.DeleteBehavior);
    }

    /// <summary>
    /// Genera el script SQL que EF Core ejecuta en SQL Server (sin conectarse a ninguna base)
    /// y comprueba que incluye las restricciones de integridad.
    /// </summary>
    [Theory]
    [InlineData("CONSTRAINT [CK_Plaza_Fechas] CHECK ([FechaCierre] > [FechaPublicacion])")]
    [InlineData("CONSTRAINT [CK_Plazas_Estado] CHECK ([Estado] IN (N'Abierta', N'EnEvaluacion', N'Cerrada'))")]
    [InlineData("CONSTRAINT [CK_Postulaciones_Estado] CHECK ([Estado] IN (N'EnRevision', N'Aprobada', N'Rechazada'))")]
    [InlineData("CONSTRAINT [CK_AspNetUsers_Rol] CHECK ([Rol] IN (N'Administrador', N'Agente de Selección', N'Candidato'))")]
    [InlineData("CREATE UNIQUE INDEX [IX_Postulaciones_IdPlaza_IdCandidato] ON [Postulaciones] ([IdPlaza], [IdCandidato])")]
    [InlineData("FOREIGN KEY ([IdPlaza]) REFERENCES [Plazas] ([IdPlaza]) ON DELETE NO ACTION")]
    public void ScriptDeSqlServer_IncluyeLasRestriccionesDeIntegridad(string sqlEsperado)
    {
        using var contexto = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=sin-conexion;Database=SoloParaGenerarScript")
            .Options);

        var script = contexto.Database.GenerateCreateScript();

        Assert.Contains(sqlEsperado, script);
    }
}
