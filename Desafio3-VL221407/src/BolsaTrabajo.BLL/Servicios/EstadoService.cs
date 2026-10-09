using AutoMapper;
using BolsaTrabajo.DAL.Repositorios;
using BolsaTrabajo.DTOs.Sistema;

namespace BolsaTrabajo.BLL.Servicios;

public interface IEstadoService
{
    Task<EstadoSistemaDto> ObtenerAsync();
}

/// <summary>Comprueba la conexión a la base de datos y devuelve un resumen general.</summary>
public class EstadoService : IEstadoService
{
    private readonly IEstadoRepository _estado;
    private readonly IMapper _mapper;

    public EstadoService(IEstadoRepository estado, IMapper mapper)
    {
        _estado = estado;
        _mapper = mapper;
    }

    public async Task<EstadoSistemaDto> ObtenerAsync()
    {
        var dto = _mapper.Map<EstadoSistemaDto>(await _estado.ObtenerAsync());
        dto.Mensaje = "La API y la base de datos funcionan correctamente.";
        return dto;
    }
}
