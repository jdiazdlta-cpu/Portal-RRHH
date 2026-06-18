using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortalRRHHFZ.Application.Common;
using PortalRRHHFZ.Application.DTOs;
using PortalRRHHFZ.Domain.Constants;
using PortalRRHHFZ.Domain.Entities;
using PortalRRHHFZ.Infrastructure.Data;

namespace PortalRRHHFZ.Api.Controllers;

[ApiController]
[Authorize(Policy = AppPolicies.RequireAdminOrRRHH)]
[Route("api/colaboradores")]
public sealed class ColaboradoresController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] int? empresaId,
        [FromQuery] int? departamentoId,
        [FromQuery] int? cargoId,
        [FromQuery] int? estatusId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.Colaboradores
            .Include(x => x.Empresa)
            .Include(x => x.Departamento)
            .Include(x => x.Cargo)
            .Include(x => x.Estatus)
            .AsNoTracking()
            .AsQueryable();

        if (empresaId.HasValue)
        {
            query = query.Where(x => x.EmpresaId == empresaId);
        }

        if (departamentoId.HasValue)
        {
            query = query.Where(x => x.DepartamentoId == departamentoId);
        }

        if (cargoId.HasValue)
        {
            query = query.Where(x => x.CargoId == cargoId);
        }

        if (estatusId.HasValue)
        {
            query = query.Where(x => x.EstatusId == estatusId);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x =>
                x.NoEmpleado.Contains(term) ||
                x.Cedula.Contains(term) ||
                x.PrimerNombre.Contains(term) ||
                x.PrimerApellido.Contains(term) ||
                (x.Email != null && x.Email.Contains(term)));
        }

        var total = await query.CountAsync(cancellationToken);
        var colaboradores = await query
            .OrderBy(x => x.PrimerApellido)
            .ThenBy(x => x.PrimerNombre)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var data = new PagedResult<ColaboradorListDto>(colaboradores.Select(x => x.ToListDto()).ToList(), total, page, pageSize);
        return Ok(ApiResponse<PagedResult<ColaboradorListDto>>.Ok(data));
    }

    [HttpGet("posibles-jefes")]
    public async Task<IActionResult> PosiblesJefes(
        [FromQuery] int? empresaId,
        [FromQuery] int? departamentoId,
        [FromQuery] int? cargoId,
        [FromQuery] int? excludeColaboradorId,
        CancellationToken cancellationToken)
    {
        var query = db.Colaboradores
            .Include(x => x.Empresa)
            .Include(x => x.Departamento)
            .Include(x => x.Cargo)
            .AsNoTracking()
            .Where(x => x.IsActive);

        if (empresaId.HasValue)
        {
            query = query.Where(x => x.EmpresaId == empresaId.Value);
        }

        if (departamentoId.HasValue)
        {
            query = query.Where(x => x.DepartamentoId == departamentoId.Value);
        }

        if (cargoId.HasValue)
        {
            query = query.Where(x => x.CargoId == cargoId.Value);
        }

        if (excludeColaboradorId.HasValue)
        {
            query = query.Where(x => x.ColaboradorId != excludeColaboradorId.Value);
        }

        var colaboradores = await query
            .OrderBy(x => x.PrimerApellido)
            .ThenBy(x => x.PrimerNombre)
            .ToListAsync(cancellationToken);

        var data = colaboradores
            .Select(x => new PosibleJefeDto(
                x.ColaboradorId,
                x.NoEmpleado,
                x.NombreCompleto(),
                x.Empresa.Nombre,
                x.Departamento.Nombre,
                x.Cargo.Nombre))
            .ToList();

        return Ok(ApiResponse<List<PosibleJefeDto>>.Ok(data));
    }

    [HttpGet("{id:int}")]
    public Task<IActionResult> GetById(int id, CancellationToken cancellationToken) => GetDetalle(id, cancellationToken);

    [HttpGet("{id:int}/perfil")]
    public Task<IActionResult> Perfil(int id, CancellationToken cancellationToken) => GetDetalle(id, cancellationToken);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UpsertColaboradorRequest request, CancellationToken cancellationToken)
    {
        var validation = await ValidateAsync(request, null, cancellationToken);
        if (validation is not null)
        {
            return BadRequest(ApiResponse<object>.Fail(validation));
        }

        var colaborador = new Colaborador { CreatedBy = User.Identity?.Name };
        Apply(colaborador, request);
        db.Colaboradores.Add(colaborador);
        await db.SaveChangesAsync(cancellationToken);

        db.HistorialColaborador.Add(new HistorialColaborador
        {
            ColaboradorId = colaborador.ColaboradorId,
            UsuarioId = User.CurrentUserId(),
            Accion = "CREACION",
            ValorNuevo = colaborador.NoEmpleado,
            Observacion = "Creacion de colaborador",
            CreatedBy = User.Identity?.Name
        });
        await db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = colaborador.ColaboradorId }, ApiResponse<object>.Ok(new { colaborador.ColaboradorId }, "Colaborador creado."));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpsertColaboradorRequest request, CancellationToken cancellationToken)
    {
        var colaborador = await db.Colaboradores.FirstOrDefaultAsync(x => x.ColaboradorId == id, cancellationToken);
        if (colaborador is null)
        {
            return NotFound(ApiResponse<object>.Fail("Colaborador no encontrado."));
        }

        var validation = await ValidateAsync(request, id, cancellationToken);
        if (validation is not null)
        {
            return BadRequest(ApiResponse<object>.Fail(validation));
        }

        var before = colaborador.NoEmpleado;
        var jefeAnteriorId = colaborador.JefeInmediatoId;
        var jefeAnterior = await GetJefeDisplayAsync(jefeAnteriorId, cancellationToken);
        var jefeNuevo = await GetJefeDisplayAsync(request.JefeInmediatoId, cancellationToken);
        Apply(colaborador, request);
        colaborador.UpdatedBy = User.Identity?.Name;
        db.HistorialColaborador.Add(new HistorialColaborador
        {
            ColaboradorId = colaborador.ColaboradorId,
            UsuarioId = User.CurrentUserId(),
            Accion = "ACTUALIZACION",
            Campo = "Registro",
            ValorAnterior = before,
            ValorNuevo = colaborador.NoEmpleado,
            Observacion = "Actualizacion manual de colaborador",
            CreatedBy = User.Identity?.Name
        });

        if (jefeAnteriorId != request.JefeInmediatoId)
        {
            db.HistorialColaborador.Add(new HistorialColaborador
            {
                ColaboradorId = colaborador.ColaboradorId,
                UsuarioId = User.CurrentUserId(),
                Accion = "ACTUALIZACION",
                Campo = "JefeInmediatoId",
                ValorAnterior = FormatJefeValue(jefeAnteriorId, jefeAnterior),
                ValorNuevo = FormatJefeValue(request.JefeInmediatoId, jefeNuevo),
                Observacion = "Cambio de jefe inmediato",
                CreatedBy = User.Identity?.Name
            });
        }

        await db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { colaborador.ColaboradorId }, "Colaborador actualizado."));
    }

    [HttpPatch("{id:int}/activar")]
    public Task<IActionResult> Activar(int id, CancellationToken cancellationToken) => Toggle(id, true, cancellationToken);

    [HttpPatch("{id:int}/desactivar")]
    public Task<IActionResult> Desactivar(int id, CancellationToken cancellationToken) => Toggle(id, false, cancellationToken);

    [HttpGet("{id:int}/historial")]
    public async Task<IActionResult> Historial(int id, CancellationToken cancellationToken)
    {
        if (!await db.Colaboradores.AnyAsync(x => x.ColaboradorId == id, cancellationToken))
        {
            return NotFound(ApiResponse<object>.Fail("Colaborador no encontrado."));
        }

        var data = await db.HistorialColaborador
            .Include(x => x.Usuario)
            .Where(x => x.ColaboradorId == id)
            .OrderByDescending(x => x.Fecha)
            .Select(x => new HistorialDto(x.HistorialColaboradorId, x.Usuario.NombreUsuario, x.Accion, x.Campo, x.ValorAnterior, x.ValorNuevo, x.Fecha, x.Observacion))
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<List<HistorialDto>>.Ok(data));
    }

    private async Task<IActionResult> GetDetalle(int id, CancellationToken cancellationToken)
    {
        var colaborador = await db.Colaboradores.IncludeDetalle().AsNoTracking().FirstOrDefaultAsync(x => x.ColaboradorId == id, cancellationToken);
        if (colaborador is null)
        {
            return NotFound(ApiResponse<object>.Fail("Colaborador no encontrado."));
        }

        var nombreCompleto = colaborador.NombreCompleto();
        var documentos = colaborador.Documentos
            .OrderByDescending(x => x.FechaCarga)
            .Select(x => x.ToDto())
            .ToList();
        var alertas = colaborador.Alertas
            .OrderBy(x => x.FechaVencimiento)
            .Select(x => new AlertaDto(
                x.AlertaId,
                x.TipoAlerta.ToString(),
                x.EstadoAlerta.ToString(),
                x.ColaboradorId,
                nombreCompleto,
                x.DocumentoColaboradorId,
                x.FechaVencimiento,
                x.Mensaje,
                x.FechaGeneracion,
                x.FechaGestion,
                x.ObservacionGestion))
            .ToList();

        var dto = new ColaboradorDetalleDto(
            colaborador.ColaboradorId,
            colaborador.NoEmpleado,
            colaborador.Cedula,
            colaborador.FechaVencimientoCedula,
            colaborador.SeguroSocial,
            colaborador.PrimerNombre,
            colaborador.SegundoNombre,
            colaborador.PrimerApellido,
            colaborador.SegundoApellido,
            nombreCompleto,
            colaborador.Sexo,
            colaborador.Telefono,
            colaborador.Email,
            colaborador.FechaNacimiento,
            colaborador.Direccion,
            colaborador.EmpresaId,
            colaborador.Empresa.Nombre,
            colaborador.DepartamentoId,
            colaborador.Departamento.Nombre,
            colaborador.CargoId,
            colaborador.Cargo.Nombre,
            colaborador.JefeInmediatoId,
            colaborador.JefeInmediato?.NombreCompleto(),
            colaborador.FechaIngreso,
            colaborador.TipoContratoId,
            colaborador.TipoContrato.Nombre,
            colaborador.FechaVencimientoContrato,
            colaborador.FechaVencimientoPeriodoProbatorio,
            colaborador.TieneLicencia,
            colaborador.NumeroLicencia,
            colaborador.TipoLicencia,
            colaborador.FechaVencimientoLicencia,
            colaborador.EstatusId,
            colaborador.Estatus.Nombre,
            colaborador.Salario,
            colaborador.Viaticos,
            colaborador.GastosRepresentacion,
            colaborador.FechaSalida,
            colaborador.MotivoSalidaId,
            colaborador.MotivoSalida?.Nombre,
            colaborador.Vacante,
            colaborador.UltimaVacacion,
            colaborador.IsActive,
            documentos,
            alertas);

        return Ok(ApiResponse<ColaboradorDetalleDto>.Ok(dto));
    }

    private async Task<IActionResult> Toggle(int id, bool active, CancellationToken cancellationToken)
    {
        var colaborador = await db.Colaboradores.FirstOrDefaultAsync(x => x.ColaboradorId == id, cancellationToken);
        if (colaborador is null)
        {
            return NotFound(ApiResponse<object>.Fail("Colaborador no encontrado."));
        }

        colaborador.IsActive = active;
        colaborador.UpdatedBy = User.Identity?.Name;
        db.HistorialColaborador.Add(new HistorialColaborador
        {
            ColaboradorId = colaborador.ColaboradorId,
            UsuarioId = User.CurrentUserId(),
            Accion = active ? "ACTIVACION" : "DESACTIVACION",
            Campo = "IsActive",
            ValorNuevo = active.ToString(),
            CreatedBy = User.Identity?.Name
        });
        await db.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { colaborador.ColaboradorId, colaborador.IsActive }, active ? "Colaborador activado." : "Colaborador desactivado."));
    }

    private async Task<string?> ValidateAsync(UpsertColaboradorRequest request, int? colaboradorId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.NoEmpleado))
        {
            return "NoEmpleado es obligatorio.";
        }

        if (string.IsNullOrWhiteSpace(request.Cedula))
        {
            return "Cedula es obligatoria.";
        }

        if (string.IsNullOrWhiteSpace(request.PrimerNombre) || string.IsNullOrWhiteSpace(request.PrimerApellido))
        {
            return "Primer nombre y primer apellido son obligatorios.";
        }

        if (await db.Colaboradores.AnyAsync(x => x.NoEmpleado == request.NoEmpleado.Trim() && (!colaboradorId.HasValue || x.ColaboradorId != colaboradorId), cancellationToken))
        {
            return "NoEmpleado ya existe.";
        }

        if (await db.Colaboradores.AnyAsync(x => x.Cedula == request.Cedula.Trim() && (!colaboradorId.HasValue || x.ColaboradorId != colaboradorId), cancellationToken))
        {
            return "Cedula ya existe.";
        }

        var departamento = await db.Departamentos.AsNoTracking().FirstOrDefaultAsync(x => x.DepartamentoId == request.DepartamentoId && x.IsActive, cancellationToken);
        var cargo = await db.Cargos.AsNoTracking().FirstOrDefaultAsync(x => x.CargoId == request.CargoId && x.IsActive, cancellationToken);
        if (!await db.Empresas.AnyAsync(x => x.EmpresaId == request.EmpresaId && x.IsActive, cancellationToken))
        {
            return "Empresa no valida.";
        }

        if (departamento is null || departamento.EmpresaId != request.EmpresaId)
        {
            return "Departamento no pertenece a la empresa seleccionada.";
        }

        if (cargo is null || cargo.DepartamentoId != request.DepartamentoId)
        {
            return "Cargo no pertenece al departamento seleccionado.";
        }

        var tipoContrato = await db.TiposContrato.AsNoTracking().FirstOrDefaultAsync(x => x.TipoContratoId == request.TipoContratoId && x.IsActive, cancellationToken);
        if (tipoContrato is null)
        {
            return "Tipo de contrato no valido.";
        }

        if (tipoContrato.RequiereFechaVencimiento && !request.FechaVencimientoContrato.HasValue)
        {
            return "El contrato eventual requiere fecha de vencimiento.";
        }

        if (!await db.EstatusColaborador.AnyAsync(x => x.EstatusId == request.EstatusId && x.IsActive, cancellationToken))
        {
            return "Estatus no valido.";
        }

        if (request.MotivoSalidaId.HasValue && !await db.MotivosSalida.AnyAsync(x => x.MotivoSalidaId == request.MotivoSalidaId && x.IsActive, cancellationToken))
        {
            return "Motivo de salida no valido.";
        }

        if (request.JefeInmediatoId.HasValue)
        {
            if (colaboradorId.HasValue && request.JefeInmediatoId.Value == colaboradorId.Value)
            {
                return "El jefe inmediato no puede ser el mismo colaborador.";
            }

            var jefe = await db.Colaboradores.AsNoTracking().FirstOrDefaultAsync(x => x.ColaboradorId == request.JefeInmediatoId.Value, cancellationToken);
            if (jefe is null)
            {
                return "Jefe inmediato no encontrado.";
            }

            if (!jefe.IsActive)
            {
                return "Jefe inmediato inactivo.";
            }
        }

        if (request.TieneLicencia && (string.IsNullOrWhiteSpace(request.TipoLicencia) || !request.FechaVencimientoLicencia.HasValue))
        {
            return "Si tiene licencia, debe indicar tipo y fecha de vencimiento.";
        }

        return null;
    }

    private static void Apply(Colaborador colaborador, UpsertColaboradorRequest request)
    {
        colaborador.NoEmpleado = request.NoEmpleado.Trim();
        colaborador.Cedula = request.Cedula.Trim();
        colaborador.FechaVencimientoCedula = request.FechaVencimientoCedula;
        colaborador.SeguroSocial = request.SeguroSocial?.Trim();
        colaborador.PrimerNombre = request.PrimerNombre.Trim();
        colaborador.SegundoNombre = request.SegundoNombre?.Trim();
        colaborador.PrimerApellido = request.PrimerApellido.Trim();
        colaborador.SegundoApellido = request.SegundoApellido?.Trim();
        colaborador.Sexo = request.Sexo?.Trim();
        colaborador.Telefono = request.Telefono?.Trim();
        colaborador.Email = request.Email?.Trim();
        colaborador.FechaNacimiento = request.FechaNacimiento;
        colaborador.Direccion = request.Direccion?.Trim();
        colaborador.EmpresaId = request.EmpresaId;
        colaborador.DepartamentoId = request.DepartamentoId;
        colaborador.CargoId = request.CargoId;
        colaborador.JefeInmediatoId = request.JefeInmediatoId;
        colaborador.FechaIngreso = request.FechaIngreso;
        colaborador.TipoContratoId = request.TipoContratoId;
        colaborador.FechaVencimientoContrato = request.FechaVencimientoContrato;
        colaborador.FechaVencimientoPeriodoProbatorio = request.FechaVencimientoPeriodoProbatorio;
        colaborador.TieneLicencia = request.TieneLicencia;
        colaborador.NumeroLicencia = request.NumeroLicencia?.Trim();
        colaborador.TipoLicencia = request.TipoLicencia?.Trim();
        colaborador.FechaVencimientoLicencia = request.FechaVencimientoLicencia;
        colaborador.EstatusId = request.EstatusId;
        colaborador.Salario = request.Salario;
        colaborador.Viaticos = request.Viaticos;
        colaborador.GastosRepresentacion = request.GastosRepresentacion;
        colaborador.FechaSalida = request.FechaSalida;
        colaborador.MotivoSalidaId = request.MotivoSalidaId;
        colaborador.Vacante = request.Vacante;
        colaborador.UltimaVacacion = request.UltimaVacacion;
    }

    private async Task<string?> GetJefeDisplayAsync(int? jefeId, CancellationToken cancellationToken)
    {
        if (!jefeId.HasValue)
        {
            return null;
        }

        var jefe = await db.Colaboradores
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ColaboradorId == jefeId.Value, cancellationToken);

        return jefe?.NombreCompleto();
    }

    private static string? FormatJefeValue(int? jefeId, string? nombre)
    {
        return jefeId.HasValue ? $"{nombre ?? "N/D"} ({jefeId.Value})" : null;
    }
}
