using BolsaTrabajo.BLL.Excepciones;
using BolsaTrabajo.BLL.Servicios;
using BolsaTrabajo.DTOs.Plazas;
using BolsaTrabajo.Entities.Enums;
using BolsaTrabajo.Tests.Fakes;

namespace BolsaTrabajo.Tests.Servicios;

/// <summary>Reglas de negocio de las plazas (PlazaService de la BLL).</summary>
public class PlazaServiceTests
{
    private static GuardarPlazaDto PlazaValida(int diasPublicacion = 0, int diasCierre = 15) => new()
    {
        Titulo = "Analista de Sistemas",
        Descripcion = "Desarrollo de sistemas institucionales",
        Institucion = "Ministerio de Hacienda",
        FechaPublicacion = DateTime.Today.AddDays(diasPublicacion),
        FechaCierre = DateTime.Today.AddDays(diasCierre)
    };

    // ---------- 1. Crear una plaza válida aumenta el número de registros ----------

    [Fact]
    public async Task CrearPlazaValida_AumentaElNumeroDeRegistros()
    {
        var e = new Escenario();
        var antes = e.Db.Plazas.Count;

        var creada = await e.Plazas.CrearAsync(PlazaValida(), e.Agente.Id);

        Assert.Equal(antes + 1, e.Db.Plazas.Count);
        Assert.Contains(e.Db.Plazas, p => p.IdPlaza == creada.IdPlaza && p.Titulo == "Analista de Sistemas");
    }

    [Fact]
    public async Task CrearVariasPlazas_CadaUnaSumaUnRegistro()
    {
        var e = new Escenario();

        await e.Plazas.CrearAsync(PlazaValida(), e.Agente.Id);
        await e.Plazas.CrearAsync(PlazaValida(), e.Agente.Id);
        await e.Plazas.CrearAsync(PlazaValida(), e.OtroAgente.Id);

        Assert.Equal(3, e.Db.Plazas.Count);
        Assert.Equal(2, (await e.Plazas.ListarAsync(e.ComoAgente())).Count);
    }

    // ---------- 6. Una plaza nueva tiene el estado esperado por defecto ----------

    [Fact]
    public async Task PlazaNueva_QuedaAbiertaSinPublicarYAsignadaAlAgente()
    {
        var e = new Escenario();

        var creada = await e.Plazas.CrearAsync(PlazaValida(), e.Agente.Id);

        Assert.Equal(EstadoPlaza.Abierta, creada.Estado);
        Assert.False(creada.Publicada);
        Assert.Equal(e.Agente.Id, creada.IdAgente);
        Assert.Equal("Agente Uno", creada.NombreAgente);
        Assert.Equal(0, creada.TotalPostulaciones);
    }

    [Fact]
    public async Task PlazaNueva_NoApareceEnDisponiblesHastaPublicarla()
    {
        var e = new Escenario();
        var creada = await e.Plazas.CrearAsync(PlazaValida(), e.Agente.Id);

        Assert.DoesNotContain(await e.Plazas.ListarDisponiblesAsync(), p => p.IdPlaza == creada.IdPlaza);

        await e.Plazas.PublicarAsync(creada.IdPlaza, e.Agente.Id);

        Assert.Contains(await e.Plazas.ListarDisponiblesAsync(), p => p.IdPlaza == creada.IdPlaza);
    }

    // ---------- 4. FechaCierre debe ser posterior a FechaPublicacion ----------

    [Theory]
    [InlineData(5, 1)]   // cierre antes de la publicación
    [InlineData(3, 3)]   // cierre el mismo día que la publicación
    [InlineData(10, 0)]
    public void ValidarFechas_CierreNoPosteriorAPublicacion_LanzaError(int diasPublicacion, int diasCierre)
    {
        var ex = Assert.Throws<ReglaNegocioException>(() =>
            PlazaService.ValidarFechas(DateTime.Today.AddDays(diasPublicacion), DateTime.Today.AddDays(diasCierre)));

        Assert.Equal("La fecha de cierre debe ser posterior a la fecha de publicación.", ex.Message);
    }

    [Fact]
    public void ValidarFechas_CierreEnElPasado_LanzaError()
    {
        var ex = Assert.Throws<ReglaNegocioException>(() =>
            PlazaService.ValidarFechas(DateTime.Today.AddDays(-20), DateTime.Today.AddDays(-1)));

        Assert.Equal("La fecha de cierre no puede estar en el pasado.", ex.Message);
    }

    [Fact]
    public void ValidarFechas_FechasCorrectas_NoLanzaError()
    {
        var error = Record.Exception(() => PlazaService.ValidarFechas(DateTime.Today, DateTime.Today.AddDays(1)));
        Assert.Null(error);
    }

    [Fact]
    public async Task CrearPlazaConFechasInvalidas_NoGuardaNingunRegistro()
    {
        var e = new Escenario();

        await Assert.ThrowsAsync<ReglaNegocioException>(() =>
            e.Plazas.CrearAsync(PlazaValida(diasPublicacion: 10, diasCierre: 2), e.Agente.Id));

        Assert.Empty(e.Db.Plazas);
    }

    [Fact]
    public async Task EditarPlazaConFechasInvalidas_NoCambiaLaPlaza()
    {
        var e = new Escenario();
        var creada = await e.Plazas.CrearAsync(PlazaValida(), e.Agente.Id);
        var invalida = PlazaValida(diasPublicacion: 8, diasCierre: 4);
        invalida.Titulo = "Título cambiado";

        await Assert.ThrowsAsync<ReglaNegocioException>(() => e.Plazas.ActualizarAsync(creada.IdPlaza, invalida, e.Agente.Id));

        Assert.Equal("Analista de Sistemas", e.Db.Plazas.Single().Titulo);
    }

    // ---------- 7. Otras validaciones de negocio y permisos ----------

    [Fact]
    public async Task CrearPlazaSinAgenteAutenticado_LanzaNoAutenticado()
    {
        var e = new Escenario();

        await Assert.ThrowsAsync<NoAutenticadoException>(() => e.Plazas.CrearAsync(PlazaValida(), idAgente: null));
        Assert.Empty(e.Db.Plazas);
    }

    [Fact]
    public async Task EditarPlazaDeOtroAgente_LanzaAccesoDenegado()
    {
        var e = new Escenario();
        var creada = await e.Plazas.CrearAsync(PlazaValida(), e.Agente.Id);

        await Assert.ThrowsAsync<AccesoDenegadoException>(() =>
            e.Plazas.ActualizarAsync(creada.IdPlaza, PlazaValida(), e.OtroAgente.Id));
    }

    [Fact]
    public async Task EditarPlazaPropia_GuardaLosCambios()
    {
        var e = new Escenario();
        var creada = await e.Plazas.CrearAsync(PlazaValida(), e.Agente.Id);
        var cambios = PlazaValida(diasCierre: 30);
        cambios.Titulo = "Analista de Sistemas Senior";

        var editada = await e.Plazas.ActualizarAsync(creada.IdPlaza, cambios, e.Agente.Id);

        Assert.Equal("Analista de Sistemas Senior", editada.Titulo);
        Assert.Equal("Analista de Sistemas Senior", e.Db.Plazas.Single().Titulo);
    }

    [Fact]
    public async Task PublicarDosVeces_LanzaError()
    {
        var e = new Escenario();
        var creada = await e.Plazas.CrearAsync(PlazaValida(), e.Agente.Id);
        await e.Plazas.PublicarAsync(creada.IdPlaza, e.Agente.Id);

        var ex = await Assert.ThrowsAsync<ReglaNegocioException>(() => e.Plazas.PublicarAsync(creada.IdPlaza, e.Agente.Id));
        Assert.Equal("La plaza ya está publicada.", ex.Message);
    }

    [Fact]
    public async Task PlazaCerrada_NoPuedeReabrirse()
    {
        var e = new Escenario();
        var plaza = e.Db.AgregarPlaza(e.Agente.Id, estado: EstadoPlaza.Cerrada);

        await Assert.ThrowsAsync<ReglaNegocioException>(() =>
            e.Plazas.CambiarEstadoAsync(plaza.IdPlaza, EstadoPlaza.Abierta, e.ComoAgente()));
        Assert.Equal(EstadoPlaza.Cerrada, e.Db.Plazas.Single().Estado);
    }

    [Fact]
    public async Task CambiarEstado_AgenteDueno_YAdministrador_Pueden_OtroAgenteNo()
    {
        var e = new Escenario();
        var plaza = e.Db.AgregarPlaza(e.Agente.Id);

        await Assert.ThrowsAsync<AccesoDenegadoException>(() =>
            e.Plazas.CambiarEstadoAsync(plaza.IdPlaza, EstadoPlaza.EnEvaluacion, e.ComoOtroAgente()));

        await e.Plazas.CambiarEstadoAsync(plaza.IdPlaza, EstadoPlaza.EnEvaluacion, e.ComoAgente());
        Assert.Equal(EstadoPlaza.EnEvaluacion, e.Db.Plazas.Single().Estado);

        await e.Plazas.CambiarEstadoAsync(plaza.IdPlaza, EstadoPlaza.Cerrada, Escenario.ComoAdministrador());
        Assert.Equal(EstadoPlaza.Cerrada, e.Db.Plazas.Single().Estado);
    }

    [Fact]
    public async Task EliminarPlazaConPostulaciones_LanzaError_YNoLaBorra()
    {
        var e = new Escenario();
        var plaza = e.Db.AgregarPlaza(e.Agente.Id);
        await e.Postulaciones.PostularAsync(plaza.IdPlaza, e.Candidato.Id);

        await Assert.ThrowsAsync<ReglaNegocioException>(() => e.Plazas.EliminarAsync(plaza.IdPlaza, e.Agente.Id));
        Assert.Single(e.Db.Plazas);
    }

    [Fact]
    public async Task EliminarPlazaSinPostulaciones_LaBorra()
    {
        var e = new Escenario();
        var creada = await e.Plazas.CrearAsync(PlazaValida(), e.Agente.Id);

        await e.Plazas.EliminarAsync(creada.IdPlaza, e.Agente.Id);

        Assert.Empty(e.Db.Plazas);
    }

    [Fact]
    public async Task PlazaBorrador_NoLaVeUnCandidato_PeroSiSuAgente()
    {
        var e = new Escenario();
        var borrador = e.Db.AgregarPlaza(e.Agente.Id, publicada: false);

        await Assert.ThrowsAsync<NoEncontradoException>(() => e.Plazas.ObtenerAsync(borrador.IdPlaza, e.ComoCandidato()));
        await Assert.ThrowsAsync<NoEncontradoException>(() => e.Plazas.ObtenerAsync(borrador.IdPlaza, usuario: null));
        Assert.Equal(borrador.IdPlaza, (await e.Plazas.ObtenerAsync(borrador.IdPlaza, e.ComoAgente())).IdPlaza);
    }
}
