using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PortalRRHHFZ.Application.Interfaces.Alertas;
using PortalRRHHFZ.Application.Interfaces.Auth;
using PortalRRHHFZ.Application.Interfaces.Cargos;
using PortalRRHHFZ.Application.Interfaces.Catalogos;
using PortalRRHHFZ.Application.Interfaces.Colaboradores;
using PortalRRHHFZ.Application.Interfaces.Dashboard;
using PortalRRHHFZ.Application.Interfaces.Departamentos;
using PortalRRHHFZ.Application.Interfaces.Documentos;
using PortalRRHHFZ.Application.Interfaces.Empresas;
using PortalRRHHFZ.Application.Interfaces.Storage;
using PortalRRHHFZ.Application.Interfaces.Usuarios;
using PortalRRHHFZ.Domain.Entities;
using PortalRRHHFZ.Infrastructure.Auth;
using PortalRRHHFZ.Infrastructure.Data;
using PortalRRHHFZ.Infrastructure.Seed;
using PortalRRHHFZ.Infrastructure.Services;

namespace PortalRRHHFZ.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("La cadena de conexion 'DefaultConnection' no esta configurada.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
        services.AddScoped<IAlertaService, AlertaService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICargoService, CargoService>();
        services.AddScoped<ICatalogoService, CatalogoService>();
        services.AddScoped<IColaboradorService, ColaboradorService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IDepartamentoService, DepartamentoService>();
        services.AddScoped<IDocumentoColaboradorService, DocumentoColaboradorService>();
        services.AddScoped<IEmpresaService, EmpresaService>();
        services.AddScoped<IFileStorageService, LocalFileStorageService>();
        services.AddScoped<IUsuarioService, UsuarioService>();
        services.AddScoped<DevelopmentAdminSeeder>();

        return services;
    }
}
