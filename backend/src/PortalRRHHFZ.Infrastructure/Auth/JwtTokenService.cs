using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PortalRRHHFZ.Application.DTOs.Auth;
using PortalRRHHFZ.Application.Interfaces.Auth;
using PortalRRHHFZ.Domain.Entities;

namespace PortalRRHHFZ.Infrastructure.Auth;

public sealed class JwtTokenService(IOptions<JwtOptions> options) : IJwtTokenService
{
    private readonly JwtOptions _options = options.Value;

    public LoginResponse CreateToken(Usuario usuario)
    {
        if (string.IsNullOrWhiteSpace(_options.SecretKey) || _options.SecretKey.Length < 32)
        {
            throw new InvalidOperationException("Jwt:SecretKey debe tener al menos 32 caracteres.");
        }

        var expiresAt = DateTime.UtcNow.AddMinutes(_options.ExpirationMinutes);
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var roleName = usuario.Rol.Nombre;

        var claims = new List<Claim>
        {
            new("UserId", usuario.UsuarioId.ToString()),
            new("Email", usuario.Email),
            new("NombreUsuario", usuario.NombreUsuario),
            new("Rol", roleName),
            new(ClaimTypes.NameIdentifier, usuario.UsuarioId.ToString()),
            new(ClaimTypes.Email, usuario.Email),
            new(ClaimTypes.Name, usuario.NombreUsuario),
            new(ClaimTypes.Role, roleName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new LoginResponse
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAt = expiresAt,
            User = new AuthUserDto
            {
                UserId = usuario.UsuarioId,
                Email = usuario.Email,
                NombreUsuario = usuario.NombreUsuario,
                Rol = roleName
            }
        };
    }
}
