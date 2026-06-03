using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PortalRRHHFZ.Application.Common;
using PortalRRHHFZ.Application.DTOs.Usuarios;
using PortalRRHHFZ.Application.Interfaces.Usuarios;
using PortalRRHHFZ.Domain.Entities;
using PortalRRHHFZ.Infrastructure.Data;

namespace PortalRRHHFZ.Infrastructure.Services;

public sealed partial class UsuarioService(
    AppDbContext dbContext,
    IPasswordHasher<Usuario> passwordHasher) : IUsuarioService
{
    public async Task<ApiResponse<IReadOnlyCollection<UsuarioListDto>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var usuarios = await dbContext.Usuarios
            .Include(usuario => usuario.Rol)
            .AsNoTracking()
            .OrderBy(usuario => usuario.NombreUsuario)
            .Select(usuario => new UsuarioListDto
            {
                UsuarioId = usuario.UsuarioId,
                NombreUsuario = usuario.NombreUsuario,
                Email = usuario.Email,
                Rol = usuario.Rol.Nombre,
                IsActive = usuario.IsActive,
                UltimoAcceso = usuario.UltimoAcceso,
                CreatedAt = usuario.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return ApiResponse<IReadOnlyCollection<UsuarioListDto>>.Ok(usuarios);
    }

    public async Task<ApiResponse<UsuarioDetailDto>> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var usuario = await dbContext.Usuarios
            .Include(user => user.Rol)
            .AsNoTracking()
            .SingleOrDefaultAsync(user => user.UsuarioId == id, cancellationToken);

        return usuario is null
            ? ApiResponse<UsuarioDetailDto>.Fail("Usuario no encontrado.")
            : ApiResponse<UsuarioDetailDto>.Ok(ToDetailDto(usuario));
    }

    public async Task<ApiResponse<UsuarioDetailDto>> CreateAsync(
        CreateUsuarioRequest request,
        string? currentUser,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = await ValidateCreateAsync(request, cancellationToken);

        if (validationErrors.Count > 0)
        {
            return ApiResponse<UsuarioDetailDto>.Fail("No fue posible crear el usuario.", validationErrors);
        }

        var now = DateTime.UtcNow;
        var usuario = new Usuario
        {
            NombreUsuario = request.NombreUsuario.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            RolId = request.RolId,
            IsActive = request.IsActive,
            CreatedAt = now,
            CreatedBy = currentUser
        };

        usuario.PasswordHash = passwordHasher.HashPassword(usuario, request.Password);

        dbContext.Usuarios.Add(usuario);
        await dbContext.SaveChangesAsync(cancellationToken);

        await dbContext.Entry(usuario).Reference(user => user.Rol).LoadAsync(cancellationToken);

        return ApiResponse<UsuarioDetailDto>.Ok(ToDetailDto(usuario), "Usuario creado correctamente.");
    }

    public async Task<ApiResponse<UsuarioDetailDto>> UpdateAsync(
        int id,
        UpdateUsuarioRequest request,
        string? currentUser,
        CancellationToken cancellationToken = default)
    {
        var usuario = await dbContext.Usuarios
            .Include(user => user.Rol)
            .SingleOrDefaultAsync(user => user.UsuarioId == id, cancellationToken);

        if (usuario is null)
        {
            return ApiResponse<UsuarioDetailDto>.Fail("Usuario no encontrado.");
        }

        var validationErrors = await ValidateUpdateAsync(id, request, cancellationToken);

        if (validationErrors.Count > 0)
        {
            return ApiResponse<UsuarioDetailDto>.Fail("No fue posible actualizar el usuario.", validationErrors);
        }

        usuario.NombreUsuario = request.NombreUsuario.Trim();
        usuario.Email = request.Email.Trim().ToLowerInvariant();
        usuario.RolId = request.RolId;
        usuario.IsActive = request.IsActive;
        usuario.UpdatedAt = DateTime.UtcNow;
        usuario.UpdatedBy = currentUser;

        await dbContext.SaveChangesAsync(cancellationToken);
        await dbContext.Entry(usuario).Reference(user => user.Rol).LoadAsync(cancellationToken);

        return ApiResponse<UsuarioDetailDto>.Ok(ToDetailDto(usuario), "Usuario actualizado correctamente.");
    }

    public async Task<ApiResponse<UsuarioDetailDto>> ActivateAsync(
        int id,
        string? currentUser,
        CancellationToken cancellationToken = default)
    {
        var usuario = await dbContext.Usuarios
            .Include(user => user.Rol)
            .SingleOrDefaultAsync(user => user.UsuarioId == id, cancellationToken);

        if (usuario is null)
        {
            return ApiResponse<UsuarioDetailDto>.Fail("Usuario no encontrado.");
        }

        usuario.IsActive = true;
        usuario.UpdatedAt = DateTime.UtcNow;
        usuario.UpdatedBy = currentUser;

        await dbContext.SaveChangesAsync(cancellationToken);

        return ApiResponse<UsuarioDetailDto>.Ok(ToDetailDto(usuario), "Usuario activado correctamente.");
    }

    public async Task<ApiResponse<UsuarioDetailDto>> DeactivateAsync(
        int id,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId(principal);

        if (currentUserId == id)
        {
            return ApiResponse<UsuarioDetailDto>.Fail("No puedes desactivar tu propio usuario.");
        }

        var usuario = await dbContext.Usuarios
            .Include(user => user.Rol)
            .SingleOrDefaultAsync(user => user.UsuarioId == id, cancellationToken);

        if (usuario is null)
        {
            return ApiResponse<UsuarioDetailDto>.Fail("Usuario no encontrado.");
        }

        usuario.IsActive = false;
        usuario.UpdatedAt = DateTime.UtcNow;
        usuario.UpdatedBy = principal.Identity?.Name;

        await dbContext.SaveChangesAsync(cancellationToken);

        return ApiResponse<UsuarioDetailDto>.Ok(ToDetailDto(usuario), "Usuario desactivado correctamente.");
    }

    public async Task<ApiResponse<UsuarioDetailDto>> ResetPasswordAsync(
        int id,
        ResetPasswordRequest request,
        string? currentUser,
        CancellationToken cancellationToken = default)
    {
        var usuario = await dbContext.Usuarios
            .Include(user => user.Rol)
            .SingleOrDefaultAsync(user => user.UsuarioId == id, cancellationToken);

        if (usuario is null)
        {
            return ApiResponse<UsuarioDetailDto>.Fail("Usuario no encontrado.");
        }

        var passwordErrors = ValidatePassword(request.Password);

        if (passwordErrors.Count > 0)
        {
            return ApiResponse<UsuarioDetailDto>.Fail("No fue posible resetear la contraseña.", passwordErrors);
        }

        usuario.PasswordHash = passwordHasher.HashPassword(usuario, request.Password);
        usuario.UpdatedAt = DateTime.UtcNow;
        usuario.UpdatedBy = currentUser;

        await dbContext.SaveChangesAsync(cancellationToken);

        return ApiResponse<UsuarioDetailDto>.Ok(ToDetailDto(usuario), "Contraseña actualizada correctamente.");
    }

    private async Task<List<string>> ValidateCreateAsync(
        CreateUsuarioRequest request,
        CancellationToken cancellationToken)
    {
        var errors = ValidateCommonFields(
            request.NombreUsuario,
            request.Email,
            request.RolId);

        errors.AddRange(ValidatePassword(request.Password));

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var normalizedUserName = request.NombreUsuario.Trim().ToLowerInvariant();

        if (await dbContext.Usuarios.AnyAsync(
                user => user.Email.ToLower() == normalizedEmail,
                cancellationToken))
        {
            errors.Add("El email ya esta registrado.");
        }

        if (await dbContext.Usuarios.AnyAsync(
                user => user.NombreUsuario.ToLower() == normalizedUserName,
                cancellationToken))
        {
            errors.Add("El nombre de usuario ya esta registrado.");
        }

        if (!await ActiveRoleExistsAsync(request.RolId, cancellationToken))
        {
            errors.Add("El rol seleccionado no existe o esta inactivo.");
        }

        return errors;
    }

    private async Task<List<string>> ValidateUpdateAsync(
        int usuarioId,
        UpdateUsuarioRequest request,
        CancellationToken cancellationToken)
    {
        var errors = ValidateCommonFields(
            request.NombreUsuario,
            request.Email,
            request.RolId);

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var normalizedUserName = request.NombreUsuario.Trim().ToLowerInvariant();

        if (await dbContext.Usuarios.AnyAsync(
                user => user.UsuarioId != usuarioId && user.Email.ToLower() == normalizedEmail,
                cancellationToken))
        {
            errors.Add("El email ya esta registrado.");
        }

        if (await dbContext.Usuarios.AnyAsync(
                user => user.UsuarioId != usuarioId && user.NombreUsuario.ToLower() == normalizedUserName,
                cancellationToken))
        {
            errors.Add("El nombre de usuario ya esta registrado.");
        }

        if (!await ActiveRoleExistsAsync(request.RolId, cancellationToken))
        {
            errors.Add("El rol seleccionado no existe o esta inactivo.");
        }

        return errors;
    }

    private static List<string> ValidateCommonFields(
        string nombreUsuario,
        string email,
        int rolId)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(nombreUsuario))
        {
            errors.Add("NombreUsuario es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            errors.Add("Email es obligatorio.");
        }
        else if (!EmailRegex().IsMatch(email.Trim()))
        {
            errors.Add("Email no tiene un formato valido.");
        }

        if (rolId <= 0)
        {
            errors.Add("RolId es obligatorio.");
        }

        return errors;
    }

    private static List<string> ValidatePassword(string password)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(password))
        {
            errors.Add("Password es obligatoria.");
            return errors;
        }

        if (password.Length < 8)
        {
            errors.Add("Password debe tener al menos 8 caracteres.");
        }

        if (!password.Any(char.IsUpper))
        {
            errors.Add("Password debe tener al menos una mayuscula.");
        }

        if (!password.Any(char.IsDigit))
        {
            errors.Add("Password debe tener al menos un numero.");
        }

        if (!password.Any(character => !char.IsLetterOrDigit(character)))
        {
            errors.Add("Password debe tener al menos un simbolo.");
        }

        return errors;
    }

    private async Task<bool> ActiveRoleExistsAsync(int rolId, CancellationToken cancellationToken)
    {
        return await dbContext.Roles.AnyAsync(
            rol => rol.RolId == rolId && rol.IsActive,
            cancellationToken);
    }

    private static int? GetCurrentUserId(ClaimsPrincipal principal)
    {
        var userIdValue = principal.FindFirstValue("UserId")
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

        return int.TryParse(userIdValue, out var userId) ? userId : null;
    }

    private static UsuarioDetailDto ToDetailDto(Usuario usuario)
    {
        return new UsuarioDetailDto
        {
            UsuarioId = usuario.UsuarioId,
            NombreUsuario = usuario.NombreUsuario,
            Email = usuario.Email,
            RolId = usuario.RolId,
            Rol = usuario.Rol.Nombre,
            IsActive = usuario.IsActive,
            UltimoAcceso = usuario.UltimoAcceso,
            CreatedAt = usuario.CreatedAt,
            UpdatedAt = usuario.UpdatedAt,
            CreatedBy = usuario.CreatedBy,
            UpdatedBy = usuario.UpdatedBy
        };
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase)]
    private static partial Regex EmailRegex();
}
