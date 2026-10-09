using AutoMapper;
using BolsaTrabajo.BLL.Archivos;
using BolsaTrabajo.BLL.Excepciones;
using BolsaTrabajo.DAL.Excepciones;
using BolsaTrabajo.DAL.Repositorios;
using BolsaTrabajo.DTOs.Auth;
using BolsaTrabajo.DTOs.Usuarios;
using BolsaTrabajo.Entities;
using Microsoft.AspNetCore.Identity;

namespace BolsaTrabajo.BLL.Servicios;

public interface IUsuarioService
{
    Task<List<UsuarioDto>> ListarAsync(string? rol = null);
    Task<UsuarioDto> ObtenerAsync(string idUsuario);
    Task<List<RolDto>> ListarRolesAsync();
    Task<UsuarioDto> RegistrarCandidatoAsync(RegistroCandidatoDto dto);
    Task<UsuarioDto> CrearAsync(CrearUsuarioDto dto);
    Task<UsuarioDto> ActualizarDatosAsync(string idUsuario, ActualizarPerfilDto dto);
    Task<UsuarioDto> CambiarRolAsync(string idUsuario, string nuevoRol, string idAdministradorActual);
    Task EliminarAsync(string idUsuario, string idAdministradorActual);
    Task<UsuarioDto> IniciarSesionAsync(string email, string password);
}

/// <summary>
/// Gestión de usuarios.
/// - Crear usuario, contraseñas, roles e inicio de sesión: ASP.NET Core Identity (UserManager).
/// - Consultar, actualizar datos y eliminar: Dapper (IUsuarioRepository).
/// </summary>
public class UsuarioService : IUsuarioService
{
    private readonly UserManager<Usuario> _userManager;
    private readonly IUsuarioRepository _usuarios;
    private readonly IPlazaRepository _plazas;
    private readonly IPostulacionRepository _postulaciones;
    private readonly IHojaDeVidaRepository _hojas;
    private readonly IAlmacenArchivosCV _almacen;
    private readonly IMapper _mapper;

    public UsuarioService(
        UserManager<Usuario> userManager,
        IUsuarioRepository usuarios,
        IPlazaRepository plazas,
        IPostulacionRepository postulaciones,
        IHojaDeVidaRepository hojas,
        IAlmacenArchivosCV almacen,
        IMapper mapper)
    {
        _userManager = userManager;
        _usuarios = usuarios;
        _plazas = plazas;
        _postulaciones = postulaciones;
        _hojas = hojas;
        _almacen = almacen;
        _mapper = mapper;
    }

    public async Task<List<UsuarioDto>> ListarAsync(string? rol = null)
    {
        if (!string.IsNullOrWhiteSpace(rol))
            ValidarRol(rol);
        return _mapper.Map<List<UsuarioDto>>(await _usuarios.ListarAsync(string.IsNullOrWhiteSpace(rol) ? null : rol));
    }

    public async Task<UsuarioDto> ObtenerAsync(string idUsuario) =>
        _mapper.Map<UsuarioDto>(await _usuarios.ObtenerPorIdAsync(idUsuario)
            ?? throw new NoEncontradoException("No existe el usuario indicado."));

    public async Task<List<RolDto>> ListarRolesAsync() =>
        _mapper.Map<List<RolDto>>(await _usuarios.ListarRolesAsync());

    public Task<UsuarioDto> RegistrarCandidatoAsync(RegistroCandidatoDto dto) =>
        CrearConRolAsync(dto.NombreCompleto, dto.Email, dto.Password, Roles.Candidato);

    public Task<UsuarioDto> CrearAsync(CrearUsuarioDto dto) =>
        CrearConRolAsync(dto.NombreCompleto, dto.Email, dto.Password, dto.Rol);

    public async Task<UsuarioDto> ActualizarDatosAsync(string idUsuario, ActualizarPerfilDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.NombreCompleto))
            throw new ReglaNegocioException("El nombre completo es obligatorio.");

        var telefono = string.IsNullOrWhiteSpace(dto.Telefono) ? null : dto.Telefono.Trim();
        if (!await _usuarios.ActualizarDatosAsync(idUsuario, dto.NombreCompleto.Trim(), telefono))
            throw new NoEncontradoException("No existe el usuario indicado.");

        return await ObtenerAsync(idUsuario);
    }

    public async Task<UsuarioDto> CambiarRolAsync(string idUsuario, string nuevoRol, string idAdministradorActual)
    {
        ValidarRol(nuevoRol);
        if (idUsuario == idAdministradorActual && nuevoRol != Roles.Administrador)
            throw new ReglaNegocioException("No puede quitarse a sí mismo el rol de Administrador.");

        var usuario = await _userManager.FindByIdAsync(idUsuario)
            ?? throw new NoEncontradoException("No existe el usuario indicado.");

        if (usuario.Rol == Roles.AgenteSeleccion && nuevoRol != Roles.AgenteSeleccion
            && await _plazas.ContarPorAgenteAsync(idUsuario) > 0)
            throw new ReglaNegocioException("No se puede cambiar el rol: el agente tiene plazas a su cargo.");
        if (usuario.Rol == Roles.Candidato && nuevoRol != Roles.Candidato
            && await _postulaciones.ContarPorCandidatoAsync(idUsuario) > 0)
            throw new ReglaNegocioException("No se puede cambiar el rol: el candidato tiene postulaciones registradas.");

        var rolesActuales = await _userManager.GetRolesAsync(usuario);
        if (rolesActuales.Count > 0)
            Verificar(await _userManager.RemoveFromRolesAsync(usuario, rolesActuales));
        Verificar(await _userManager.AddToRoleAsync(usuario, nuevoRol));

        usuario.Rol = nuevoRol;
        Verificar(await _userManager.UpdateAsync(usuario));
        return await ObtenerAsync(idUsuario);
    }

    /// <summary>Elimina el usuario, su hoja de vida y su CV, si no tiene plazas ni postulaciones.</summary>
    public async Task EliminarAsync(string idUsuario, string idAdministradorActual)
    {
        if (idUsuario == idAdministradorActual)
            throw new ReglaNegocioException("No puede eliminar su propia cuenta.");

        var usuario = await _usuarios.ObtenerPorIdAsync(idUsuario)
            ?? throw new NoEncontradoException("No existe el usuario indicado.");

        var totalPlazas = await _plazas.ContarPorAgenteAsync(idUsuario);
        var totalPostulaciones = await _postulaciones.ContarPorCandidatoAsync(idUsuario);
        if (totalPlazas > 0 || totalPostulaciones > 0)
            throw new ReglaNegocioException(
                $"No se puede eliminar a {usuario.NombreCompleto}: tiene {totalPlazas} plaza(s) y {totalPostulaciones} postulación(es) registradas.");

        var hoja = await _hojas.ObtenerPorUsuarioAsync(idUsuario);
        try
        {
            await _usuarios.EliminarAsync(idUsuario);
        }
        catch (RegistroEnUsoException)
        {
            throw new ReglaNegocioException("No se puede eliminar el usuario porque tiene plazas o postulaciones registradas.");
        }

        _almacen.Eliminar(hoja?.ArchivoCV);
    }

    /// <summary>
    /// Verifica correo y contraseña. Tras 5 intentos fallidos la cuenta se bloquea 5 minutos
    /// (protección de Identity contra ataques de fuerza bruta).
    /// </summary>
    public async Task<UsuarioDto> IniciarSesionAsync(string email, string password)
    {
        const string credencialesInvalidas = "Correo o contraseña incorrectos.";

        var usuario = await _userManager.FindByEmailAsync(email)
            ?? throw new NoAutenticadoException(credencialesInvalidas);

        if (await _userManager.IsLockedOutAsync(usuario))
            throw new NoAutenticadoException(
                "La cuenta está bloqueada temporalmente por varios intentos fallidos. Intente de nuevo en unos minutos.");

        if (!await _userManager.CheckPasswordAsync(usuario, password))
        {
            await _userManager.AccessFailedAsync(usuario);
            throw new NoAutenticadoException(credencialesInvalidas);
        }

        await _userManager.ResetAccessFailedCountAsync(usuario);

        // El rol que viaja en el token sale de las tablas de roles de Identity.
        var dto = _mapper.Map<UsuarioDto>(usuario);
        dto.Rol = (await _userManager.GetRolesAsync(usuario)).FirstOrDefault() ?? usuario.Rol;
        return dto;
    }

    private async Task<UsuarioDto> CrearConRolAsync(string nombreCompleto, string email, string password, string rol)
    {
        ValidarRol(rol);
        if (string.IsNullOrWhiteSpace(nombreCompleto))
            throw new ReglaNegocioException("El nombre completo es obligatorio.");
        if (await _userManager.FindByEmailAsync(email) is not null)
            throw new ReglaNegocioException("Ya existe una cuenta registrada con ese correo.");

        var usuario = new Usuario
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            NombreCompleto = nombreCompleto.Trim(),
            Rol = rol
        };

        Verificar(await _userManager.CreateAsync(usuario, password));
        Verificar(await _userManager.AddToRoleAsync(usuario, rol));
        return await ObtenerAsync(usuario.Id);
    }

    private static void ValidarRol(string rol)
    {
        if (!Roles.Todos.Contains(rol))
            throw new ReglaNegocioException(
                $"El rol '{rol}' no es válido. Roles disponibles: {string.Join(", ", Roles.Todos)}.");
    }

    /// <summary>Convierte los errores de Identity (contraseña débil, correo repetido...) en un mensaje claro.</summary>
    private static void Verificar(IdentityResult resultado)
    {
        if (!resultado.Succeeded)
            throw new ReglaNegocioException(string.Join(" ", resultado.Errors.Select(e => e.Description)));
    }
}
