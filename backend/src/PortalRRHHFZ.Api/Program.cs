using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using PortalRRHHFZ.Application.Common;
using PortalRRHHFZ.Application.DTOs.Auth;
using PortalRRHHFZ.Application.DTOs.Usuarios;
using PortalRRHHFZ.Application.Interfaces.Auth;
using PortalRRHHFZ.Application.Interfaces.Usuarios;
using PortalRRHHFZ.Infrastructure;
using PortalRRHHFZ.Infrastructure.Auth;
using PortalRRHHFZ.Infrastructure.Data;
using PortalRRHHFZ.Infrastructure.Seed;

var builder = WebApplication.CreateBuilder(args);

const string FrontendCorsPolicy = "FrontendCorsPolicy";
var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()
    ?? throw new InvalidOperationException("La configuracion Jwt no esta definida.");

if (string.IsNullOrWhiteSpace(jwtOptions.SecretKey) || jwtOptions.SecretKey.Length < 32)
{
    throw new InvalidOperationException("Jwt:SecretKey debe tener al menos 32 caracteres.");
}

builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173",
                "https://localhost:5173",
                "http://127.0.0.1:5173",
                "https://127.0.0.1:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingresa solo el token JWT. Swagger enviara: Bearer {token}."
    });

    options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", null),
            []
        }
    });
});
builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("RequireAdminOrRRHH", policy => policy.RequireRole("Admin", "RRHH"));
    options.AddPolicy("RequireSupervisor", policy => policy.RequireRole("Supervisor"));
    options.AddPolicy("RequireConsulta", policy => policy.RequireRole("Consulta"));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();

    using var scope = app.Services.CreateScope();
    var adminSeeder = scope.ServiceProvider.GetRequiredService<DevelopmentAdminSeeder>();
    await adminSeeder.SeedAsync();
}

app.UseHttpsRedirection();
app.UseCors(FrontendCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

var authGroup = app.MapGroup("/api/auth")
    .WithTags("Auth");

authGroup.MapPost("/login", async (
        LoginRequest request,
        IAuthService authService,
        CancellationToken cancellationToken) =>
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.BadRequest(ApiResponse<LoginResponse>.Fail(
                "Email y password son requeridos."));
        }

        var response = await authService.LoginAsync(request, cancellationToken);

        return response.Success
            ? Results.Ok(response)
            : Results.Json(response, statusCode: StatusCodes.Status401Unauthorized);
    })
    .AllowAnonymous()
    .WithName("Login");

authGroup.MapGet("/me", async (
        HttpContext httpContext,
        IAuthService authService,
        CancellationToken cancellationToken) =>
    {
        var response = await authService.GetCurrentUserAsync(httpContext.User, cancellationToken);

        return response.Success
            ? Results.Ok(response)
            : Results.Json(response, statusCode: StatusCodes.Status401Unauthorized);
    })
    .RequireAuthorization()
    .WithName("CurrentUser");

var usuariosGroup = app.MapGroup("/api/usuarios")
    .RequireAuthorization("RequireAdmin")
    .WithTags("Usuarios");

usuariosGroup.MapGet("/", async (
        IUsuarioService usuarioService,
        CancellationToken cancellationToken) =>
    {
        var response = await usuarioService.GetAllAsync(cancellationToken);
        return Results.Ok(response);
    })
    .WithName("GetUsuarios");

usuariosGroup.MapGet("/{id:int}", async (
        int id,
        IUsuarioService usuarioService,
        CancellationToken cancellationToken) =>
    {
        var response = await usuarioService.GetByIdAsync(id, cancellationToken);

        return response.Success
            ? Results.Ok(response)
            : Results.NotFound(response);
    })
    .WithName("GetUsuarioById");

usuariosGroup.MapPost("/", async (
        CreateUsuarioRequest request,
        HttpContext httpContext,
        IUsuarioService usuarioService,
        CancellationToken cancellationToken) =>
    {
        var response = await usuarioService.CreateAsync(
            request,
            httpContext.User.Identity?.Name,
            cancellationToken);

        return response.Success
            ? Results.Created($"/api/usuarios/{response.Data?.UsuarioId}", response)
            : Results.BadRequest(response);
    })
    .WithName("CreateUsuario");

usuariosGroup.MapPut("/{id:int}", async (
        int id,
        UpdateUsuarioRequest request,
        HttpContext httpContext,
        IUsuarioService usuarioService,
        CancellationToken cancellationToken) =>
    {
        var response = await usuarioService.UpdateAsync(
            id,
            request,
            httpContext.User.Identity?.Name,
            cancellationToken);

        return response.Success
            ? Results.Ok(response)
            : Results.BadRequest(response);
    })
    .WithName("UpdateUsuario");

usuariosGroup.MapPatch("/{id:int}/activar", async (
        int id,
        HttpContext httpContext,
        IUsuarioService usuarioService,
        CancellationToken cancellationToken) =>
    {
        var response = await usuarioService.ActivateAsync(
            id,
            httpContext.User.Identity?.Name,
            cancellationToken);

        return response.Success
            ? Results.Ok(response)
            : Results.BadRequest(response);
    })
    .WithName("ActivateUsuario");

usuariosGroup.MapPatch("/{id:int}/desactivar", async (
        int id,
        HttpContext httpContext,
        IUsuarioService usuarioService,
        CancellationToken cancellationToken) =>
    {
        var response = await usuarioService.DeactivateAsync(
            id,
            httpContext.User,
            cancellationToken);

        return response.Success
            ? Results.Ok(response)
            : Results.BadRequest(response);
    })
    .WithName("DeactivateUsuario");

usuariosGroup.MapPut("/{id:int}/reset-password", async (
        int id,
        ResetPasswordRequest request,
        HttpContext httpContext,
        IUsuarioService usuarioService,
        CancellationToken cancellationToken) =>
    {
        var response = await usuarioService.ResetPasswordAsync(
            id,
            request,
            httpContext.User.Identity?.Name,
            cancellationToken);

        return response.Success
            ? Results.Ok(response)
            : Results.BadRequest(response);
    })
    .WithName("ResetUsuarioPassword");

app.MapGet("/api/health", () =>
        Results.Ok(ApiResponse<object>.Ok(new
        {
            service = "Portal RRHH FZ API",
            status = "Healthy",
            timestamp = DateTimeOffset.UtcNow
        })))
    .WithName("HealthCheck")
    .WithTags("System");

app.MapGet("/api/db-test", async (AppDbContext dbContext) =>
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        try
        {
            var canConnect = await dbContext.Database.CanConnectAsync(timeout.Token);

            return canConnect
                ? Results.Ok(ApiResponse<object>.Ok(new { canConnect }, "Conexion a SQL Server exitosa."))
                : Results.Ok(ApiResponse<object>.Fail("No fue posible conectar con SQL Server."));
        }
        catch (OperationCanceledException)
        {
            return Results.Ok(ApiResponse<object>.Fail(
                "Tiempo agotado al probar la conexion a SQL Server.",
                ["Verifica que DefaultConnection apunte a una instancia disponible."]));
        }
        catch (Exception ex)
        {
            return Results.Ok(ApiResponse<object>.Fail(
                "Error al probar la conexion a SQL Server.",
                [ex.Message]));
        }
    })
    .WithName("DatabaseTest")
    .WithTags("System");

app.Run();
