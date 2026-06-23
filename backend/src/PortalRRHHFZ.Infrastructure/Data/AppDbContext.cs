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
    public DbSet<Solicitud> Solicitudes => Set<Solicitud>();
    public DbSet<RequisicionPersonal> RequisicionesPersonal => Set<RequisicionPersonal>();
    public DbSet<AccionPersonal> AccionesPersonal => Set<AccionPersonal>();
    public DbSet<AccionPersonalCambioAplicado> AccionPersonalCambiosAplicados => Set<AccionPersonalCambioAplicado>();
    public DbSet<SolicitudAprobacion> SolicitudAprobaciones => Set<SolicitudAprobacion>();
    public DbSet<SolicitudHistorial> SolicitudHistorial => Set<SolicitudHistorial>();
    public DbSet<Organigrama> Organigramas => Set<Organigrama>();
    public DbSet<OrganigramaNodo> OrganigramaNodos => Set<OrganigramaNodo>();
    public DbSet<DepartamentoResponsable> DepartamentoResponsables => Set<DepartamentoResponsable>();
    public DbSet<OrganigramaHistorialCambio> OrganigramaHistorialCambios => Set<OrganigramaHistorialCambio>();

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

        modelBuilder.Entity<Solicitud>(entity =>
        {
            entity.ToTable("Solicitudes");
            entity.HasKey(x => x.SolicitudId);
            entity.Property(x => x.CodigoSolicitud).HasMaxLength(30).IsRequired();
            entity.Property(x => x.TipoSolicitud).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(x => x.Estado).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(x => x.Justificacion).HasMaxLength(2000);
            entity.Property(x => x.Observaciones).HasMaxLength(2000);
            entity.HasIndex(x => x.CodigoSolicitud).IsUnique();
            entity.HasIndex(x => new { x.TipoSolicitud, x.Estado });
            entity.HasOne(x => x.SolicitanteUsuario).WithMany().HasForeignKey(x => x.SolicitanteUsuarioId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Colaborador).WithMany().HasForeignKey(x => x.ColaboradorId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Empresa).WithMany().HasForeignKey(x => x.EmpresaId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Departamento).WithMany().HasForeignKey(x => x.DepartamentoId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Cargo).WithMany().HasForeignKey(x => x.CargoId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RequisicionPersonal>(entity =>
        {
            entity.ToTable("RequisicionesPersonal");
            entity.HasKey(x => x.RequisicionPersonalId);
            entity.Property(x => x.CargoSolicitado).HasMaxLength(180).IsRequired();
            entity.Property(x => x.DependenciaJerarquica).HasMaxLength(250);
            entity.Property(x => x.PrincipalesResponsabilidades).HasMaxLength(3000);
            entity.Property(x => x.FuncionesEspecificas).HasMaxLength(3000);
            entity.Property(x => x.EquipoACargo).HasMaxLength(1000);
            entity.Property(x => x.CentroTrabajo).HasMaxLength(180);
            entity.Property(x => x.Salario).HasColumnType("decimal(18,2)");
            entity.Property(x => x.GastoRepresentacion).HasColumnType("decimal(18,2)");
            entity.Property(x => x.SalarioVariable).HasColumnType("decimal(18,2)");
            entity.Property(x => x.OtrosConceptos).HasMaxLength(1000);
            entity.Property(x => x.NombrePersonaReemplazada).HasMaxLength(180);
            entity.Property(x => x.PeriodoPrueba).HasMaxLength(120);
            entity.Property(x => x.FormacionRequerida).HasMaxLength(2000);
            entity.Property(x => x.FormacionComplementaria).HasMaxLength(2000);
            entity.Property(x => x.ConocimientosTecnicos).HasMaxLength(2000);
            entity.Property(x => x.ConocimientosValorados).HasMaxLength(2000);
            entity.Property(x => x.IdiomaNivel).HasMaxLength(300);
            entity.Property(x => x.FuncionesExperiencia).HasMaxLength(2000);
            entity.Property(x => x.AreaSectorExperiencia).HasMaxLength(1000);
            entity.Property(x => x.ExperienciaValorable).HasMaxLength(2000);
            entity.Property(x => x.SexoPreferido).HasMaxLength(40);
            entity.Property(x => x.CaracteristicasPersonales).HasMaxLength(2000);
            entity.Property(x => x.SolicitadoPorTexto).HasMaxLength(180);
            entity.Property(x => x.AutorizadoPorTexto).HasMaxLength(180);
            entity.HasIndex(x => x.SolicitudId).IsUnique();
            entity.HasOne(x => x.Solicitud).WithOne(x => x.RequisicionPersonal).HasForeignKey<RequisicionPersonal>(x => x.SolicitudId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.DepartamentoSolicitado).WithMany().HasForeignKey(x => x.DepartamentoSolicitadoId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ColaboradorReemplazado).WithMany().HasForeignKey(x => x.ColaboradorReemplazadoId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.TipoContrato).WithMany().HasForeignKey(x => x.TipoContratoId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AccionPersonal>(entity =>
        {
            entity.ToTable("AccionesPersonal");
            entity.HasKey(x => x.AccionPersonalId);
            entity.Property(x => x.TipoAccion).HasConversion<string>().HasMaxLength(80).IsRequired();
            entity.Property(x => x.Justificacion).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.Observaciones).HasMaxLength(2000);
            entity.Property(x => x.NombreColaboradorSnapshot).HasMaxLength(220);
            entity.Property(x => x.NoEmpleadoSnapshot).HasMaxLength(50);
            entity.Property(x => x.CedulaSnapshot).HasMaxLength(50);
            entity.Property(x => x.SalarioActual).HasColumnType("decimal(18,2)");
            entity.Property(x => x.ViaticosActual).HasColumnType("decimal(18,2)");
            entity.Property(x => x.GastosRepresentacionActual).HasColumnType("decimal(18,2)");
            entity.Property(x => x.QuienReemplaza).HasMaxLength(180);
            entity.Property(x => x.SalarioNuevo).HasColumnType("decimal(18,2)");
            entity.Property(x => x.ViaticosNuevo).HasColumnType("decimal(18,2)");
            entity.Property(x => x.GastosRepresentacionNuevo).HasColumnType("decimal(18,2)");
            entity.Property(x => x.OtrosBeneficios).HasMaxLength(1000);
            entity.Property(x => x.SalarioAnterior).HasColumnType("decimal(18,2)");
            entity.Property(x => x.SalarioNuevoAjuste).HasColumnType("decimal(18,2)");
            entity.Property(x => x.AjustePorMes).HasColumnType("decimal(18,2)");
            entity.Property(x => x.MotivoAjuste).HasMaxLength(1000);
            entity.Property(x => x.TipoLicenciaAccion).HasMaxLength(120);
            entity.Property(x => x.EspecificacionLicencia).HasMaxLength(1000);
            entity.Property(x => x.TipoFinalizacion).HasMaxLength(120);
            entity.Property(x => x.Puntualidad).HasMaxLength(20);
            entity.Property(x => x.Honestidad).HasMaxLength(20);
            entity.Property(x => x.TrabajoEquipo).HasMaxLength(20);
            entity.Property(x => x.Productividad).HasMaxLength(20);
            entity.Property(x => x.Iniciativa).HasMaxLength(20);
            entity.Property(x => x.RespetoJefe).HasMaxLength(20);
            entity.Property(x => x.RespetoCompaneros).HasMaxLength(20);
            entity.Property(x => x.ResultadoEjecucion).HasMaxLength(2000);
            entity.Property(x => x.ErrorEjecucion).HasMaxLength(2000);
            entity.HasIndex(x => x.SolicitudId).IsUnique();
            entity.HasIndex(x => new { x.TipoAccion, x.Ejecutada });
            entity.HasIndex(x => x.ColaboradorId);
            entity.HasOne(x => x.Solicitud).WithOne(x => x.AccionPersonal).HasForeignKey<AccionPersonal>(x => x.SolicitudId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Colaborador).WithMany().HasForeignKey(x => x.ColaboradorId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.EmpresaActual).WithMany().HasForeignKey(x => x.EmpresaActualId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.DepartamentoActual).WithMany().HasForeignKey(x => x.DepartamentoActualId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CargoActual).WithMany().HasForeignKey(x => x.CargoActualId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.JefeActual).WithMany().HasForeignKey(x => x.JefeActualId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.TipoContratoActual).WithMany().HasForeignKey(x => x.TipoContratoActualId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.EstatusActual).WithMany().HasForeignKey(x => x.EstatusActualId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.TipoContratoNuevo).WithMany().HasForeignKey(x => x.TipoContratoNuevoId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CargoNuevo).WithMany().HasForeignKey(x => x.CargoNuevoId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.DepartamentoNuevo).WithMany().HasForeignKey(x => x.DepartamentoNuevoId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.EmpresaNueva).WithMany().HasForeignKey(x => x.EmpresaNuevaId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.JefeNuevo).WithMany().HasForeignKey(x => x.JefeNuevoId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CargoTrasladoActual).WithMany().HasForeignKey(x => x.CargoTrasladoActualId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CargoTrasladoNuevo).WithMany().HasForeignKey(x => x.CargoTrasladoNuevoId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.DepartamentoTrasladoActual).WithMany().HasForeignKey(x => x.DepartamentoTrasladoActualId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.DepartamentoTrasladoNuevo).WithMany().HasForeignKey(x => x.DepartamentoTrasladoNuevoId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.EmpresaTrasladoActual).WithMany().HasForeignKey(x => x.EmpresaTrasladoActualId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.EmpresaTrasladoNueva).WithMany().HasForeignKey(x => x.EmpresaTrasladoNuevaId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.JefeTrasladoNuevo).WithMany().HasForeignKey(x => x.JefeTrasladoNuevoId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.MotivoSalida).WithMany().HasForeignKey(x => x.MotivoSalidaId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.EjecutadaPorUsuario).WithMany().HasForeignKey(x => x.EjecutadaPorUsuarioId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AccionPersonalCambioAplicado>(entity =>
        {
            entity.ToTable("AccionPersonalCambiosAplicados");
            entity.HasKey(x => x.AccionPersonalCambioAplicadoId);
            entity.Property(x => x.Campo).HasMaxLength(120).IsRequired();
            entity.Property(x => x.ValorAnterior).HasMaxLength(1000);
            entity.Property(x => x.ValorNuevo).HasMaxLength(1000);
            entity.HasIndex(x => new { x.AccionPersonalId, x.Fecha });
            entity.HasOne(x => x.AccionPersonal).WithMany(x => x.CambiosAplicados).HasForeignKey(x => x.AccionPersonalId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SolicitudAprobacion>(entity =>
        {
            entity.ToTable("SolicitudAprobaciones");
            entity.HasKey(x => x.SolicitudAprobacionId);
            entity.Property(x => x.Etapa).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(x => x.RolAprobador).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Estado).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(x => x.Comentario).HasMaxLength(1000);
            entity.HasIndex(x => new { x.SolicitudId, x.Orden }).IsUnique();
            entity.HasOne(x => x.Solicitud).WithMany(x => x.Aprobaciones).HasForeignKey(x => x.SolicitudId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.UsuarioAprobador).WithMany().HasForeignKey(x => x.UsuarioAprobadorId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ColaboradorAprobador).WithMany().HasForeignKey(x => x.ColaboradorAprobadorId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.DepartamentoResponsable).WithMany().HasForeignKey(x => x.DepartamentoResponsableId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SolicitudHistorial>(entity =>
        {
            entity.ToTable("SolicitudHistorial");
            entity.HasKey(x => x.SolicitudHistorialId);
            entity.Property(x => x.Accion).HasMaxLength(80).IsRequired();
            entity.Property(x => x.EstadoAnterior).HasConversion<string>().HasMaxLength(50);
            entity.Property(x => x.EstadoNuevo).HasConversion<string>().HasMaxLength(50);
            entity.Property(x => x.Comentario).HasMaxLength(1000);
            entity.HasIndex(x => new { x.SolicitudId, x.Fecha });
            entity.HasOne(x => x.Solicitud).WithMany(x => x.Historial).HasForeignKey(x => x.SolicitudId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Organigrama>(entity =>
        {
            entity.ToTable("Organigramas");
            entity.HasKey(x => x.OrganigramaId);
            entity.Property(x => x.Nombre).HasMaxLength(180).IsRequired();
            entity.Property(x => x.Descripcion).HasMaxLength(1000);
            entity.HasIndex(x => new { x.EmpresaId, x.IsActive });
            entity.HasOne(x => x.Empresa).WithMany().HasForeignKey(x => x.EmpresaId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrganigramaNodo>(entity =>
        {
            entity.ToTable("OrganigramaNodos");
            entity.HasKey(x => x.OrganigramaNodoId);
            entity.Property(x => x.NombreNodo).HasMaxLength(180).IsRequired();
            entity.Property(x => x.Descripcion).HasMaxLength(1000);
            entity.HasIndex(x => new { x.OrganigramaId, x.Nivel, x.Orden });
            entity.HasIndex(x => x.NodoPadreId);
            entity.HasOne(x => x.Organigrama).WithMany(x => x.Nodos).HasForeignKey(x => x.OrganigramaId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Empresa).WithMany().HasForeignKey(x => x.EmpresaId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Departamento).WithMany().HasForeignKey(x => x.DepartamentoId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Cargo).WithMany().HasForeignKey(x => x.CargoId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.NodoPadre).WithMany(x => x.Hijos).HasForeignKey(x => x.NodoPadreId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DepartamentoResponsable>(entity =>
        {
            entity.ToTable("DepartamentoResponsables");
            entity.HasKey(x => x.DepartamentoResponsableId);
            entity.Property(x => x.TipoResponsable).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Observacion).HasMaxLength(1000);
            entity.HasIndex(x => new { x.EmpresaId, x.DepartamentoId, x.TipoResponsable, x.IsActive });
            entity.HasIndex(x => new { x.DepartamentoId, x.PuedeAprobarSolicitudes, x.IsActive });
            entity.HasOne(x => x.Empresa).WithMany().HasForeignKey(x => x.EmpresaId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Departamento).WithMany().HasForeignKey(x => x.DepartamentoId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ColaboradorResponsable).WithMany().HasForeignKey(x => x.ColaboradorResponsableId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.UsuarioResponsable).WithMany().HasForeignKey(x => x.UsuarioResponsableId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrganigramaHistorialCambio>(entity =>
        {
            entity.ToTable("OrganigramaHistorialCambios");
            entity.HasKey(x => x.OrganigramaHistorialCambioId);
            entity.Property(x => x.Entidad).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Accion).HasMaxLength(80).IsRequired();
            entity.Property(x => x.ValorAnterior).HasMaxLength(2000);
            entity.Property(x => x.ValorNuevo).HasMaxLength(2000);
            entity.Property(x => x.Comentario).HasMaxLength(1000);
            entity.HasIndex(x => new { x.Entidad, x.EntidadId, x.Fecha });
            entity.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
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
