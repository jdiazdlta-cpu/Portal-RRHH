using System.Globalization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PortalRRHHFZ.Application.Common;
using PortalRRHHFZ.Application.DTOs.Colaboradores;
using PortalRRHHFZ.Application.Interfaces.Colaboradores;
using PortalRRHHFZ.Domain.Entities;
using PortalRRHHFZ.Infrastructure.Data;

namespace PortalRRHHFZ.Infrastructure.Services;

public sealed class ColaboradorService(AppDbContext dbContext) : IColaboradorService
{
    private static readonly string[] ImportantFields =
    [
        nameof(Colaborador.EmpresaId),
        nameof(Colaborador.DepartamentoId),
        nameof(Colaborador.CargoId),
        nameof(Colaborador.JefeInmediatoId),
        nameof(Colaborador.TipoContratoId),
        nameof(Colaborador.FechaVencimientoContrato),
        nameof(Colaborador.FechaVencimientoPeriodoProbatorio),
        nameof(Colaborador.EstatusId),
        nameof(Colaborador.Salario),
        nameof(Colaborador.Viaticos),
        nameof(Colaborador.GastosRepresentacion),
        nameof(Colaborador.FechaSalida),
        nameof(Colaborador.MotivoSalidaId),
        nameof(Colaborador.Vacante)
    ];

    public async Task<ApiResponse<IReadOnlyCollection<ColaboradorListDto>>> GetAllAsync(
        ColaboradorFilterRequest filters,
        CancellationToken cancellationToken = default)
    {
        var query = BaseQuery().AsNoTracking();

        if (filters.EmpresaId.HasValue)
        {
            query = query.Where(colaborador => colaborador.EmpresaId == filters.EmpresaId.Value);
        }

        if (filters.DepartamentoId.HasValue)
        {
            query = query.Where(colaborador => colaborador.DepartamentoId == filters.DepartamentoId.Value);
        }

        if (filters.CargoId.HasValue)
        {
            query = query.Where(colaborador => colaborador.CargoId == filters.CargoId.Value);
        }

        if (filters.EstatusId.HasValue)
        {
            query = query.Where(colaborador => colaborador.EstatusId == filters.EstatusId.Value);
        }

        if (filters.TipoContratoId.HasValue)
        {
            query = query.Where(colaborador => colaborador.TipoContratoId == filters.TipoContratoId.Value);
        }

        if (filters.IsActive.HasValue)
        {
            query = query.Where(colaborador => colaborador.IsActive == filters.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            var search = filters.Search.Trim().ToLower();
            query = query.Where(colaborador =>
                colaborador.NoEmpleado.ToLower().Contains(search)
                || colaborador.Cedula.ToLower().Contains(search)
                || colaborador.PrimerNombre.ToLower().Contains(search)
                || (colaborador.SegundoNombre != null && colaborador.SegundoNombre.ToLower().Contains(search))
                || colaborador.PrimerApellido.ToLower().Contains(search)
                || (colaborador.SegundoApellido != null && colaborador.SegundoApellido.ToLower().Contains(search))
                || (colaborador.Email != null && colaborador.Email.ToLower().Contains(search)));
        }

        var colaboradores = await query
            .OrderBy(colaborador => colaborador.PrimerApellido)
            .ThenBy(colaborador => colaborador.PrimerNombre)
            .ToListAsync(cancellationToken);

        return ApiResponse<IReadOnlyCollection<ColaboradorListDto>>.Ok(
            colaboradores.Select(ToListDto).ToList());
    }

    public async Task<ApiResponse<ColaboradorDetailDto>> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var colaborador = await BaseQuery()
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.ColaboradorId == id, cancellationToken);

        return colaborador is null
            ? ApiResponse<ColaboradorDetailDto>.Fail("Colaborador no encontrado.")
            : ApiResponse<ColaboradorDetailDto>.Ok(ToDetailDto(colaborador));
    }

    public async Task<ApiResponse<ColaboradorPerfilDto>> GetPerfilAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var colaborador = await BaseQuery()
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.ColaboradorId == id, cancellationToken);

        return colaborador is null
            ? ApiResponse<ColaboradorPerfilDto>.Fail("Colaborador no encontrado.")
            : ApiResponse<ColaboradorPerfilDto>.Ok(ToPerfilDto(colaborador));
    }

    public async Task<ApiResponse<ColaboradorDetailDto>> CreateAsync(
        CreateColaboradorRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        var currentUser = GetCurrentUser(principal);

        if (currentUser.UserId is null)
        {
            return ApiResponse<ColaboradorDetailDto>.Fail("Usuario autenticado invalido.");
        }

        var validationErrors = await ValidateAsync(request, null, cancellationToken);

        if (validationErrors.Count > 0)
        {
            return ApiResponse<ColaboradorDetailDto>.Fail("No fue posible crear el colaborador.", validationErrors);
        }

        var colaborador = new Colaborador();
        ApplyRequest(colaborador, request);
        colaborador.CreatedAt = DateTime.UtcNow;
        colaborador.CreatedBy = currentUser.UserName;
        colaborador.IsActive = true;

        dbContext.Colaboradores.Add(colaborador);
        await dbContext.SaveChangesAsync(cancellationToken);

        AddHistory(
            colaborador.ColaboradorId,
            currentUser.UserId.Value,
            "Crear",
            null,
            null,
            null,
            "Colaborador creado.",
            currentUser.UserName);

        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetDetailResponseAsync(colaborador.ColaboradorId, "Colaborador creado correctamente.", cancellationToken);
    }

    public async Task<ApiResponse<ColaboradorDetailDto>> UpdateAsync(
        int id,
        UpdateColaboradorRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        var currentUser = GetCurrentUser(principal);

        if (currentUser.UserId is null)
        {
            return ApiResponse<ColaboradorDetailDto>.Fail("Usuario autenticado invalido.");
        }

        var colaborador = await dbContext.Colaboradores
            .SingleOrDefaultAsync(item => item.ColaboradorId == id, cancellationToken);

        if (colaborador is null)
        {
            return ApiResponse<ColaboradorDetailDto>.Fail("Colaborador no encontrado.");
        }

        var validationErrors = await ValidateAsync(request, id, cancellationToken);

        if (validationErrors.Count > 0)
        {
            return ApiResponse<ColaboradorDetailDto>.Fail("No fue posible actualizar el colaborador.", validationErrors);
        }

        var before = SnapshotImportantFields(colaborador);

        ApplyRequest(colaborador, request);
        colaborador.UpdatedAt = DateTime.UtcNow;
        colaborador.UpdatedBy = currentUser.UserName;

        var after = SnapshotImportantFields(colaborador);
        AddChangeHistory(colaborador.ColaboradorId, currentUser, before, after);

        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetDetailResponseAsync(colaborador.ColaboradorId, "Colaborador actualizado correctamente.", cancellationToken);
    }

    public async Task<ApiResponse<ColaboradorDetailDto>> ActivateAsync(
        int id,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        return await ChangeActiveStateAsync(id, true, principal, cancellationToken);
    }

    public async Task<ApiResponse<ColaboradorDetailDto>> DeactivateAsync(
        int id,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        return await ChangeActiveStateAsync(id, false, principal, cancellationToken);
    }

    public async Task<ApiResponse<IReadOnlyCollection<HistorialColaboradorDto>>> GetHistorialAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var exists = await dbContext.Colaboradores.AnyAsync(
            colaborador => colaborador.ColaboradorId == id,
            cancellationToken);

        if (!exists)
        {
            return ApiResponse<IReadOnlyCollection<HistorialColaboradorDto>>.Fail("Colaborador no encontrado.");
        }

        var historial = await dbContext.HistorialColaborador
            .Include(item => item.Usuario)
            .AsNoTracking()
            .Where(item => item.ColaboradorId == id)
            .OrderByDescending(item => item.Fecha)
            .Select(item => new HistorialColaboradorDto
            {
                HistorialColaboradorId = item.HistorialColaboradorId,
                ColaboradorId = item.ColaboradorId,
                UsuarioId = item.UsuarioId,
                UsuarioNombre = item.Usuario.NombreUsuario,
                Accion = item.Accion,
                Campo = item.Campo,
                ValorAnterior = item.ValorAnterior,
                ValorNuevo = item.ValorNuevo,
                Fecha = item.Fecha,
                Observacion = item.Observacion
            })
            .ToListAsync(cancellationToken);

        return ApiResponse<IReadOnlyCollection<HistorialColaboradorDto>>.Ok(historial);
    }

    private IQueryable<Colaborador> BaseQuery()
    {
        return dbContext.Colaboradores
            .Include(colaborador => colaborador.Empresa)
            .Include(colaborador => colaborador.Departamento)
            .Include(colaborador => colaborador.Cargo)
            .Include(colaborador => colaborador.JefeInmediato)
            .Include(colaborador => colaborador.TipoContrato)
            .Include(colaborador => colaborador.Estatus)
            .Include(colaborador => colaborador.MotivoSalida);
    }

    private async Task<ApiResponse<ColaboradorDetailDto>> ChangeActiveStateAsync(
        int id,
        bool isActive,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var currentUser = GetCurrentUser(principal);

        if (currentUser.UserId is null)
        {
            return ApiResponse<ColaboradorDetailDto>.Fail("Usuario autenticado invalido.");
        }

        var colaborador = await dbContext.Colaboradores
            .SingleOrDefaultAsync(item => item.ColaboradorId == id, cancellationToken);

        if (colaborador is null)
        {
            return ApiResponse<ColaboradorDetailDto>.Fail("Colaborador no encontrado.");
        }

        var previousValue = colaborador.IsActive.ToString(CultureInfo.InvariantCulture);
        colaborador.IsActive = isActive;
        colaborador.UpdatedAt = DateTime.UtcNow;
        colaborador.UpdatedBy = currentUser.UserName;

        AddHistory(
            colaborador.ColaboradorId,
            currentUser.UserId.Value,
            isActive ? "Activar" : "Desactivar",
            nameof(Colaborador.IsActive),
            previousValue,
            isActive.ToString(CultureInfo.InvariantCulture),
            isActive ? "Colaborador activado." : "Colaborador desactivado.",
            currentUser.UserName);

        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetDetailResponseAsync(
            colaborador.ColaboradorId,
            isActive ? "Colaborador activado correctamente." : "Colaborador desactivado correctamente.",
            cancellationToken);
    }

    private async Task<ApiResponse<ColaboradorDetailDto>> GetDetailResponseAsync(
        int id,
        string message,
        CancellationToken cancellationToken)
    {
        var colaborador = await BaseQuery()
            .AsNoTracking()
            .SingleAsync(item => item.ColaboradorId == id, cancellationToken);

        return ApiResponse<ColaboradorDetailDto>.Ok(ToDetailDto(colaborador), message);
    }

    private async Task<List<string>> ValidateAsync(
        CreateColaboradorRequest request,
        int? colaboradorId,
        CancellationToken cancellationToken)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.NoEmpleado))
        {
            errors.Add("NoEmpleado es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(request.Cedula))
        {
            errors.Add("Cedula es obligatoria.");
        }

        if (string.IsNullOrWhiteSpace(request.PrimerNombre))
        {
            errors.Add("PrimerNombre es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(request.PrimerApellido))
        {
            errors.Add("PrimerApellido es obligatorio.");
        }

        if (!request.FechaIngreso.HasValue)
        {
            errors.Add("FechaIngreso es obligatoria.");
        }

        if (request.EmpresaId <= 0)
        {
            errors.Add("EmpresaId es obligatorio.");
        }

        if (request.DepartamentoId <= 0)
        {
            errors.Add("DepartamentoId es obligatorio.");
        }

        if (request.CargoId <= 0)
        {
            errors.Add("CargoId es obligatorio.");
        }

        if (request.TipoContratoId <= 0)
        {
            errors.Add("TipoContratoId es obligatorio.");
        }

        if (request.EstatusId <= 0)
        {
            errors.Add("EstatusId es obligatorio.");
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        var noEmpleado = request.NoEmpleado.Trim().ToLower();
        var cedula = request.Cedula.Trim().ToLower();

        if (await dbContext.Colaboradores.AnyAsync(
                colaborador =>
                    colaborador.NoEmpleado.ToLower() == noEmpleado
                    && (!colaboradorId.HasValue || colaborador.ColaboradorId != colaboradorId.Value),
                cancellationToken))
        {
            errors.Add("NoEmpleado ya esta registrado.");
        }

        if (await dbContext.Colaboradores.AnyAsync(
                colaborador =>
                    colaborador.Cedula.ToLower() == cedula
                    && (!colaboradorId.HasValue || colaborador.ColaboradorId != colaboradorId.Value),
                cancellationToken))
        {
            errors.Add("Cedula ya esta registrada.");
        }

        var empresaActiva = await dbContext.Empresas.AnyAsync(
            empresa => empresa.EmpresaId == request.EmpresaId && empresa.IsActive,
            cancellationToken);

        if (!empresaActiva)
        {
            errors.Add("EmpresaId no existe o esta inactiva.");
        }

        var departamento = await dbContext.Departamentos
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.DepartamentoId == request.DepartamentoId, cancellationToken);

        if (departamento is null || !departamento.IsActive)
        {
            errors.Add("DepartamentoId no existe o esta inactivo.");
        }
        else if (departamento.EmpresaId != request.EmpresaId)
        {
            errors.Add("DepartamentoId no pertenece a EmpresaId.");
        }

        var cargo = await dbContext.Cargos
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.CargoId == request.CargoId, cancellationToken);

        if (cargo is null || !cargo.IsActive)
        {
            errors.Add("CargoId no existe o esta inactivo.");
        }
        else if (cargo.DepartamentoId != request.DepartamentoId)
        {
            errors.Add("CargoId no pertenece a DepartamentoId.");
        }

        var tipoContrato = await dbContext.TiposContrato
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.TipoContratoId == request.TipoContratoId, cancellationToken);

        if (tipoContrato is null || !tipoContrato.IsActive)
        {
            errors.Add("TipoContratoId no existe o esta inactivo.");
        }
        else if (tipoContrato.RequiereFechaVencimiento && !request.FechaVencimientoContrato.HasValue)
        {
            errors.Add("FechaVencimientoContrato es obligatoria para el tipo de contrato seleccionado.");
        }

        var estatus = await dbContext.EstatusColaborador
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.EstatusId == request.EstatusId, cancellationToken);

        if (estatus is null || !estatus.IsActive)
        {
            errors.Add("EstatusId no existe o esta inactivo.");
        }
        else if (estatus.Codigo.Equals("C", StringComparison.OrdinalIgnoreCase) && !request.FechaSalida.HasValue)
        {
            errors.Add("FechaSalida es requerida cuando el estatus es Cesante.");
        }

        if (request.MotivoSalidaId.HasValue)
        {
            var motivoSalidaActivo = await dbContext.MotivosSalida.AnyAsync(
                motivo => motivo.MotivoSalidaId == request.MotivoSalidaId.Value && motivo.IsActive,
                cancellationToken);

            if (!motivoSalidaActivo)
            {
                errors.Add("MotivoSalidaId no existe o esta inactivo.");
            }
        }

        if (request.JefeInmediatoId.HasValue)
        {
            if (colaboradorId.HasValue && request.JefeInmediatoId.Value == colaboradorId.Value)
            {
                errors.Add("JefeInmediatoId no puede ser el mismo colaborador.");
            }
            else
            {
                var jefeActivo = await dbContext.Colaboradores.AnyAsync(
                    jefe => jefe.ColaboradorId == request.JefeInmediatoId.Value && jefe.IsActive,
                    cancellationToken);

                if (!jefeActivo)
                {
                    errors.Add("JefeInmediatoId no existe o esta inactivo.");
                }
            }
        }

        if (request.TieneLicencia)
        {
            if (string.IsNullOrWhiteSpace(request.NumeroLicencia))
            {
                errors.Add("NumeroLicencia es obligatorio cuando TieneLicencia es true.");
            }

            if (string.IsNullOrWhiteSpace(request.TipoLicencia))
            {
                errors.Add("TipoLicencia es obligatorio cuando TieneLicencia es true.");
            }

            if (!request.FechaVencimientoLicencia.HasValue)
            {
                errors.Add("FechaVencimientoLicencia es obligatoria cuando TieneLicencia es true.");
            }
        }

        return errors;
    }

    private static void ApplyRequest(Colaborador colaborador, CreateColaboradorRequest request)
    {
        colaborador.NoEmpleado = request.NoEmpleado.Trim();
        colaborador.Cedula = request.Cedula.Trim();
        colaborador.FechaVencimientoCedula = request.FechaVencimientoCedula;
        colaborador.SeguroSocial = NormalizeNullable(request.SeguroSocial);
        colaborador.PrimerNombre = request.PrimerNombre.Trim();
        colaborador.SegundoNombre = NormalizeNullable(request.SegundoNombre);
        colaborador.PrimerApellido = request.PrimerApellido.Trim();
        colaborador.SegundoApellido = NormalizeNullable(request.SegundoApellido);
        colaborador.Sexo = NormalizeNullable(request.Sexo);
        colaborador.Telefono = NormalizeNullable(request.Telefono);
        colaborador.Email = NormalizeNullable(request.Email)?.ToLowerInvariant();
        colaborador.FechaNacimiento = request.FechaNacimiento;
        colaborador.Direccion = NormalizeNullable(request.Direccion);
        colaborador.EmpresaId = request.EmpresaId;
        colaborador.DepartamentoId = request.DepartamentoId;
        colaborador.CargoId = request.CargoId;
        colaborador.JefeInmediatoId = request.JefeInmediatoId;
        colaborador.FechaIngreso = request.FechaIngreso!.Value;
        colaborador.TipoContratoId = request.TipoContratoId;
        colaborador.FechaVencimientoContrato = request.FechaVencimientoContrato;
        colaborador.FechaVencimientoPeriodoProbatorio = request.FechaVencimientoPeriodoProbatorio;
        colaborador.TieneLicencia = request.TieneLicencia;
        colaborador.NumeroLicencia = request.TieneLicencia ? NormalizeNullable(request.NumeroLicencia) : null;
        colaborador.TipoLicencia = request.TieneLicencia ? NormalizeNullable(request.TipoLicencia) : null;
        colaborador.FechaVencimientoLicencia = request.TieneLicencia ? request.FechaVencimientoLicencia : null;
        colaborador.EstatusId = request.EstatusId;
        colaborador.Salario = request.Salario;
        colaborador.Viaticos = request.Viaticos;
        colaborador.GastosRepresentacion = request.GastosRepresentacion;
        colaborador.FechaSalida = request.FechaSalida;
        colaborador.MotivoSalidaId = request.MotivoSalidaId;
        colaborador.Vacante = request.Vacante;
        colaborador.UltimaVacacion = request.UltimaVacacion;
    }

    private static ColaboradorListDto ToListDto(Colaborador colaborador)
    {
        return new ColaboradorListDto
        {
            ColaboradorId = colaborador.ColaboradorId,
            NoEmpleado = colaborador.NoEmpleado,
            Cedula = colaborador.Cedula,
            NombreCompleto = GetNombreCompleto(colaborador),
            Email = colaborador.Email,
            EmpresaId = colaborador.EmpresaId,
            EmpresaNombre = colaborador.Empresa.Nombre,
            DepartamentoId = colaborador.DepartamentoId,
            DepartamentoNombre = colaborador.Departamento.Nombre,
            CargoId = colaborador.CargoId,
            CargoNombre = colaborador.Cargo.Nombre,
            EstatusId = colaborador.EstatusId,
            EstatusNombre = colaborador.Estatus.Nombre,
            TipoContratoId = colaborador.TipoContratoId,
            TipoContratoNombre = colaborador.TipoContrato.Nombre,
            FechaIngreso = colaborador.FechaIngreso,
            IsActive = colaborador.IsActive
        };
    }

    private static ColaboradorDetailDto ToDetailDto(Colaborador colaborador)
    {
        return new ColaboradorDetailDto
        {
            ColaboradorId = colaborador.ColaboradorId,
            NoEmpleado = colaborador.NoEmpleado,
            Cedula = colaborador.Cedula,
            FechaVencimientoCedula = colaborador.FechaVencimientoCedula,
            SeguroSocial = colaborador.SeguroSocial,
            PrimerNombre = colaborador.PrimerNombre,
            SegundoNombre = colaborador.SegundoNombre,
            PrimerApellido = colaborador.PrimerApellido,
            SegundoApellido = colaborador.SegundoApellido,
            NombreCompleto = GetNombreCompleto(colaborador),
            Sexo = colaborador.Sexo,
            Telefono = colaborador.Telefono,
            Email = colaborador.Email,
            FechaNacimiento = colaborador.FechaNacimiento,
            Direccion = colaborador.Direccion,
            EmpresaId = colaborador.EmpresaId,
            EmpresaNombre = colaborador.Empresa.Nombre,
            DepartamentoId = colaborador.DepartamentoId,
            DepartamentoNombre = colaborador.Departamento.Nombre,
            CargoId = colaborador.CargoId,
            CargoNombre = colaborador.Cargo.Nombre,
            JefeInmediatoId = colaborador.JefeInmediatoId,
            JefeInmediatoNombre = colaborador.JefeInmediato is null ? null : GetNombreCompleto(colaborador.JefeInmediato),
            FechaIngreso = colaborador.FechaIngreso,
            TipoContratoId = colaborador.TipoContratoId,
            TipoContratoNombre = colaborador.TipoContrato.Nombre,
            FechaVencimientoContrato = colaborador.FechaVencimientoContrato,
            FechaVencimientoPeriodoProbatorio = colaborador.FechaVencimientoPeriodoProbatorio,
            TieneLicencia = colaborador.TieneLicencia,
            NumeroLicencia = colaborador.NumeroLicencia,
            TipoLicencia = colaborador.TipoLicencia,
            FechaVencimientoLicencia = colaborador.FechaVencimientoLicencia,
            EstatusId = colaborador.EstatusId,
            EstatusNombre = colaborador.Estatus.Nombre,
            Salario = colaborador.Salario,
            Viaticos = colaborador.Viaticos,
            GastosRepresentacion = colaborador.GastosRepresentacion,
            FechaSalida = colaborador.FechaSalida,
            MotivoSalidaId = colaborador.MotivoSalidaId,
            MotivoSalidaNombre = colaborador.MotivoSalida?.Nombre,
            Vacante = colaborador.Vacante,
            UltimaVacacion = colaborador.UltimaVacacion,
            CreatedAt = colaborador.CreatedAt,
            UpdatedAt = colaborador.UpdatedAt,
            CreatedBy = colaborador.CreatedBy,
            UpdatedBy = colaborador.UpdatedBy,
            IsActive = colaborador.IsActive
        };
    }

    private static ColaboradorPerfilDto ToPerfilDto(Colaborador colaborador)
    {
        return new ColaboradorPerfilDto
        {
            DatosPersonales = new ColaboradorPerfilDatosPersonalesDto
            {
                ColaboradorId = colaborador.ColaboradorId,
                NoEmpleado = colaborador.NoEmpleado,
                Cedula = colaborador.Cedula,
                NombreCompleto = GetNombreCompleto(colaborador),
                Sexo = colaborador.Sexo,
                Telefono = colaborador.Telefono,
                Email = colaborador.Email,
                FechaNacimiento = colaborador.FechaNacimiento,
                Direccion = colaborador.Direccion
            },
            DatosLaborales = new ColaboradorPerfilDatosLaboralesDto
            {
                EmpresaId = colaborador.EmpresaId,
                EmpresaNombre = colaborador.Empresa.Nombre,
                DepartamentoId = colaborador.DepartamentoId,
                DepartamentoNombre = colaborador.Departamento.Nombre,
                CargoId = colaborador.CargoId,
                CargoNombre = colaborador.Cargo.Nombre,
                JefeInmediatoId = colaborador.JefeInmediatoId,
                JefeInmediatoNombre = colaborador.JefeInmediato is null ? null : GetNombreCompleto(colaborador.JefeInmediato),
                EstatusId = colaborador.EstatusId,
                EstatusNombre = colaborador.Estatus.Nombre,
                FechaIngreso = colaborador.FechaIngreso,
                FechaSalida = colaborador.FechaSalida,
                MotivoSalidaId = colaborador.MotivoSalidaId,
                MotivoSalidaNombre = colaborador.MotivoSalida?.Nombre,
                Vacante = colaborador.Vacante,
                IsActive = colaborador.IsActive
            },
            Contrato = new ColaboradorPerfilContratoDto
            {
                TipoContratoId = colaborador.TipoContratoId,
                TipoContratoNombre = colaborador.TipoContrato.Nombre,
                FechaVencimientoContrato = colaborador.FechaVencimientoContrato,
                FechaVencimientoPeriodoProbatorio = colaborador.FechaVencimientoPeriodoProbatorio
            },
            Vencimientos = new ColaboradorPerfilVencimientosDto
            {
                FechaVencimientoCedula = colaborador.FechaVencimientoCedula,
                TieneLicencia = colaborador.TieneLicencia,
                NumeroLicencia = colaborador.NumeroLicencia,
                TipoLicencia = colaborador.TipoLicencia,
                FechaVencimientoLicencia = colaborador.FechaVencimientoLicencia,
                FechaVencimientoContrato = colaborador.FechaVencimientoContrato,
                FechaVencimientoPeriodoProbatorio = colaborador.FechaVencimientoPeriodoProbatorio
            },
            Compensacion = new ColaboradorPerfilCompensacionDto
            {
                Salario = colaborador.Salario,
                Viaticos = colaborador.Viaticos,
                GastosRepresentacion = colaborador.GastosRepresentacion
            }
        };
    }

    private static Dictionary<string, string?> SnapshotImportantFields(Colaborador colaborador)
    {
        return ImportantFields.ToDictionary(
            field => field,
            field => FormatValue(field switch
            {
                nameof(Colaborador.EmpresaId) => colaborador.EmpresaId,
                nameof(Colaborador.DepartamentoId) => colaborador.DepartamentoId,
                nameof(Colaborador.CargoId) => colaborador.CargoId,
                nameof(Colaborador.JefeInmediatoId) => colaborador.JefeInmediatoId,
                nameof(Colaborador.TipoContratoId) => colaborador.TipoContratoId,
                nameof(Colaborador.FechaVencimientoContrato) => colaborador.FechaVencimientoContrato,
                nameof(Colaborador.FechaVencimientoPeriodoProbatorio) => colaborador.FechaVencimientoPeriodoProbatorio,
                nameof(Colaborador.EstatusId) => colaborador.EstatusId,
                nameof(Colaborador.Salario) => colaborador.Salario,
                nameof(Colaborador.Viaticos) => colaborador.Viaticos,
                nameof(Colaborador.GastosRepresentacion) => colaborador.GastosRepresentacion,
                nameof(Colaborador.FechaSalida) => colaborador.FechaSalida,
                nameof(Colaborador.MotivoSalidaId) => colaborador.MotivoSalidaId,
                nameof(Colaborador.Vacante) => colaborador.Vacante,
                _ => null
            }));
    }

    private void AddChangeHistory(
        int colaboradorId,
        CurrentUser currentUser,
        Dictionary<string, string?> before,
        Dictionary<string, string?> after)
    {
        foreach (var field in ImportantFields)
        {
            if (before[field] == after[field])
            {
                continue;
            }

            AddHistory(
                colaboradorId,
                currentUser.UserId!.Value,
                "Actualizar",
                field,
                before[field],
                after[field],
                $"Campo {field} actualizado.",
                currentUser.UserName);
        }
    }

    private void AddHistory(
        int colaboradorId,
        int usuarioId,
        string accion,
        string? campo,
        string? valorAnterior,
        string? valorNuevo,
        string? observacion,
        string? currentUser)
    {
        dbContext.HistorialColaborador.Add(new HistorialColaborador
        {
            ColaboradorId = colaboradorId,
            UsuarioId = usuarioId,
            Accion = accion,
            Campo = campo,
            ValorAnterior = valorAnterior,
            ValorNuevo = valorNuevo,
            Fecha = DateTime.UtcNow,
            Observacion = observacion,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = currentUser,
            IsActive = true
        });
    }

    private static CurrentUser GetCurrentUser(ClaimsPrincipal principal)
    {
        var userIdValue = principal.FindFirstValue("UserId")
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

        return new CurrentUser(
            int.TryParse(userIdValue, out var userId) ? userId : null,
            principal.Identity?.Name);
    }

    private static string GetNombreCompleto(Colaborador colaborador)
    {
        return string.Join(
            " ",
            new[]
            {
                colaborador.PrimerNombre,
                colaborador.SegundoNombre,
                colaborador.PrimerApellido,
                colaborador.SegundoApellido
            }.Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? FormatValue(object? value)
    {
        return value switch
        {
            null => null,
            DateTime dateTime => dateTime.ToString("O", CultureInfo.InvariantCulture),
            decimal decimalValue => decimalValue.ToString(CultureInfo.InvariantCulture),
            bool boolValue => boolValue.ToString(CultureInfo.InvariantCulture),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString()
        };
    }

    private sealed record CurrentUser(int? UserId, string? UserName);
}
