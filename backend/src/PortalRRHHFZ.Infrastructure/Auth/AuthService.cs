using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PortalRRHHFZ.Application.Common;
using PortalRRHHFZ.Application.DTOs.Auth;
using PortalRRHHFZ.Application.Interfaces.Auth;
using PortalRRHHFZ.Domain.Entities;
using PortalRRHHFZ.Infrastructure.Data;

namespace PortalRRHHFZ.Infrastructure.Auth;

public sealed class AuthService(
    AppDbContext dbContext,
    IPasswordHasher<Usuario> passwordHasher,
    IJwtTokenService jwtTokenService) : IAuthService
{
    public async Task<ApiResponse<LoginResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var usuario = await dbContext.Usuarios
            .Include(user => user.Rol)
            .SingleOrDefaultAsync(user => user.Email.ToLower() == email, cancellationToken);

        if (usuario is null)
        {
            return ApiResponse<LoginResponse>.Fail("Credenciales invalidas.");
        }

        if (!usuario.IsActive)
        {
            return ApiResponse<LoginResponse>.Fail("Usuario inactivo.");
        }

        if (usuario.Rol is null || !usuario.Rol.IsActive)
        {
            return ApiResponse<LoginResponse>.Fail("Rol inactivo.");
        }

        var verificationResult = passwordHasher.VerifyHashedPassword(
            usuario,
            usuario.PasswordHash,
            request.Password);

        if (verificationResult == PasswordVerificationResult.Failed)
        {
            return ApiResponse<LoginResponse>.Fail("Credenciales invalidas.");
        }

        usuario.UltimoAcceso = DateTime.UtcNow;
        usuario.UpdatedAt = DateTime.UtcNow;
        usuario.UpdatedBy = "Auth";

        if (verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
        {
            usuario.PasswordHash = passwordHasher.HashPassword(usuario, request.Password);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return ApiResponse<LoginResponse>.Ok(jwtTokenService.CreateToken(usuario), "Login exitoso.");
    }

    public async Task<ApiResponse<AuthUserDto>> GetCurrentUserAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        var userIdValue = principal.FindFirstValue("UserId")
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out var userId))
        {
            return ApiResponse<AuthUserDto>.Fail("Token invalido.");
        }

        var usuario = await dbContext.Usuarios
            .Include(user => user.Rol)
            .AsNoTracking()
            .SingleOrDefaultAsync(user => user.UsuarioId == userId, cancellationToken);

        if (usuario is null || !usuario.IsActive || usuario.Rol is null || !usuario.Rol.IsActive)
        {
            return ApiResponse<AuthUserDto>.Fail("Usuario no autorizado.");
        }

        return ApiResponse<AuthUserDto>.Ok(new AuthUserDto
        {
            UserId = usuario.UsuarioId,
            Email = usuario.Email,
            NombreUsuario = usuario.NombreUsuario,
            Rol = usuario.Rol.Nombre
        });
    }
}
