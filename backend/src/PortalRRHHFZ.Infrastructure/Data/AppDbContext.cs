using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PortalRRHHFZ.Domain.Entities;
using PortalRRHHFZ.Domain.Enums;

namespace PortalRRHHFZ.Infrastructure.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    private static readonly DateTime SeedDate = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Empresa> Empresas => Set<Empresa>();
    public DbSet<Departamento> Departamentos => Set<Departamento>();
    public DbSet<Cargo> Cargos => Set<Cargo>();
    public DbSet<TipoContrato> TiposContrato => Set<TipoContrato>();
    public DbSet<EstatusColaborador> EstatusColaborador => Set<EstatusColaborador>();
    public DbSet<MotivoSalida> MotivosSalida => Set<MotivoSalida>();
    public DbSet<TipoDocumento> TiposDocumento => Set<TipoDocumento>();
    public DbSet<Colaborador> Colaboradores => Set<Colaborador>();
    public DbSet<DocumentoColaborador> DocumentosColaborador => Set<DocumentoColaborador>();
    public DbSet<Alerta> Alertas => Set<Alerta>();
    public DbSet<HistorialColaborador> HistorialColaborador => Set<HistorialColaborador>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureRoles(modelBuilder.Entity<Rol>());
        ConfigureUsuarios(modelBuilder.Entity<Usuario>());
        ConfigureEmpresas(modelBuilder.Entity<Empresa>());
        ConfigureDepartamentos(modelBuilder.Entity<Departamento>());
        ConfigureCargos(modelBuilder.Entity<Cargo>());
        ConfigureTiposContrato(modelBuilder.Entity<TipoContrato>());
        ConfigureEstatusColaborador(modelBuilder.Entity<EstatusColaborador>());
        ConfigureMotivosSalida(modelBuilder.Entity<MotivoSalida>());
        ConfigureTiposDocumento(modelBuilder.Entity<TipoDocumento>());
        ConfigureColaboradores(modelBuilder.Entity<Colaborador>());
        ConfigureDocumentosColaborador(modelBuilder.Entity<DocumentoColaborador>());
        ConfigureAlertas(modelBuilder.Entity<Alerta>());
        ConfigureHistorialColaborador(modelBuilder.Entity<HistorialColaborador>());

        SeedCatalogs(modelBuilder);
    }

    private static void ConfigureAudit<TEntity>(EntityTypeBuilder<TEntity> builder)
        where TEntity : AuditableEntity
    {
        builder.Property(entity => entity.CreatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(entity => entity.CreatedBy)
            .HasMaxLength(100);

        builder.Property(entity => entity.UpdatedBy)
            .HasMaxLength(100);

        builder.Property(entity => entity.IsActive)
            .HasDefaultValue(true);
    }

    private static void ConfigureRoles(EntityTypeBuilder<Rol> builder)
    {
        builder.ToTable("Roles");
        builder.HasKey(rol => rol.RolId);
        builder.Property(rol => rol.Nombre).IsRequired().HasMaxLength(50);
        builder.Property(rol => rol.Descripcion).HasMaxLength(250);
        builder.HasIndex(rol => rol.Nombre).IsUnique();
        ConfigureAudit(builder);
    }

    private static void ConfigureUsuarios(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("Usuarios");
        builder.HasKey(usuario => usuario.UsuarioId);
        builder.Property(usuario => usuario.NombreUsuario).IsRequired().HasMaxLength(100);
        builder.Property(usuario => usuario.Email).IsRequired().HasMaxLength(150);
        builder.Property(usuario => usuario.PasswordHash).IsRequired().HasMaxLength(500);
        builder.HasIndex(usuario => usuario.NombreUsuario).IsUnique();
        builder.HasIndex(usuario => usuario.Email).IsUnique();
        builder.HasOne(usuario => usuario.Rol)
            .WithMany(rol => rol.Usuarios)
            .HasForeignKey(usuario => usuario.RolId)
            .OnDelete(DeleteBehavior.Restrict);
        ConfigureAudit(builder);
    }

    private static void ConfigureEmpresas(EntityTypeBuilder<Empresa> builder)
    {
        builder.ToTable("Empresas");
        builder.HasKey(empresa => empresa.EmpresaId);
        builder.Property(empresa => empresa.Nombre).IsRequired().HasMaxLength(150);
        builder.Property(empresa => empresa.Ruc).HasMaxLength(50);
        ConfigureAudit(builder);
    }

    private static void ConfigureDepartamentos(EntityTypeBuilder<Departamento> builder)
    {
        builder.ToTable("Departamentos");
        builder.HasKey(departamento => departamento.DepartamentoId);
        builder.Property(departamento => departamento.Nombre).IsRequired().HasMaxLength(150);
        builder.HasIndex(departamento => new { departamento.EmpresaId, departamento.Nombre }).IsUnique();
        builder.HasOne(departamento => departamento.Empresa)
            .WithMany(empresa => empresa.Departamentos)
            .HasForeignKey(departamento => departamento.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);
        ConfigureAudit(builder);
    }

    private static void ConfigureCargos(EntityTypeBuilder<Cargo> builder)
    {
        builder.ToTable("Cargos");
        builder.HasKey(cargo => cargo.CargoId);
        builder.Property(cargo => cargo.Nombre).IsRequired().HasMaxLength(150);
        builder.HasIndex(cargo => new { cargo.DepartamentoId, cargo.Nombre }).IsUnique();
        builder.HasOne(cargo => cargo.Departamento)
            .WithMany(departamento => departamento.Cargos)
            .HasForeignKey(cargo => cargo.DepartamentoId)
            .OnDelete(DeleteBehavior.Restrict);
        ConfigureAudit(builder);
    }

    private static void ConfigureTiposContrato(EntityTypeBuilder<TipoContrato> builder)
    {
        builder.ToTable("TiposContrato");
        builder.HasKey(tipoContrato => tipoContrato.TipoContratoId);
        builder.Property(tipoContrato => tipoContrato.Nombre).IsRequired().HasMaxLength(100);
        builder.HasIndex(tipoContrato => tipoContrato.Nombre).IsUnique();
        ConfigureAudit(builder);
    }

    private static void ConfigureEstatusColaborador(EntityTypeBuilder<EstatusColaborador> builder)
    {
        builder.ToTable("EstatusColaborador");
        builder.HasKey(estatus => estatus.EstatusId);
        builder.Property(estatus => estatus.Nombre).IsRequired().HasMaxLength(100);
        builder.Property(estatus => estatus.Codigo).IsRequired().HasMaxLength(10);
        builder.HasIndex(estatus => estatus.Nombre).IsUnique();
        builder.HasIndex(estatus => estatus.Codigo).IsUnique();
        ConfigureAudit(builder);
    }

    private static void ConfigureMotivosSalida(EntityTypeBuilder<MotivoSalida> builder)
    {
        builder.ToTable("MotivosSalida");
        builder.HasKey(motivo => motivo.MotivoSalidaId);
        builder.Property(motivo => motivo.Nombre).IsRequired().HasMaxLength(150);
        builder.HasIndex(motivo => motivo.Nombre).IsUnique();
        ConfigureAudit(builder);
    }

    private static void ConfigureTiposDocumento(EntityTypeBuilder<TipoDocumento> builder)
    {
        builder.ToTable("TiposDocumento");
        builder.HasKey(tipoDocumento => tipoDocumento.TipoDocumentoId);
        builder.Property(tipoDocumento => tipoDocumento.Nombre).IsRequired().HasMaxLength(100);
        builder.HasIndex(tipoDocumento => tipoDocumento.Nombre).IsUnique();
        ConfigureAudit(builder);
    }

    private static void ConfigureColaboradores(EntityTypeBuilder<Colaborador> builder)
    {
        builder.ToTable("Colaboradores");
        builder.HasKey(colaborador => colaborador.ColaboradorId);
        builder.Property(colaborador => colaborador.NoEmpleado).IsRequired().HasMaxLength(50);
        builder.Property(colaborador => colaborador.Cedula).IsRequired().HasMaxLength(50);
        builder.Property(colaborador => colaborador.SeguroSocial).HasMaxLength(50);
        builder.Property(colaborador => colaborador.PrimerNombre).IsRequired().HasMaxLength(100);
        builder.Property(colaborador => colaborador.SegundoNombre).HasMaxLength(100);
        builder.Property(colaborador => colaborador.PrimerApellido).IsRequired().HasMaxLength(100);
        builder.Property(colaborador => colaborador.SegundoApellido).HasMaxLength(100);
        builder.Property(colaborador => colaborador.Sexo).HasMaxLength(20);
        builder.Property(colaborador => colaborador.Telefono).HasMaxLength(50);
        builder.Property(colaborador => colaborador.Email).HasMaxLength(150);
        builder.Property(colaborador => colaborador.Direccion).HasMaxLength(500);
        builder.Property(colaborador => colaborador.NumeroLicencia).HasMaxLength(100);
        builder.Property(colaborador => colaborador.TipoLicencia).HasMaxLength(100);
        builder.Property(colaborador => colaborador.Salario).HasColumnType("decimal(18,2)");
        builder.Property(colaborador => colaborador.Viaticos).HasColumnType("decimal(18,2)");
        builder.Property(colaborador => colaborador.GastosRepresentacion).HasColumnType("decimal(18,2)");
        builder.HasIndex(colaborador => colaborador.NoEmpleado).IsUnique();
        builder.HasIndex(colaborador => colaborador.Cedula).IsUnique();
        builder.HasIndex(colaborador => colaborador.EmpresaId);
        builder.HasIndex(colaborador => colaborador.DepartamentoId);
        builder.HasIndex(colaborador => colaborador.CargoId);
        builder.HasIndex(colaborador => colaborador.EstatusId);

        builder.HasOne(colaborador => colaborador.Empresa)
            .WithMany(empresa => empresa.Colaboradores)
            .HasForeignKey(colaborador => colaborador.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(colaborador => colaborador.Departamento)
            .WithMany(departamento => departamento.Colaboradores)
            .HasForeignKey(colaborador => colaborador.DepartamentoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(colaborador => colaborador.Cargo)
            .WithMany(cargo => cargo.Colaboradores)
            .HasForeignKey(colaborador => colaborador.CargoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(colaborador => colaborador.JefeInmediato)
            .WithMany(jefe => jefe.Subordinados)
            .HasForeignKey(colaborador => colaborador.JefeInmediatoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(colaborador => colaborador.TipoContrato)
            .WithMany(tipoContrato => tipoContrato.Colaboradores)
            .HasForeignKey(colaborador => colaborador.TipoContratoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(colaborador => colaborador.Estatus)
            .WithMany(estatus => estatus.Colaboradores)
            .HasForeignKey(colaborador => colaborador.EstatusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(colaborador => colaborador.MotivoSalida)
            .WithMany(motivo => motivo.Colaboradores)
            .HasForeignKey(colaborador => colaborador.MotivoSalidaId)
            .OnDelete(DeleteBehavior.Restrict);

        ConfigureAudit(builder);
    }

    private static void ConfigureDocumentosColaborador(EntityTypeBuilder<DocumentoColaborador> builder)
    {
        builder.ToTable("DocumentosColaborador");
        builder.HasKey(documento => documento.DocumentoColaboradorId);
        builder.Property(documento => documento.NombreArchivo).IsRequired().HasMaxLength(255);
        builder.Property(documento => documento.RutaArchivo).IsRequired().HasMaxLength(500);
        builder.Property(documento => documento.Observacion).HasMaxLength(500);
        builder.HasIndex(documento => documento.ColaboradorId);
        builder.HasIndex(documento => documento.TipoDocumentoId);
        builder.HasIndex(documento => documento.FechaVencimiento);

        builder.HasOne(documento => documento.Colaborador)
            .WithMany(colaborador => colaborador.Documentos)
            .HasForeignKey(documento => documento.ColaboradorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(documento => documento.TipoDocumento)
            .WithMany(tipoDocumento => tipoDocumento.Documentos)
            .HasForeignKey(documento => documento.TipoDocumentoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(documento => documento.UsuarioSubida)
            .WithMany(usuario => usuario.DocumentosSubidos)
            .HasForeignKey(documento => documento.SubidoPor)
            .OnDelete(DeleteBehavior.Restrict);

        ConfigureAudit(builder);
    }

    private static void ConfigureAlertas(EntityTypeBuilder<Alerta> builder)
    {
        builder.ToTable("Alertas");
        builder.HasKey(alerta => alerta.AlertaId);
        builder.Property(alerta => alerta.TipoAlerta)
            .HasConversion<int>()
            .IsRequired();
        builder.Property(alerta => alerta.EstadoAlerta)
            .HasConversion<int>()
            .IsRequired();
        builder.Property(alerta => alerta.Mensaje).IsRequired().HasMaxLength(500);
        builder.Property(alerta => alerta.ObservacionGestion).HasMaxLength(500);
        builder.HasIndex(alerta => alerta.EstadoAlerta);
        builder.HasIndex(alerta => alerta.FechaVencimiento);
        builder.HasIndex(alerta => new
            {
                alerta.TipoAlerta,
                alerta.ColaboradorId,
                alerta.DocumentoColaboradorId,
                alerta.FechaVencimiento
            })
            .IsUnique();

        builder.HasOne(alerta => alerta.Colaborador)
            .WithMany(colaborador => colaborador.Alertas)
            .HasForeignKey(alerta => alerta.ColaboradorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(alerta => alerta.DocumentoColaborador)
            .WithMany(documento => documento.Alertas)
            .HasForeignKey(alerta => alerta.DocumentoColaboradorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(alerta => alerta.UsuarioGestion)
            .WithMany(usuario => usuario.AlertasGestionadas)
            .HasForeignKey(alerta => alerta.GestionadaPor)
            .OnDelete(DeleteBehavior.Restrict);

        ConfigureAudit(builder);
    }

    private static void ConfigureHistorialColaborador(EntityTypeBuilder<HistorialColaborador> builder)
    {
        builder.ToTable("HistorialColaborador");
        builder.HasKey(historial => historial.HistorialColaboradorId);
        builder.Property(historial => historial.Accion).IsRequired().HasMaxLength(100);
        builder.Property(historial => historial.Campo).HasMaxLength(100);
        builder.Property(historial => historial.ValorAnterior).HasMaxLength(1000);
        builder.Property(historial => historial.ValorNuevo).HasMaxLength(1000);
        builder.Property(historial => historial.Observacion).HasMaxLength(500);
        builder.HasIndex(historial => historial.ColaboradorId);
        builder.HasIndex(historial => historial.UsuarioId);
        builder.HasIndex(historial => historial.Fecha);

        builder.HasOne(historial => historial.Colaborador)
            .WithMany(colaborador => colaborador.Historiales)
            .HasForeignKey(historial => historial.ColaboradorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(historial => historial.Usuario)
            .WithMany(usuario => usuario.Historiales)
            .HasForeignKey(historial => historial.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        ConfigureAudit(builder);
    }

    private static void SeedCatalogs(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Rol>().HasData(
            new Rol { RolId = 1, Nombre = "Admin", Descripcion = "Administrador del sistema", CreatedAt = SeedDate, IsActive = true },
            new Rol { RolId = 2, Nombre = "RRHH", Descripcion = "Gestion de Recursos Humanos", CreatedAt = SeedDate, IsActive = true },
            new Rol { RolId = 3, Nombre = "Supervisor", Descripcion = "Rol reservado para fases futuras", CreatedAt = SeedDate, IsActive = true },
            new Rol { RolId = 4, Nombre = "Consulta", Descripcion = "Rol reservado para fases futuras", CreatedAt = SeedDate, IsActive = true });

        modelBuilder.Entity<TipoContrato>().HasData(
            new TipoContrato { TipoContratoId = 1, Nombre = "Permanente", RequiereFechaVencimiento = false, CreatedAt = SeedDate, IsActive = true },
            new TipoContrato { TipoContratoId = 2, Nombre = "Eventual", RequiereFechaVencimiento = true, CreatedAt = SeedDate, IsActive = true });

        modelBuilder.Entity<EstatusColaborador>().HasData(
            new EstatusColaborador { EstatusId = 1, Nombre = "Activo", Codigo = "A", CreatedAt = SeedDate, IsActive = true },
            new EstatusColaborador { EstatusId = 2, Nombre = "Cesante", Codigo = "C", CreatedAt = SeedDate, IsActive = true },
            new EstatusColaborador { EstatusId = 3, Nombre = "Vacaciones", Codigo = "V", CreatedAt = SeedDate, IsActive = true },
            new EstatusColaborador { EstatusId = 4, Nombre = "Servicio", Codigo = "S", CreatedAt = SeedDate, IsActive = true },
            new EstatusColaborador { EstatusId = 5, Nombre = "Suspendido", Codigo = "SU", CreatedAt = SeedDate, IsActive = true });

        modelBuilder.Entity<MotivoSalida>().HasData(
            new MotivoSalida { MotivoSalidaId = 1, Nombre = "Renuncia", CreatedAt = SeedDate, IsActive = true },
            new MotivoSalida { MotivoSalidaId = 2, Nombre = "Despido", CreatedAt = SeedDate, IsActive = true },
            new MotivoSalida { MotivoSalidaId = 3, Nombre = "Mutuo acuerdo", CreatedAt = SeedDate, IsActive = true },
            new MotivoSalida { MotivoSalidaId = 4, Nombre = "Finalización de contrato", CreatedAt = SeedDate, IsActive = true },
            new MotivoSalida { MotivoSalidaId = 5, Nombre = "No aplica", CreatedAt = SeedDate, IsActive = true });

        modelBuilder.Entity<TipoDocumento>().HasData(
            new TipoDocumento { TipoDocumentoId = 1, Nombre = "Cédula", TieneVencimientoSugerido = true, CreatedAt = SeedDate, IsActive = true },
            new TipoDocumento { TipoDocumentoId = 2, Nombre = "Contrato", TieneVencimientoSugerido = true, CreatedAt = SeedDate, IsActive = true },
            new TipoDocumento { TipoDocumentoId = 3, Nombre = "Carnet", TieneVencimientoSugerido = true, CreatedAt = SeedDate, IsActive = true },
            new TipoDocumento { TipoDocumentoId = 4, Nombre = "Licencia", TieneVencimientoSugerido = true, CreatedAt = SeedDate, IsActive = true },
            new TipoDocumento { TipoDocumentoId = 5, Nombre = "Certificado", TieneVencimientoSugerido = true, CreatedAt = SeedDate, IsActive = true },
            new TipoDocumento { TipoDocumentoId = 6, Nombre = "Evaluación", TieneVencimientoSugerido = false, CreatedAt = SeedDate, IsActive = true },
            new TipoDocumento { TipoDocumentoId = 7, Nombre = "Carta disciplinaria", TieneVencimientoSugerido = false, CreatedAt = SeedDate, IsActive = true },
            new TipoDocumento { TipoDocumentoId = 8, Nombre = "Comprobante", TieneVencimientoSugerido = false, CreatedAt = SeedDate, IsActive = true },
            new TipoDocumento { TipoDocumentoId = 9, Nombre = "CV", TieneVencimientoSugerido = false, CreatedAt = SeedDate, IsActive = true },
            new TipoDocumento { TipoDocumentoId = 10, Nombre = "Otro", TieneVencimientoSugerido = false, CreatedAt = SeedDate, IsActive = true });
    }
}
