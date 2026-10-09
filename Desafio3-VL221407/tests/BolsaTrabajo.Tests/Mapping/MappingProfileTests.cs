using AutoMapper;
using BolsaTrabajo.BLL.Mapping;
using BolsaTrabajo.DTOs.HojasDeVida;
using BolsaTrabajo.DTOs.Plazas;
using BolsaTrabajo.Entities;
using BolsaTrabajo.Entities.Consultas;
using BolsaTrabajo.Entities.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace BolsaTrabajo.Tests.Mapping;

/// <summary>Comprueba que AutoMapper esté bien configurado para todos los DTOs.</summary>
public class MappingProfileTests
{
    private static readonly MapperConfiguration Configuracion =
        new(cfg => cfg.AddProfile<MappingProfile>(), NullLoggerFactory.Instance);

    [Fact]
    public void ConfiguracionDeAutoMapper_EsValida_NingunCampoQuedaSinMapear()
    {
        Configuracion.AssertConfigurationIsValid();
    }

    [Fact]
    public void GuardarPlazaDto_NoSobrescribeLosCamposQueControlaElSistema()
    {
        var mapper = Configuracion.CreateMapper();
        var plaza = new Plaza { IdPlaza = 7, IdAgente = "agente-1", Estado = EstadoPlaza.Cerrada, Publicada = true };

        mapper.Map(new GuardarPlazaDto { Titulo = "Nuevo", Descripcion = "D", Institucion = "I" }, plaza);

        Assert.Equal("Nuevo", plaza.Titulo);
        Assert.Equal(7, plaza.IdPlaza);
        Assert.Equal("agente-1", plaza.IdAgente);
        Assert.Equal(EstadoPlaza.Cerrada, plaza.Estado);
        Assert.True(plaza.Publicada);
    }

    [Fact]
    public void PlazaDetalle_SeConvierteEnPlazaDto()
    {
        var dto = Configuracion.CreateMapper().Map<PlazaDto>(new PlazaDetalle
        {
            IdPlaza = 3, Titulo = "Docente", NombreAgente = "María", TotalPostulaciones = 4, Estado = EstadoPlaza.EnEvaluacion
        });

        Assert.Equal(3, dto.IdPlaza);
        Assert.Equal("María", dto.NombreAgente);
        Assert.Equal(4, dto.TotalPostulaciones);
        Assert.Equal(EstadoPlaza.EnEvaluacion, dto.Estado);
    }

    [Fact]
    public void HojaDeVida_IndicaSiTieneCvYSuNombre()
    {
        var mapper = Configuracion.CreateMapper();

        var conCv = mapper.Map<HojaDeVidaDto>(new HojaDeVida { ArchivoCV = "x.pdf", NombreArchivoCV = "Mi CV.pdf", Usuario = new Usuario { NombreCompleto = "Ana" } });
        var sinCv = mapper.Map<HojaDeVidaDto>(new HojaDeVida());

        Assert.True(conCv.TieneArchivoCV);
        Assert.Equal("Mi CV.pdf", conCv.NombreArchivoCV);
        Assert.Equal("Ana", conCv.NombreCandidato);
        Assert.False(sinCv.TieneArchivoCV);
    }
}
