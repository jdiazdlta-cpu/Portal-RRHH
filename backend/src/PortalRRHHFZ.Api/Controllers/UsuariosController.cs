using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using PortalRRHHFZ.Application.Common;
using PortalRRHHFZ.Application.DTOs;
using PortalRRHHFZ.Domain.Constants;
using PortalRRHHFZ.Domain.Entities;
using PortalRRHHFZ.Infrastructure.Data;

namespace PortalRRHHFZ.Api.Controllers;

[ApiController]
[Authorize(Policy = AppPolicies.RequireAdmin)]
[Route("api/usuarios")]
public sealed class UsuariosController(AppDbContext db, IPasswordHasher<Usuario> hasher) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var data = await db.Usuarios.Include(x => x.Rol).OrderBy(x => x.NombreUsuario).Select(x => x.ToDto()).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<UsuarioDto>>.Ok(data));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var usuario = await db.Usuarios.Include(x => x.Rol).FirstOrDefaultAsync(x => x.UsuarioId == id, cancellationToken);
        return usuario is null
            ? NotFound(ApiResponse<object>.Fail("Usuario no encontrado."))
            : Ok(ApiResponse<UsuarioDto>.Ok(usuario.ToDto()));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUsuarioRequest request, CancellationToken cancellationToken)
    {
        var validation = await ValidateUsuarioAsync(request.NombreUsuario, request.Email, request.RolId, null, cancellationToken);
        if (validation is not null)
        {
            return BadRequest(ApiResponse<object>.Fail(validation));
        }

        var passwordValidation = ValidatePassword(request.Password, request.ConfirmPassword);
        if (passwordValidation is not null)
        {
            return BadRequest(ApiResponse<object>.Fail(passwordValidation));
        }

        var usuario = new Usuario
        {
            NombreUsuario = request.NombreUsuario.Trim(),
            Email = request.Email.Trim(),
            RolId = request.RolId,
            IsActive = request.IsActive,
            CreatedBy = User.Identity?.Name
        };
        usuario.PasswordHash = hasher.HashPassword(usuario, request.Password);

        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync(cancellationToken);
        await db.Entry(usuario).Reference(x => x.Rol).LoadAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = usuario.UsuarioId }, ApiResponse<UsuarioDto>.Ok(usuario.ToDto(), "Usuario creado."));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUsuarioRequest request, CancellationToken cancellationToken)
    {
        var usuario = await db.Usuarios.Include(x => x.Rol).FirstOrDefaultAsync(x => x.UsuarioId == id, cancellationToken);
        if (usuario is null)
        {
            return NotFound(ApiResponse<object>.Fail("Usuario no encontrado."));
        }

        var validation = await ValidateUsuarioAsync(request.NombreUsuario, request.Email, request.RolId, id, cancellationToken);
        if (validation is not null)
        {
            return BadRequest(ApiResponse<object>.Fail(validation));
        }

        usuario.NombreUsuario = request.NombreUsuario.Trim();
        usuario.Email = request.Email.Trim();
        usuario.RolId = request.RolId;
        usuario.IsActive = request.IsActive;
        usuario.UpdatedBy = User.Identity?.Name;
        await db.SaveChangesAsync(cancellationToken);
        await db.Entry(usuario).Reference(x => x.Rol).LoadAsync(cancellationToken);

        return Ok(ApiResponse<UsuarioDto>.Ok(usuario.ToDto(), "Usuario actualizado."));
    }

    [HttpPatch("{id:int}/activar")]
    public Task<IActionResult> Activar(int id, CancellationToken cancellationToken) => Toggle(id, true, cancellationToken);

    [HttpPatch("{id:int}/desactivar")]
    public Task<IActionResult> Desactivar(int id, CancellationToken cancellationToken) => Toggle(id, false, cancellationToken);

    [HttpPut("{id:int}/reset-password")]
    public async Task<IActionResult> ResetPassword(int id, [FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var passwordValidation = ValidatePassword(request.Password, null);
        if (passwordValidation is not null)
        {
            return BadRequest(ApiResponse<object>.Fail(passwordValidation));
        }

        var usuario = await db.Usuarios.FirstOrDefaultAsync(x => x.UsuarioId == id, cancellationToken);
        if (usuario is null)
        {
            return NotFound(ApiResponse<object>.Fail("Usuario no encontrado."));
        }

        usuario.PasswordHash = hasher.HashPassword(usuario, request.Password);
        usuario.UpdatedBy = User.Identity?.Name;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { usuario.UsuarioId }, "Contrasena actualizada."));
    }

    private async Task<IActionResult> Toggle(int id, bool active, CancellationToken cancellationToken)
    {
        var usuario = await db.Usuarios.Include(x => x.Rol).FirstOrDefaultAsync(x => x.UsuarioId == id, cancellationToken);
        if (usuario is null)
        {
            return NotFound(ApiResponse<object>.Fail("Usuario no encontrado."));
        }

        if (!active && id == User.CurrentUserId())
        {
            return BadRequest(ApiResponse<object>.Fail("No puedes desactivar tu propio usuario."));
        }

        usuario.IsActive = active;
        usuario.UpdatedBy = User.Identity?.Name;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<UsuarioDto>.Ok(usuario.ToDto(), active ? "Usuario activado." : "Usuario desactivado."));
    }

    private async Task<string?> ValidateUsuarioAsync(string nombreUsuario, string email, int rolId, int? usuarioId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(nombreUsuario))
        {
            return "Nombre de usuario es obligatorio.";
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return "Email es obligatorio.";
        }

        if (!IsValidEmail(email))
        {
            return "Email no tiene un formato valido.";
        }

        if (!await db.Roles.AnyAsync(x => x.RolId == rolId && x.IsActive, cancellationToken))
        {
            return "Rol no valido.";
        }

        var normalizedUser = nombreUsuario.Trim();
        var normalizedEmail = email.Trim();
        if (await db.Usuarios.AnyAsync(x => x.NombreUsuario == normalizedUser && (!usuarioId.HasValue || x.UsuarioId != usuarioId.Value), cancellationToken))
        {
            return "Nombre de usuario ya existe.";
        }

        if (await db.Usuarios.AnyAsync(x => x.Email == normalizedEmail && (!usuarioId.HasValue || x.UsuarioId != usuarioId.Value), cancellationToken))
        {
            return "Email ya existe.";
        }

        return null;
    }

    private static string? ValidatePassword(string password, string? confirmPassword)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            return "La contrasena debe tener al menos 8 caracteres.";
        }

        if (confirmPassword is not null && password != confirmPassword)
        {
            return "La confirmacion de contrasena no coincide.";
        }

        if (!password.Any(char.IsUpper) || !password.Any(char.IsLower) || !password.Any(char.IsDigit) || !password.Any(ch => !char.IsLetterOrDigit(ch)))
        {
            return "La contrasena debe incluir mayuscula, minuscula, numero y simbolo.";
        }

        return null;
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var address = new MailAddress(email.Trim());
            return address.Address == email.Trim();
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
