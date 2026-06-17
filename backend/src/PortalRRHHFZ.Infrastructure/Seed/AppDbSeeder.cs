using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PortalRRHHFZ.Domain.Entities;
using PortalRRHHFZ.Infrastructure.Data;

namespace PortalRRHHFZ.Infrastructure.Seed;

public static class AppDbSeeder
{
    public static async Task SeedAsync(IServiceProvider services, bool seedDevelopmentAdmin, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (!await db.Database.CanConnectAsync(cancellationToken))
        {
            return;
        }

        if (!seedDevelopmentAdmin)
        {
            return;
        }

        var adminRole = await db.Roles.FirstAsync(x => x.Nombre == "Admin", cancellationToken);
        var exists = await db.Usuarios.AnyAsync(x => x.NombreUsuario == "admin" || x.Email == "admin@portalrrhh.local", cancellationToken);
        if (exists)
        {
            return;
        }

        var admin = new Usuario
        {
            NombreUsuario = "admin",
            Email = "admin@portalrrhh.local",
            RolId = adminRole.RolId,
            IsActive = true,
            CreatedBy = "seed"
        };

        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Usuario>>();
        admin.PasswordHash = hasher.HashPassword(admin, "Admin123*");
        db.Usuarios.Add(admin);
        await db.SaveChangesAsync(cancellationToken);
    }
}
