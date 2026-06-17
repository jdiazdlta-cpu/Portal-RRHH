using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortalRRHHFZ.Application.Common;
using PortalRRHHFZ.Application.DTOs;
using PortalRRHHFZ.Domain.Constants;
using PortalRRHHFZ.Domain.Entities;
using PortalRRHHFZ.Domain.Enums;
using PortalRRHHFZ.Infrastructure.Data;

namespace PortalRRHHFZ.Api.Controllers;

[ApiController]
[Authorize(Policy = AppPolicies.RequireAdminOrRRHH)]
[Route("api/dashboard")]
public sealed class DashboardController(AppDbContext db) : ControllerBase
{
    private static readonly string[] EstatusOperativos = ["A", "V", "S"];
    private static readonly TipoAlerta[] TiposRecordatorio =
    [
        TipoAlerta.Cedula,
        TipoAlerta.Licencia,
        TipoAlerta.Contrato,
        TipoAlerta.PeriodoProbatorio,
        TipoAlerta.Documento
    ];

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen([FromQuery] int? empresaId, [FromQuery] int? estatusId, [FromQuery] string? codigoEstatus, CancellationToken cancellationToken)
    {
        var colaboradores = ColaboradoresActivos(empresaId, estatusId, codigoEstatus);
        var total = await colaboradores.CountAsync(cancellationToken);
        var counts = await colaboradores
            .GroupBy(x => x.Estatus.Codigo)
            .Select(x => new { Codigo = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.Codigo, x => x.Count, cancellationToken);

        var alertas = AlertasOperativas(empresaId, estatusId, codigoEstatus);
        var alertasActivas = await alertas.CountAsync(x => x.EstadoAlerta == EstadoAlerta.Pendiente || x.EstadoAlerta == EstadoAlerta.Vencida, cancellationToken);
        var vencimientos = await alertas.CountAsync(x => x.EstadoAlerta == EstadoAlerta.Pendiente, cancellationToken);

        var dto = new DashboardResumenDto(
            total,
            counts.GetValueOrDefault("A"),
            counts.GetValueOrDefault("C"),
            counts.GetValueOrDefault("V"),
            counts.GetValueOrDefault("S"),
            alertasActivas,
            vencimientos);

        return Ok(ApiResponse<DashboardResumenDto>.Ok(dto));
    }

    [HttpGet("vencimientos")]
    public async Task<IActionResult> Vencimientos([FromQuery] int? empresaId, [FromQuery] int? estatusId, [FromQuery] string? codigoEstatus, [FromQuery] string? tipoAlerta, CancellationToken cancellationToken)
    {
        var data = await ApplyTipoAlertaFilter(AlertasOperativas(empresaId, estatusId, codigoEstatus), tipoAlerta)
            .Where(x => x.EstadoAlerta == EstadoAlerta.Pendiente || x.EstadoAlerta == EstadoAlerta.Vencida)
            .OrderBy(x => x.FechaVencimiento)
            .Take(50)
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<List<AlertaDto>>.Ok(data.Select(x => x.ToDto()).ToList()));
    }

    [HttpGet("colaboradores-por-estatus")]
    public async Task<IActionResult> ColaboradoresPorEstatus([FromQuery] int? empresaId, [FromQuery] int? estatusId, [FromQuery] string? codigoEstatus, CancellationToken cancellationToken)
    {
        var grouped = await ColaboradoresActivos(empresaId, estatusId, codigoEstatus)
            .GroupBy(x => x.Estatus.Nombre)
            .Select(x => new { Label = x.Key, Value = x.Count() })
            .OrderByDescending(x => x.Value)
            .ToListAsync(cancellationToken);

        var data = grouped
            .Select(x => new ChartItemDto(x.Label ?? "Sin estatus", x.Value))
            .ToList();

        return Ok(ApiResponse<List<ChartItemDto>>.Ok(data));
    }

    [HttpGet("colaboradores-por-departamento")]
    public async Task<IActionResult> ColaboradoresPorDepartamento([FromQuery] int? empresaId, [FromQuery] int? estatusId, [FromQuery] string? codigoEstatus, CancellationToken cancellationToken)
    {
        var grouped = await ColaboradoresActivos(empresaId, estatusId, codigoEstatus)
            .GroupBy(x => x.Departamento.Nombre)
            .Select(x => new { Label = x.Key, Value = x.Count() })
            .OrderByDescending(x => x.Value)
            .ToListAsync(cancellationToken);

        var data = grouped
            .Select(x => new ChartItemDto(x.Label ?? "Sin departamento", x.Value))
            .ToList();

        return Ok(ApiResponse<List<ChartItemDto>>.Ok(data));
    }

    [HttpGet("colaboradores-por-empresa")]
    public async Task<IActionResult> ColaboradoresPorEmpresa([FromQuery] int? empresaId, [FromQuery] int? estatusId, [FromQuery] string? codigoEstatus, CancellationToken cancellationToken)
    {
        var grouped = await ColaboradoresActivos(empresaId, estatusId, codigoEstatus)
            .GroupBy(x => x.Empresa.Nombre)
            .Select(x => new { Label = x.Key, Value = x.Count() })
            .OrderByDescending(x => x.Value)
            .ToListAsync(cancellationToken);

        var data = grouped
            .Select(x => new ChartItemDto(x.Label ?? "Sin empresa", x.Value))
            .ToList();

        return Ok(ApiResponse<List<ChartItemDto>>.Ok(data));
    }

    [HttpGet("colaboradores-por-tipo-contrato")]
    public async Task<IActionResult> ColaboradoresPorTipoContrato([FromQuery] int? empresaId, [FromQuery] int? estatusId, [FromQuery] string? codigoEstatus, CancellationToken cancellationToken)
    {
        var grouped = await ColaboradoresActivos(empresaId, estatusId, codigoEstatus)
            .GroupBy(x => x.TipoContrato.Nombre)
            .Select(x => new { Label = x.Key, Value = x.Count() })
            .OrderByDescending(x => x.Value)
            .ToListAsync(cancellationToken);

        var data = grouped
            .Select(x => new ChartItemDto(x.Label ?? "Sin tipo", x.Value))
            .ToList();

        return Ok(ApiResponse<List<ChartItemDto>>.Ok(data));
    }

    [HttpGet("altas-bajas")]
    public async Task<IActionResult> AltasBajas(
        [FromQuery] int? empresaId,
        [FromQuery] int? estatusId,
        [FromQuery] string? codigoEstatus,
        [FromQuery] int? year,
        [FromQuery(Name = "anio")] int? anio,
        [FromQuery] int? month,
        [FromQuery(Name = "mes")] int? mes,
        CancellationToken cancellationToken)
    {
        var targetYear = year ?? anio ?? DateTime.Today.Year;
        var selectedMonth = NormalizeMonth(month ?? mes);
        IEnumerable<int> months = selectedMonth.HasValue ? [selectedMonth.Value] : Enumerable.Range(1, 12);
        var colaboradores = ColaboradoresActivos(empresaId, estatusId, codigoEstatus);
        var result = new List<AltasBajasDto>();

        foreach (var currentMonth in months)
        {
            var start = new DateTime(targetYear, currentMonth, 1);
            var end = start.AddMonths(1);
            var altas = await colaboradores.CountAsync(x => x.FechaIngreso >= start && x.FechaIngreso < end, cancellationToken);
            var bajas = await colaboradores.CountAsync(x => x.FechaSalida.HasValue && x.FechaSalida.Value >= start && x.FechaSalida.Value < end, cancellationToken);
            result.Add(new AltasBajasDto(start.ToString("yyyy-MM"), altas, bajas));
        }

        return Ok(ApiResponse<List<AltasBajasDto>>.Ok(result));
    }

    [HttpGet("ultimos-movimientos")]
    public async Task<IActionResult> UltimosMovimientos([FromQuery] int? empresaId, [FromQuery] int? estatusId, [FromQuery] string? codigoEstatus, CancellationToken cancellationToken)
    {
        var query = db.HistorialColaborador
            .Include(x => x.Colaborador)
            .Include(x => x.Usuario)
            .AsNoTracking()
            .AsQueryable();

        if (empresaId.HasValue)
        {
            query = query.Where(x => x.Colaborador.EmpresaId == empresaId.Value);
        }

        query = ApplyEstatusFilter(query, estatusId, codigoEstatus);

        var data = await query
            .OrderByDescending(x => x.Fecha)
            .Take(20)
            .Select(x => new MovimientoDto(x.HistorialColaboradorId, x.Colaborador.PrimerNombre + " " + x.Colaborador.PrimerApellido, x.Usuario.NombreUsuario, x.Accion, x.Fecha, x.Observacion))
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<List<MovimientoDto>>.Ok(data));
    }

    [HttpGet("recordatorios-documentos")]
    public async Task<IActionResult> RecordatoriosDocumentos([FromQuery] int? empresaId, [FromQuery] int? estatusId, [FromQuery] string? codigoEstatus, [FromQuery] string? tipoAlerta, CancellationToken cancellationToken)
    {
        var hoy = DateTime.Today;
        var alertas = await ApplyTipoAlertaFilter(AlertasOperativas(empresaId, estatusId, codigoEstatus), tipoAlerta)
            .Where(x => x.EstadoAlerta == EstadoAlerta.Pendiente && TiposRecordatorio.Contains(x.TipoAlerta) && x.FechaVencimiento >= hoy)
            .OrderBy(x => x.FechaVencimiento)
            .Take(30)
            .ToListAsync(cancellationToken);

        var data = alertas
            .Select(x => new RecordatorioDocumentoDto(
                x.AlertaId,
                x.ColaboradorId,
                x.Colaborador.NombreCompleto(),
                x.Colaborador.Empresa.Nombre,
                x.TipoAlerta.ToString(),
                x.FechaVencimiento,
                Math.Max(0, (x.FechaVencimiento.Date - hoy).Days)))
            .ToList();

        return Ok(ApiResponse<List<RecordatorioDocumentoDto>>.Ok(data));
    }

    [HttpGet("altas-detalle")]
    public async Task<IActionResult> AltasDetalle(
        [FromQuery] int? empresaId,
        [FromQuery] int? estatusId,
        [FromQuery] string? codigoEstatus,
        [FromQuery] int? year,
        [FromQuery(Name = "anio")] int? anio,
        [FromQuery] int? month,
        [FromQuery(Name = "mes")] int? mes,
        CancellationToken cancellationToken)
    {
        var (start, end) = ResolveMonthRange(year, anio, month, mes);

        var colaboradores = await ColaboradoresActivos(empresaId, estatusId, codigoEstatus)
            .Include(x => x.Empresa)
            .Include(x => x.Departamento)
            .Include(x => x.Cargo)
            .Include(x => x.Estatus)
            .Where(x => x.FechaIngreso >= start && x.FechaIngreso < end)
            .OrderByDescending(x => x.FechaIngreso)
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<List<ColaboradorListDto>>.Ok(colaboradores.Select(x => x.ToListDto()).ToList()));
    }

    [HttpGet("bajas-detalle")]
    public async Task<IActionResult> BajasDetalle(
        [FromQuery] int? empresaId,
        [FromQuery] int? estatusId,
        [FromQuery] string? codigoEstatus,
        [FromQuery] int? year,
        [FromQuery(Name = "anio")] int? anio,
        [FromQuery] int? month,
        [FromQuery(Name = "mes")] int? mes,
        CancellationToken cancellationToken)
    {
        var (start, end) = ResolveMonthRange(year, anio, month, mes);

        var colaboradores = await ColaboradoresActivos(empresaId, estatusId, codigoEstatus)
            .Include(x => x.Empresa)
            .Include(x => x.Departamento)
            .Include(x => x.Cargo)
            .Include(x => x.Estatus)
            .Where(x => x.FechaSalida.HasValue && x.FechaSalida.Value >= start && x.FechaSalida.Value < end)
            .OrderByDescending(x => x.FechaSalida)
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<List<ColaboradorListDto>>.Ok(colaboradores.Select(x => x.ToListDto()).ToList()));
    }

    private IQueryable<Colaborador> ColaboradoresActivos(int? empresaId, int? estatusId, string? codigoEstatus)
    {
        var query = db.Colaboradores
            .AsNoTracking()
            .Where(x => x.IsActive);

        if (empresaId.HasValue)
        {
            query = query.Where(x => x.EmpresaId == empresaId.Value);
        }

        return ApplyEstatusFilter(query, estatusId, codigoEstatus);
    }

    private IQueryable<Alerta> AlertasOperativas(int? empresaId, int? estatusId, string? codigoEstatus)
    {
        var query = db.Alertas
            .Include(x => x.Colaborador)
            .ThenInclude(x => x.Empresa)
            .AsNoTracking()
            .Where(x =>
                x.IsActive &&
                x.Colaborador.IsActive &&
                EstatusOperativos.Contains(x.Colaborador.Estatus.Codigo));

        if (empresaId.HasValue)
        {
            query = query.Where(x => x.Colaborador.EmpresaId == empresaId.Value);
        }

        return ApplyAlertaEstatusFilter(query, estatusId, codigoEstatus);
    }

    private static IQueryable<Colaborador> ApplyEstatusFilter(IQueryable<Colaborador> query, int? estatusId, string? codigoEstatus)
    {
        if (estatusId.HasValue)
        {
            query = query.Where(x => x.EstatusId == estatusId.Value);
        }

        if (!string.IsNullOrWhiteSpace(codigoEstatus))
        {
            var codigo = codigoEstatus.Trim();
            query = query.Where(x => x.Estatus.Codigo == codigo);
        }

        return query;
    }

    private static IQueryable<HistorialColaborador> ApplyEstatusFilter(IQueryable<HistorialColaborador> query, int? estatusId, string? codigoEstatus)
    {
        if (estatusId.HasValue)
        {
            query = query.Where(x => x.Colaborador.EstatusId == estatusId.Value);
        }

        if (!string.IsNullOrWhiteSpace(codigoEstatus))
        {
            var codigo = codigoEstatus.Trim();
            query = query.Where(x => x.Colaborador.Estatus.Codigo == codigo);
        }

        return query;
    }

    private static IQueryable<Alerta> ApplyAlertaEstatusFilter(IQueryable<Alerta> query, int? estatusId, string? codigoEstatus)
    {
        if (estatusId.HasValue)
        {
            query = query.Where(x => x.Colaborador.EstatusId == estatusId.Value);
        }

        if (!string.IsNullOrWhiteSpace(codigoEstatus))
        {
            var codigo = codigoEstatus.Trim();
            query = query.Where(x => x.Colaborador.Estatus.Codigo == codigo);
        }

        return query;
    }

    private static IQueryable<Alerta> ApplyTipoAlertaFilter(IQueryable<Alerta> query, string? tipoAlerta)
    {
        if (Enum.TryParse<TipoAlerta>(tipoAlerta, true, out var tipo))
        {
            query = query.Where(x => x.TipoAlerta == tipo);
        }

        return query;
    }

    private static (DateTime Start, DateTime End) ResolveMonthRange(int? year, int? anio, int? month, int? mes)
    {
        var targetYear = year ?? anio ?? DateTime.Today.Year;
        var targetMonth = NormalizeMonth(month ?? mes) ?? DateTime.Today.Month;
        var start = new DateTime(targetYear, targetMonth, 1);
        return (start, start.AddMonths(1));
    }

    private static int? NormalizeMonth(int? month)
    {
        return month is >= 1 and <= 12 ? month.Value : null;
    }
}
