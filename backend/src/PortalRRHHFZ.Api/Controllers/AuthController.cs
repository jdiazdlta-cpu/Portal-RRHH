using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortalRRHHFZ.Application.Common;
using PortalRRHHFZ.Application.DTOs;
using PortalRRHHFZ.Application.Interfaces;
using PortalRRHHFZ.Domain.Entities;
using PortalRRHHFZ.Infrastructure.Data;

namespace PortalRRHHFZ.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(AppDbContext db, IPasswordHasher<Usuario> hasher, IJwtTokenService jwtTokenService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var usuario = await db.Usuarios
            .Include(x => x.Rol)
            .FirstOrDefaultAsync(x =>
                x.IsActive &&
                x.Rol.IsActive &&
                (x.NombreUsuario == request.NombreUsuario || x.Email == request.NombreUsuario),
                cancellationToken);

        if (usuario is null)
        {
            return Unauthorized(ApiResponse<object>.Fail("Credenciales invalidas."));
        }

        var result = hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            return Unauthorized(ApiResponse<object>.Fail("Credenciales invalidas."));
        }

        usuario.UltimoAcceso = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        var token = jwtTokenService.Generate(usuario);
        var dto = new AuthResultDto(token.Token, token.ExpiresAt, new CurrentUserDto(usuario.UsuarioId, usuario.NombreUsuario, usuario.Email, usuario.Rol.Nombre));
        return Ok(ApiResponse<AuthResultDto>.Ok(dto));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var id = User.CurrentUserId();
        var usuario = await db.Usuarios.Include(x => x.Rol).FirstOrDefaultAsync(x => x.UsuarioId == id && x.IsActive, cancellationToken);
        if (usuario is null)
        {
            return Unauthorized(ApiResponse<object>.Fail("Sesion no valida."));
        }

        return Ok(ApiResponse<CurrentUserDto>.Ok(new CurrentUserDto(usuario.UsuarioId, usuario.NombreUsuario, usuario.Email, usuario.Rol.Nombre)));
    }
}
