using AutoMapper;
using BolsaTrabajo.DTOs.HojasDeVida;
using BolsaTrabajo.DTOs.Plazas;
using BolsaTrabajo.DTOs.Postulaciones;
using BolsaTrabajo.DTOs.Sistema;
using BolsaTrabajo.DTOs.Usuarios;
using BolsaTrabajo.Entities;
using BolsaTrabajo.Entities.Consultas;

namespace BolsaTrabajo.BLL.Mapping;

/// <summary>
/// Reglas de AutoMapper para convertir entre entidades/consultas y DTOs.
/// Los mapas de entrada (DTO → entidad) usan MemberList.Source: se valida que se usen todos
/// los datos que envía el usuario, y los campos que controla el sistema (Id, Estado, IdAgente...) no se tocan.
/// </summary>
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Plazas
        CreateMap<PlazaDetalle, PlazaDto>();
        CreateMap<GuardarPlazaDto, Plaza>(MemberList.Source);
        CreateMap<ResumenProcesoSeleccion, ResumenProcesoDto>();

        // Postulaciones
        CreateMap<PostulacionDetalle, PostulacionDto>();

        // Hojas de vida
        CreateMap<HojaDeVida, HojaDeVidaDto>()
            .ForMember(d => d.NombreCandidato, o => o.MapFrom(s => s.Usuario != null ? s.Usuario.NombreCompleto : string.Empty))
            .ForMember(d => d.EmailCandidato, o => o.MapFrom(s => s.Usuario != null ? s.Usuario.Email : null))
            .ForMember(d => d.TieneArchivoCV, o => o.MapFrom(s => !string.IsNullOrEmpty(s.ArchivoCV)));
        CreateMap<GuardarHojaDeVidaDto, HojaDeVida>(MemberList.Source);

        // Usuarios y roles
        CreateMap<Usuario, UsuarioDto>()
            .ForMember(d => d.Email, o => o.MapFrom(s => s.Email ?? string.Empty))
            .ForMember(d => d.Telefono, o => o.MapFrom(s => s.PhoneNumber));
        CreateMap<RolResumen, RolDto>();

        // Estado del sistema
        CreateMap<EstadoSistema, EstadoSistemaDto>()
            .ForMember(d => d.Mensaje, o => o.Ignore());
    }
}
