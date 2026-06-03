using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PortalRRHHFZ.Domain.Entities;
using PortalRRHHFZ.Infrastructure.Data;

namespace PortalRRHHFZ.Infrastructure.Seed;

public sealed class DevelopmentAdminSeeder(
    AppDbContext dbContext,
    IPasswordHasher<Usuario> passwordHasher)
{
    private const string AdminRoleName = "Admin";
    private const string AdminUserName = "admin";
    private const string AdminEmail = "admin@portalrrhh.local";
    private const string AdminPassword = "Admin123*";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var exists = await dbContext.Usuarios
            .AnyAsync(usuario => usuario.Email == AdminEmail || usuario.NombreUsuario == AdminUserName, cancellationToken);

        if (exists)
        {
            return;
        }

        var adminRole = await dbContext.Roles
            .SingleAsync(rol => rol.Nombre == AdminRoleName, cancellationToken);

        var admin = new Usuario
        {
            NombreUsuario = AdminUserName,
            Email = AdminEmail,
            RolId = adminRole.RolId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "DevelopmentAdminSeeder",
            IsActive = true
        };

        admin.PasswordHash = passwordHasher.HashPassword(admin, AdminPassword);

        dbContext.Usuarios.Add(admin);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
