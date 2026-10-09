using System.Text;
using BolsaTrabajo.BLL.Excepciones;
using BolsaTrabajo.BLL.Servicios;
using BolsaTrabajo.DTOs.HojasDeVida;
using BolsaTrabajo.Tests.Fakes;

namespace BolsaTrabajo.Tests.Servicios;

/// <summary>Reglas de negocio de la hoja de vida y del CV en PDF (HojaDeVidaService de la BLL).</summary>
public class HojaDeVidaServiceTests
{
    private static readonly byte[] PdfValido = Encoding.ASCII.GetBytes("%PDF-1.4\n1 0 obj<<>>endobj\ntrailer<<>>\n%%EOF");

    private static GuardarHojaDeVidaDto Hoja() => new()
    {
        FormacionAcademica = "Ingeniería en Sistemas",
        ExperienciaLaboral = "2 años",
        Competencias = "C#, SQL"
    };

    private static Task SubirAsync(Escenario e, string nombre, byte[] datos) =>
        e.HojasDeVida.SubirArchivoAsync(e.Candidato.Id, nombre, new MemoryStream(datos), datos.Length);

    [Fact]
    public async Task RegistrarHojaDeVida_AumentaRegistros_YNoPermiteDuplicarla()
    {
        var e = new Escenario();

        await e.HojasDeVida.CrearAsync(e.Candidato.Id, Hoja());
        await Assert.ThrowsAsync<ReglaNegocioException>(() => e.HojasDeVida.CrearAsync(e.Candidato.Id, Hoja()));

        Assert.Single(e.Db.HojasDeVida);
    }

    [Fact]
    public async Task HojaDeVidaSinFormacionAcademica_LanzaError()
    {
        var e = new Escenario();
        var sinFormacion = Hoja();
        sinFormacion.FormacionAcademica = "  ";

        await Assert.ThrowsAsync<ReglaNegocioException>(() => e.HojasDeVida.CrearAsync(e.Candidato.Id, sinFormacion));
        Assert.Empty(e.Db.HojasDeVida);
    }

    [Fact]
    public async Task SubirCvSinHojaDeVida_LanzaError()
    {
        var e = new Escenario();
        await Assert.ThrowsAsync<ReglaNegocioException>(() => SubirAsync(e, "cv.pdf", PdfValido));
        Assert.Empty(e.Almacen.Archivos);
    }

    [Theory]
    [InlineData("cv.docx")]
    [InlineData("cv.exe")]
    [InlineData("cv")]
    public async Task SubirCvConExtensionDistintaDePdf_LanzaError(string nombre)
    {
        var e = new Escenario();
        await e.HojasDeVida.CrearAsync(e.Candidato.Id, Hoja());

        var ex = await Assert.ThrowsAsync<ReglaNegocioException>(() => SubirAsync(e, nombre, PdfValido));
        Assert.Equal("Solo se permiten archivos PDF.", ex.Message);
        Assert.Empty(e.Almacen.Archivos);
    }

    [Fact]
    public async Task SubirArchivoFalsoConExtensionPdf_LanzaError()
    {
        var e = new Escenario();
        await e.HojasDeVida.CrearAsync(e.Candidato.Id, Hoja());

        var ex = await Assert.ThrowsAsync<ReglaNegocioException>(() => SubirAsync(e, "cv.pdf", Encoding.ASCII.GetBytes("no soy un pdf")));
        Assert.Equal("El archivo no es un PDF válido.", ex.Message);
    }

    [Fact]
    public async Task SubirPdfValido_GuardaElArchivoYSuNombreOriginal()
    {
        var e = new Escenario();
        await e.HojasDeVida.CrearAsync(e.Candidato.Id, Hoja());

        await SubirAsync(e, "Mi CV.pdf", PdfValido);
        var hoja = await e.HojasDeVida.ObtenerMiaAsync(e.Candidato.Id);

        Assert.True(hoja.TieneArchivoCV);
        Assert.Equal("Mi CV.pdf", hoja.NombreArchivoCV);
        Assert.Single(e.Almacen.Archivos);
    }

    [Fact]
    public async Task ReemplazarCv_BorraElArchivoAnterior()
    {
        var e = new Escenario();
        await e.HojasDeVida.CrearAsync(e.Candidato.Id, Hoja());

        await SubirAsync(e, "primero.pdf", PdfValido);
        await SubirAsync(e, "segundo.pdf", PdfValido);

        Assert.Single(e.Almacen.Archivos);
        Assert.Equal("segundo.pdf", (await e.HojasDeVida.ObtenerMiaAsync(e.Candidato.Id)).NombreArchivoCV);
    }

    [Fact]
    public async Task AgenteSoloVeLaHojaDeCandidatosPostuladosASusPlazas()
    {
        var e = new Escenario();
        await e.HojasDeVida.CrearAsync(e.Candidato.Id, Hoja());

        await Assert.ThrowsAsync<AccesoDenegadoException>(() => e.HojasDeVida.ObtenerDeCandidatoAsync(e.Candidato.Id, e.ComoAgente()));

        var plaza = e.Db.AgregarPlaza(e.Agente.Id);
        await e.Postulaciones.PostularAsync(plaza.IdPlaza, e.Candidato.Id);

        Assert.Equal("Ingeniería en Sistemas", (await e.HojasDeVida.ObtenerDeCandidatoAsync(e.Candidato.Id, e.ComoAgente())).FormacionAcademica);
        await Assert.ThrowsAsync<AccesoDenegadoException>(() => e.HojasDeVida.ObtenerDeCandidatoAsync(e.Candidato.Id, e.ComoOtroAgente()));
    }

    [Fact]
    public void EsPdf_ReconoceLaFirmaDeUnPdf()
    {
        Assert.True(HojaDeVidaService.EsPdf(PdfValido));
        Assert.False(HojaDeVidaService.EsPdf(Encoding.ASCII.GetBytes("PK\u0003\u0004 archivo zip/docx")));
        Assert.False(HojaDeVidaService.EsPdf(Array.Empty<byte>()));
    }
}
