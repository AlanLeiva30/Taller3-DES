using BolsaTrabajo.BLL.Excepciones;
using BolsaTrabajo.Entities.Enums;
using BolsaTrabajo.Tests.Fakes;

namespace BolsaTrabajo.Tests.Servicios;

/// <summary>Reglas de negocio de las postulaciones (PostulacionService de la BLL).</summary>
public class PostulacionServiceTests
{
    // ---------- 2. Una postulación sin usuario autenticado genera error ----------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task PostularSinUsuarioAutenticado_LanzaNoAutenticado_YNoGuarda(string? idCandidato)
    {
        var e = new Escenario();
        var plaza = e.Db.AgregarPlaza(e.Agente.Id);

        var ex = await Assert.ThrowsAsync<NoAutenticadoException>(() => e.Postulaciones.PostularAsync(plaza.IdPlaza, idCandidato));

        Assert.Equal("Debe iniciar sesión para postularse a una plaza.", ex.Message);
        Assert.Empty(e.Db.Postulaciones);
    }

    // ---------- 3. Un candidato no puede postularse dos veces a la misma plaza ----------

    [Fact]
    public async Task PostularDosVecesALaMismaPlaza_LanzaError_YSoloGuardaUna()
    {
        var e = new Escenario();
        var plaza = e.Db.AgregarPlaza(e.Agente.Id);

        await e.Postulaciones.PostularAsync(plaza.IdPlaza, e.Candidato.Id);
        var ex = await Assert.ThrowsAsync<ReglaNegocioException>(() => e.Postulaciones.PostularAsync(plaza.IdPlaza, e.Candidato.Id));

        Assert.Equal("Usted ya se postuló a esta plaza.", ex.Message);
        Assert.Single(e.Db.Postulaciones);
    }

    [Fact]
    public async Task MismoCandidato_PuedePostularseAPlazasDistintas()
    {
        var e = new Escenario();
        var plaza1 = e.Db.AgregarPlaza(e.Agente.Id);
        var plaza2 = e.Db.AgregarPlaza(e.Agente.Id);

        await e.Postulaciones.PostularAsync(plaza1.IdPlaza, e.Candidato.Id);
        await e.Postulaciones.PostularAsync(plaza2.IdPlaza, e.Candidato.Id);

        Assert.Equal(2, e.Db.Postulaciones.Count);
    }

    [Fact]
    public async Task PostulacionValida_QuedaEnRevision()
    {
        var e = new Escenario();
        var plaza = e.Db.AgregarPlaza(e.Agente.Id);

        var postulacion = await e.Postulaciones.PostularAsync(plaza.IdPlaza, e.Candidato.Id);

        Assert.Equal(EstadoPostulacion.EnRevision, postulacion.Estado);
        Assert.Equal("Candidato Uno", postulacion.NombreCandidato);
        Assert.Equal(EstadoPostulacion.EnRevision, e.Db.Postulaciones.Single().Estado);
    }

    // ---------- 7. Validaciones de relaciones y estados ----------

    [Fact]
    public async Task PostularAPlazaInexistente_LanzaNoEncontrado()
    {
        var e = new Escenario();
        await Assert.ThrowsAsync<NoEncontradoException>(() => e.Postulaciones.PostularAsync(999, e.Candidato.Id));
    }

    [Fact]
    public async Task PostularAPlazaNoPublicada_LanzaError()
    {
        var e = new Escenario();
        var borrador = e.Db.AgregarPlaza(e.Agente.Id, publicada: false);

        var ex = await Assert.ThrowsAsync<ReglaNegocioException>(() => e.Postulaciones.PostularAsync(borrador.IdPlaza, e.Candidato.Id));
        Assert.Equal("La plaza todavía no ha sido publicada.", ex.Message);
    }

    [Theory]
    [InlineData(EstadoPlaza.EnEvaluacion)]
    [InlineData(EstadoPlaza.Cerrada)]
    public async Task PostularAPlazaQueNoEstaAbierta_LanzaError(EstadoPlaza estado)
    {
        var e = new Escenario();
        var plaza = e.Db.AgregarPlaza(e.Agente.Id, estado: estado);

        await Assert.ThrowsAsync<ReglaNegocioException>(() => e.Postulaciones.PostularAsync(plaza.IdPlaza, e.Candidato.Id));
        Assert.Empty(e.Db.Postulaciones);
    }

    [Fact]
    public async Task PostularDespuesDeLaFechaDeCierre_LanzaError()
    {
        var e = new Escenario();
        var vencida = e.Db.AgregarPlaza(e.Agente.Id, diasDesdePublicacion: -30, diasHastaCierre: -1);

        var ex = await Assert.ThrowsAsync<ReglaNegocioException>(() => e.Postulaciones.PostularAsync(vencida.IdPlaza, e.Candidato.Id));
        Assert.Equal("El período de postulación para esta plaza ya finalizó.", ex.Message);
    }

    [Fact]
    public async Task EvaluarPostulacion_AgenteDueno_GuardaElResultado()
    {
        var e = new Escenario();
        var plaza = e.Db.AgregarPlaza(e.Agente.Id);
        var postulacion = await e.Postulaciones.PostularAsync(plaza.IdPlaza, e.Candidato.Id);

        var evaluada = await e.Postulaciones.EvaluarAsync(postulacion.IdPostulacion, EstadoPostulacion.Aprobada, e.Agente.Id);

        Assert.Equal(EstadoPostulacion.Aprobada, evaluada.Estado);
        Assert.Equal(EstadoPostulacion.Aprobada, e.Db.Postulaciones.Single().Estado);
    }

    [Fact]
    public async Task EvaluarPostulacionDePlazaAjena_LanzaAccesoDenegado()
    {
        var e = new Escenario();
        var plaza = e.Db.AgregarPlaza(e.Agente.Id);
        var postulacion = await e.Postulaciones.PostularAsync(plaza.IdPlaza, e.Candidato.Id);

        await Assert.ThrowsAsync<AccesoDenegadoException>(() =>
            e.Postulaciones.EvaluarAsync(postulacion.IdPostulacion, EstadoPostulacion.Rechazada, e.OtroAgente.Id));
        Assert.Equal(EstadoPostulacion.EnRevision, e.Db.Postulaciones.Single().Estado);
    }

    [Fact]
    public async Task EvaluarConEstadoEnRevision_LanzaError()
    {
        var e = new Escenario();
        var plaza = e.Db.AgregarPlaza(e.Agente.Id);
        var postulacion = await e.Postulaciones.PostularAsync(plaza.IdPlaza, e.Candidato.Id);

        await Assert.ThrowsAsync<ReglaNegocioException>(() =>
            e.Postulaciones.EvaluarAsync(postulacion.IdPostulacion, EstadoPostulacion.EnRevision, e.Agente.Id));
    }

    [Fact]
    public async Task EvaluarEnPlazaCerrada_LanzaError()
    {
        var e = new Escenario();
        var plaza = e.Db.AgregarPlaza(e.Agente.Id);
        var postulacion = await e.Postulaciones.PostularAsync(plaza.IdPlaza, e.Candidato.Id);
        await e.Plazas.CambiarEstadoAsync(plaza.IdPlaza, EstadoPlaza.Cerrada, e.ComoAgente());

        await Assert.ThrowsAsync<ReglaNegocioException>(() =>
            e.Postulaciones.EvaluarAsync(postulacion.IdPostulacion, EstadoPostulacion.Aprobada, e.Agente.Id));
    }

    [Fact]
    public async Task VerPostulacionesDePlazaAjena_LanzaAccesoDenegado()
    {
        var e = new Escenario();
        var plaza = e.Db.AgregarPlaza(e.Agente.Id);

        await Assert.ThrowsAsync<AccesoDenegadoException>(() => e.Postulaciones.ListarPorPlazaAsync(plaza.IdPlaza, e.ComoOtroAgente()));
    }

    [Fact]
    public async Task RetirarPostulacionEnRevision_LaElimina_PeroNoSiYaFueEvaluada()
    {
        var e = new Escenario();
        var plaza1 = e.Db.AgregarPlaza(e.Agente.Id);
        var plaza2 = e.Db.AgregarPlaza(e.Agente.Id);
        var enRevision = await e.Postulaciones.PostularAsync(plaza1.IdPlaza, e.Candidato.Id);
        var evaluada = await e.Postulaciones.PostularAsync(plaza2.IdPlaza, e.Candidato.Id);
        await e.Postulaciones.EvaluarAsync(evaluada.IdPostulacion, EstadoPostulacion.Aprobada, e.Agente.Id);

        await e.Postulaciones.RetirarAsync(enRevision.IdPostulacion, e.Candidato.Id);
        await Assert.ThrowsAsync<ReglaNegocioException>(() => e.Postulaciones.RetirarAsync(evaluada.IdPostulacion, e.Candidato.Id));

        Assert.Single(e.Db.Postulaciones);
    }
}
