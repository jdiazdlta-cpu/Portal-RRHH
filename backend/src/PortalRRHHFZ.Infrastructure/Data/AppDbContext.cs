using Microsoft.EntityFrameworkCore;
using PortalRRHHFZ.Domain.Entities;
using PortalRRHHFZ.Domain.Enums;

namespace PortalRRHHFZ.Infrastructure.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
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

        var seedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<Rol>(entity =>
        {
            entity.ToTable("Roles");
            entity.HasKey(x => x.RolId);
            entity.Property(x => x.Nombre).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Descripcion).HasMaxLength(250);
            entity.HasIndex(x => x.Nombre).IsUnique();
            entity.HasData(
                new Rol { RolId = 1, Nombre = "Admin", Descripcion = "Acceso completo a V1", CreatedAt = seedDate, IsActive = true },
                new Rol { RolId = 2, Nombre = "RRHH", Descripcion = "Gestion operativa de RRHH V1", CreatedAt = seedDate, IsActive = true },
                new Rol { RolId = 3, Nombre = "Supervisor", Descripcion = "Reservado para fases futuras", CreatedAt = seedDate, IsActive = true },
                new Rol { RolId = 4, Nombre = "Consulta", Descripcion = "Reservado para fases futuras", CreatedAt = seedDate, IsActive = true });
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.ToTable("Usuarios");
            entity.HasKey(x => x.UsuarioId);
            entity.Property(x => x.NombreUsuario).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(150).IsRequired();
            entity.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
            entity.HasIndex(x => x.NombreUsuario).IsUnique();
            entity.HasIndex(x => x.Email).IsUnique();
            entity.HasOne(x => x.Rol).WithMany(x => x.Usuarios).HasForeignKey(x => x.RolId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Empresa>(entity =>
        {
            entity.ToTable("Empresas");
            entity.HasKey(x => x.EmpresaId);
            entity.Property(x => x.Nombre).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Ruc).HasMaxLength(50);
        });

        modelBuilder.Entity<Departamento>(entity =>
        {
            entity.ToTable("Departamentos");
            entity.HasKey(x => x.DepartamentoId);
            entity.Property(x => x.Nombre).HasMaxLength(150).IsRequired();
            entity.HasOne(x => x.Empresa).WithMany(x => x.Departamentos).HasForeignKey(x => x.EmpresaId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Cargo>(entity =>
        {
            entity.ToTable("Cargos");
            entity.HasKey(x => x.CargoId);
            entity.Property(x => x.Nombre).HasMaxLength(150).IsRequired();
            entity.HasOne(x => x.Departamento).WithMany(x => x.Cargos).HasForeignKey(x => x.DepartamentoId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TipoContrato>(entity =>
        {
            entity.ToTable("TiposContrato");
            entity.HasKey(x => x.TipoContratoId);
            entity.Property(x => x.Nombre).HasMaxLength(80).IsRequired();
            entity.HasIndex(x => x.Nombre).IsUnique();
            entity.HasData(
                new TipoContrato { TipoContratoId = 1, Nombre = "Permanente", RequiereFechaVencimiento = false, CreatedAt = seedDate, IsActive = true },
                new TipoContrato { TipoContratoId = 2, Nombre = "Eventual", RequiereFechaVencimiento = true, CreatedAt = seedDate, IsActive = true });
        });

        modelBuilder.Entity<EstatusColaborador>(entity =>
        {
            entity.ToTable("EstatusColaborador");
            entity.HasKey(x => x.EstatusId);
            entity.Property(x => x.Nombre).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Codigo).HasMaxLength(10).IsRequired();
            entity.HasIndex(x => x.Codigo).IsUnique();
            entity.HasData(
                new EstatusColaborador { EstatusId = 1, Nombre = "Activo", Codigo = "A", CreatedAt = seedDate, IsActive = true },
                new EstatusColaborador { EstatusId = 2, Nombre = "Cesante", Codigo = "C", CreatedAt = seedDate, IsActive = true },
                new EstatusColaborador { EstatusId = 3, Nombre = "Vacaciones", Codigo = "V", CreatedAt = seedDate, IsActive = true },
                new EstatusColaborador { EstatusId = 4, Nombre = "Servicio", Codigo = "S", CreatedAt = seedDate, IsActive = true },
                new EstatusColaborador { EstatusId = 5, Nombre = "Suspendido", Codigo = "SU", CreatedAt = seedDate, IsActive = true });
        });

        modelBuilder.Entity<MotivoSalida>(entity =>
        {
            entity.ToTable("MotivosSalida");
            entity.HasKey(x => x.MotivoSalidaId);
            entity.Property(x => x.Nombre).HasMaxLength(120).IsRequired();
            entity.HasData(
                new MotivoSalida { MotivoSalidaId = 1, Nombre = "Renuncia", CreatedAt = seedDate, IsActive = true },
                new MotivoSalida { MotivoSalidaId = 2, Nombre = "Despido", CreatedAt = seedDate, IsActive = true },
                new MotivoSalida { MotivoSalidaId = 3, Nombre = "Mutuo acuerdo", CreatedAt = seedDate, IsActive = true },
                new MotivoSalida { MotivoSalidaId = 4, Nombre = "Finalizacion de contrato", CreatedAt = seedDate, IsActive = true },
                new MotivoSalida { MotivoSalidaId = 5, Nombre = "No aplica", CreatedAt = seedDate, IsActive = true });
        });

        modelBuilder.Entity<TipoDocumento>(entity =>
        {
            entity.ToTable("TiposDocumento");
            entity.HasKey(x => x.TipoDocumentoId);
            entity.Property(x => x.Nombre).HasMaxLength(120).IsRequired();
            entity.HasData(
                new TipoDocumento { TipoDocumentoId = 1, Nombre = "Cedula", TieneVencimientoSugerido = true, CreatedAt = seedDate, IsActive = true },
                new TipoDocumento { TipoDocumentoId = 2, Nombre = "Contrato", TieneVencimientoSugerido = true, CreatedAt = seedDate, IsActive = true },
                new TipoDocumento { TipoDocumentoId = 3, Nombre = "Carnet", TieneVencimientoSugerido = true, CreatedAt = seedDate, IsActive = true },
                new TipoDocumento { TipoDocumentoId = 4, Nombre = "Licencia", TieneVencimientoSugerido = true, CreatedAt = seedDate, IsActive = true },
                new TipoDocumento { TipoDocumentoId = 5, Nombre = "Certificado", TieneVencimientoSugerido = true, CreatedAt = seedDate, IsActive = true },
                new TipoDocumento { TipoDocumentoId = 6, Nombre = "Evaluacion", TieneVencimientoSugerido = false, CreatedAt = seedDate, IsActive = true },
                new TipoDocumento { TipoDocumentoId = 7, Nombre = "Carta disciplinaria", TieneVencimientoSugerido = false, CreatedAt = seedDate, IsActive = true },
                new TipoDocumento { TipoDocumentoId = 8, Nombre = "Comprobante", TieneVencimientoSugerido = false, CreatedAt = seedDate, IsActive = true },
                new TipoDocumento { TipoDocumentoId = 9, Nombre = "CV", TieneVencimientoSugerido = false, CreatedAt = seedDate, IsActive = true },
                new TipoDocumento { TipoDocumentoId = 10, Nombre = "Otro", TieneVencimientoSugerido = false, CreatedAt = seedDate, IsActive = true });
        });

        modelBuilder.Entity<Colaborador>(entity =>
        {
            entity.ToTable("Colaboradores");
            entity.HasKey(x => x.ColaboradorId);
            entity.Property(x => x.NoEmpleado).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Cedula).HasMaxLength(50).IsRequired();
            entity.Property(x => x.SeguroSocial).HasMaxLength(50);
            entity.Property(x => x.PrimerNombre).HasMaxLength(80).IsRequired();
            entity.Property(x => x.SegundoNombre).HasMaxLength(80);
            entity.Property(x => x.PrimerApellido).HasMaxLength(80).IsRequired();
            entity.Property(x => x.SegundoApellido).HasMaxLength(80);
            entity.Property(x => x.Sexo).HasMaxLength(20);
            entity.Property(x => x.Telefono).HasMaxLength(50);
            entity.Property(x => x.Email).HasMaxLength(150);
            entity.Property(x => x.Direccion).HasMaxLength(500);
            entity.Property(x => x.NumeroLicencia).HasMaxLength(80);
            entity.Property(x => x.TipoLicencia).HasMaxLength(80);
            entity.Property(x => x.Salario).HasColumnType("decimal(18,2)");
            entity.Property(x => x.Viaticos).HasColumnType("decimal(18,2)");
            entity.Property(x => x.GastosRepresentacion).HasColumnType("decimal(18,2)");
            entity.HasIndex(x => x.NoEmpleado).IsUnique();
            entity.HasIndex(x => x.Cedula).IsUnique();
            entity.HasOne(x => x.Empresa).WithMany(x => x.Colaboradores).HasForeignKey(x => x.EmpresaId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Departamento).WithMany(x => x.Colaboradores).HasForeignKey(x => x.DepartamentoId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Cargo).WithMany(x => x.Colaboradores).HasForeignKey(x => x.CargoId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.TipoContrato).WithMany(x => x.Colaboradores).HasForeignKey(x => x.TipoContratoId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Estatus).WithMany(x => x.Colaboradores).HasForeignKey(x => x.EstatusId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.MotivoSalida).WithMany(x => x.Colaboradores).HasForeignKey(x => x.MotivoSalidaId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.JefeInmediato).WithMany(x => x.Subordinados).HasForeignKey(x => x.JefeInmediatoId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DocumentoColaborador>(entity =>
        {
            entity.ToTable("DocumentosColaborador");
            entity.HasKey(x => x.DocumentoColaboradorId);
            entity.Property(x => x.NombreArchivo).HasMaxLength(255).IsRequired();
            entity.Property(x => x.RutaArchivo).HasMaxLength(600).IsRequired();
            entity.Property(x => x.Observacion).HasMaxLength(1000);
            entity.HasOne(x => x.Colaborador).WithMany(x => x.Documentos).HasForeignKey(x => x.ColaboradorId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.TipoDocumento).WithMany(x => x.Documentos).HasForeignKey(x => x.TipoDocumentoId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.UsuarioSubio).WithMany(x => x.DocumentosSubidos).HasForeignKey(x => x.SubidoPor).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Alerta>(entity =>
        {
            entity.ToTable("Alertas");
            entity.HasKey(x => x.AlertaId);
            entity.Property(x => x.TipoAlerta).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(x => x.EstadoAlerta).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(x => x.Mensaje).HasMaxLength(500).IsRequired();
            entity.Property(x => x.ObservacionGestion).HasMaxLength(1000);
            entity.HasIndex(x => new { x.TipoAlerta, x.ColaboradorId, x.DocumentoColaboradorId, x.FechaVencimiento }).IsUnique();
            entity.HasOne(x => x.Colaborador).WithMany(x => x.Alertas).HasForeignKey(x => x.ColaboradorId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.DocumentoColaborador).WithMany(x => x.Alertas).HasForeignKey(x => x.DocumentoColaboradorId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.UsuarioGestiono).WithMany(x => x.AlertasGestionadas).HasForeignKey(x => x.GestionadaPor).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<HistorialColaborador>(entity =>
        {
            entity.ToTable("HistorialColaborador");
            entity.HasKey(x => x.HistorialColaboradorId);
            entity.Property(x => x.Accion).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Campo).HasMaxLength(120);
            entity.Property(x => x.ValorAnterior).HasMaxLength(1000);
            entity.Property(x => x.ValorNuevo).HasMaxLength(1000);
            entity.Property(x => x.Observacion).HasMaxLength(1000);
            entity.HasOne(x => x.Colaborador).WithMany(x => x.Historiales).HasForeignKey(x => x.ColaboradorId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Usuario).WithMany(x => x.Historiales).HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.CreatedAt == default)
                {
                    entry.Entity.CreatedAt = now;
                }
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
                entry.Property(x => x.CreatedAt).IsModified = false;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
