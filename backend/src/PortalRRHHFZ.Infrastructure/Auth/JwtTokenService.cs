using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using PortalRRHHFZ.Application.Interfaces;
using PortalRRHHFZ.Domain.Entities;

namespace PortalRRHHFZ.Infrastructure.Auth;

public sealed class JwtTokenService(IConfiguration configuration) : IJwtTokenService
{
    public JwtTokenResult Generate(Usuario usuario)
    {
        var issuer = configuration["Jwt:Issuer"] ?? "PortalRRHHFZ";
        var audience = configuration["Jwt:Audience"] ?? "PortalRRHHFZ.Frontend";
        var key = configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key no configurado.");
        var expiresAt = DateTime.UtcNow.AddHours(int.TryParse(configuration["Jwt:ExpirationHours"], out var hours) ? hours : 8);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.UsuarioId.ToString()),
            new(ClaimTypes.NameIdentifier, usuario.UsuarioId.ToString()),
            new(ClaimTypes.Name, usuario.NombreUsuario),
            new(ClaimTypes.Email, usuario.Email),
            new(ClaimTypes.Role, usuario.Rol.Nombre),
            new("rolId", usuario.RolId.ToString())
        };

        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(issuer, audience, claims, expires: expiresAt, signingCredentials: credentials);

        return new JwtTokenResult(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
