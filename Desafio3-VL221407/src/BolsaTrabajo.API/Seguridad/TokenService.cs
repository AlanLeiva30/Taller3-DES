using System.Security.Claims;
using System.Text;
using BolsaTrabajo.DTOs.Auth;
using BolsaTrabajo.DTOs.Usuarios;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace BolsaTrabajo.API.Seguridad;

/// <summary>Genera el token JWT que el cliente envía en cada petición ("Authorization: Bearer ...").</summary>
public class TokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration) => _configuration = configuration;

    public RespuestaLoginDto GenerarToken(UsuarioDto usuario)
    {
        var jwt = _configuration.GetSection("Jwt");
        var expira = DateTime.UtcNow.AddMinutes(jwt.GetValue("ExpiraEnMinutos", 120));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = jwt["Issuer"],
            Audience = jwt["Audience"],
            Expires = expira,
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id),
                new Claim(ClaimTypes.Name, usuario.Email),
                new Claim(ClaimTypes.Email, usuario.Email),
                new Claim(ClaimTypes.Role, usuario.Rol),
                new Claim("nombre_completo", usuario.NombreCompleto)
            }),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!)),
                SecurityAlgorithms.HmacSha256)
        };

        return new RespuestaLoginDto
        {
            Token = new JsonWebTokenHandler().CreateToken(descriptor),
            Expira = expira,
            Usuario = usuario
        };
    }
}
