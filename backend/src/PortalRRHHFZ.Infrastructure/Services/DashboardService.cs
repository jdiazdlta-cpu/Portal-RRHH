using Microsoft.EntityFrameworkCore;
using PortalRRHHFZ.Application.Common;
using PortalRRHHFZ.Application.DTOs.Dashboard;
using PortalRRHHFZ.Application.Interfaces.Dashboard;
using PortalRRHHFZ.Domain.Entities;
using PortalRRHHFZ.Domain.Enums;
using PortalRRHHFZ.Infrastructure.Data;

namespace PortalRRHHFZ.Infrastructure.Services;

public sealed class DashboardService(AppDbContext dbContext) : IDashboardService
{
    private const int DiasAnticipacion = 7;

    public async Task<ApiResponse<DashboardResumenDto>> GetResumenAsync(
        CancellationToken cancellationToken = default)
    {
        var colaboradoresPorCodigo = (await dbContext.Colaboradores
            .AsNoTracking()
            .GroupBy(colaborador => colaborador.Estatus.Codigo)
            .Select(group => new
            {
                Codigo = group.Key,
                Total = group.Count()
            })
            .ToListAsync(cancellationToken))
            .ToDictionary(item => item.Codigo, item => item.Total, StringComparer.OrdinalIgnoreCase);

        var alertasPorEstado = (await dbContext.Alertas
            .AsNoTracking()
            .Where(alerta => alerta.IsActive)
            .GroupBy(alerta => alerta.EstadoAlerta)
            .Select(group => new
            {
                Estado = group.Key,
                Total = group.Count()
            })
            .ToListAsync(cancellationToken))
            .ToDictionary(item => item.Estado, item => item.Total);

        var resumen = new DashboardResumenDto
        {
            TotalColaboradores = await dbContext.Colaboradores.CountAsync(cancellationToken),
            TotalActivos = colaboradoresPorCodigo.GetValueOrDefault("A"),
            TotalCesantes = colaboradoresPorCodigo.GetValueOrDefault("C"),
            TotalVacaciones = colaboradoresPorCodigo.GetValueOrDefault("V"),
            TotalServicio = colaboradoresPorCodigo.GetValueOrDefault("S"),
            TotalSuspendidos = colaboradoresPorCodigo.GetValueOrDefault("SU"),
            TotalEmpresasActivas = await dbContext.Empresas.CountAsync(empresa => empresa.IsActive, cancellationToken),
            TotalDepartamentosActivos = await dbContext.Departamentos.CountAsync(departamento => departamento.IsActive, cancellationToken),
            TotalCargosActivos = await dbContext.Cargos.CountAsync(cargo => cargo.IsActive, cancellationToken),
            TotalDocumentosActivos = await dbContext.DocumentosColaborador.CountAsync(documento => documento.IsActive, cancellationToken),
            TotalAlertasActivas = alertasPorEstado.Values.Sum(),
            TotalAlertasPendientes = alertasPorEstado.GetValueOrDefault(EstadoAlerta.Pendiente),
            TotalAlertasVencidas = alertasPorEstado.GetValueOrDefault(EstadoAlerta.Vencida),
            TotalAlertasGestionadas = alertasPorEstado.GetValueOrDefault(EstadoAlerta.Gestionada),
            TotalAlertasIgnoradas = alertasPorEstado.GetValueOrDefault(EstadoAlerta.Ignorada)
        };

        return ApiResponse<DashboardResumenDto>.Ok(resumen);
    }

    public async Task<ApiResponse<DashboardVencimientosDto>> GetVencimientosAsync(
        CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var limitDate = today.AddDays(DiasAnticipacion);

        var alertasRaw = await dbContext.Alertas
            .AsNoTracking()
            .Where(alerta =>
                alerta.IsActive
                && (alerta.EstadoAlerta == EstadoAlerta.Pendiente || alerta.EstadoAlerta == EstadoAlerta.Vencida))
            .Select(alerta => new
            {
                alerta.TipoAlerta,
                alerta.EstadoAlerta,
                FechaVencimiento = alerta.FechaVencimiento.Date
            })
            .ToListAsync(cancellationToken);

        var alertas = alertasRaw
            .Select(alerta => new VencimientoAlertaRow(
                alerta.TipoAlerta,
                alerta.EstadoAlerta,
                alerta.FechaVencimiento))
            .ToList();

        var resumen = new DashboardVencimientosDto
        {
            CedulasPorVencer = CountPorVencer(alertas, TipoAlerta.Cedula, today, limitDate),
            LicenciasPorVencer = CountPorVencer(alertas, TipoAlerta.Licencia, today, limitDate),
            ContratosPorVencer = CountPorVencer(alertas, TipoAlerta.Contrato, today, limitDate),
            PeriodosProbatoriosPorVencer = CountPorVencer(alertas, TipoAlerta.PeriodoProbatorio, today, limitDate),
            DocumentosPorVencer = CountPorVencer(alertas, TipoAlerta.Documento, today, limitDate),
            CedulasVencidas = CountVencidas(alertas, TipoAlerta.Cedula, today),
            LicenciasVencidas = CountVencidas(alertas, TipoAlerta.Licencia, today),
            ContratosVencidos = CountVencidas(alertas, TipoAlerta.Contrato, today),
            PeriodosProbatoriosVencidos = CountVencidas(alertas, TipoAlerta.PeriodoProbatorio, today),
            DocumentosVencidos = CountVencidas(alertas, TipoAlerta.Documento, today),
            RequiereRecalculo = alertas.Any(alerta =>
                alerta.EstadoAlerta == EstadoAlerta.Pendiente
                && alerta.FechaVencimiento < today)
        };

        return ApiResponse<DashboardVencimientosDto>.Ok(resumen);
    }

    public async Task<ApiResponse<IReadOnlyCollection<ColaboradoresPorEstatusDto>>> GetColaboradoresPorEstatusAsync(
        CancellationToken cancellationToken = default)
    {
        var data = await dbContext.EstatusColaborador
            .AsNoTracking()
            .OrderBy(estatus => estatus.Nombre)
            .Select(estatus => new ColaboradoresPorEstatusDto
            {
                EstatusId = estatus.EstatusId,
                Nombre = estatus.Nombre,
                Codigo = estatus.Codigo,
                Total = estatus.Colaboradores.Count()
            })
            .ToListAsync(cancellationToken);

        return ApiResponse<IReadOnlyCollection<ColaboradoresPorEstatusDto>>.Ok(data);
    }

    public async Task<ApiResponse<IReadOnlyCollection<ColaboradoresPorDepartamentoDto>>> GetColaboradoresPorDepartamentoAsync(
        CancellationToken cancellationToken = default)
    {
        var data = await dbContext.Departamentos
            .AsNoTracking()
            .Where(departamento => departamento.IsActive && departamento.Empresa.IsActive)
            .OrderBy(departamento => departamento.Empresa.Nombre)
            .ThenBy(departamento => departamento.Nombre)
            .Select(departamento => new ColaboradoresPorDepartamentoDto
            {
                EmpresaId = departamento.EmpresaId,
                EmpresaNombre = departamento.Empresa.Nombre,
                DepartamentoId = departamento.DepartamentoId,
                DepartamentoNombre = departamento.Nombre,
                Total = departamento.Colaboradores.Count(colaborador => colaborador.IsActive)
            })
            .ToListAsync(cancellationToken);

        return ApiResponse<IReadOnlyCollection<ColaboradoresPorDepartamentoDto>>.Ok(data);
    }

    public async Task<ApiResponse<IReadOnlyCollection<AltasBajasDto>>> GetAltasBajasAsync(
        AltasBajasFilterRequest filters,
        CancellationToken cancellationToken = default)
    {
        var anio = filters.Anio ?? DateTime.UtcNow.Year;
        var inicio = new DateTime(anio, 1, 1);
        var fin = inicio.AddYears(1);

        var altasQuery = ApplyAltasBajasFilters(dbContext.Colaboradores.AsNoTracking(), filters)
            .Where(colaborador => colaborador.FechaIngreso >= inicio && colaborador.FechaIngreso < fin);

        var bajasQuery = ApplyAltasBajasFilters(dbContext.Colaboradores.AsNoTracking(), filters)
            .Where(colaborador => colaborador.FechaSalida.HasValue
                && colaborador.FechaSalida.Value >= inicio
                && colaborador.FechaSalida.Value < fin);

        var altas = await altasQuery
            .GroupBy(colaborador => colaborador.FechaIngreso.Month)
            .Select(group => new { Mes = group.Key, Total = group.Count() })
            .ToListAsync(cancellationToken);

        var bajas = await bajasQuery
            .GroupBy(colaborador => colaborador.FechaSalida!.Value.Month)
            .Select(group => new { Mes = group.Key, Total = group.Count() })
            .ToListAsync(cancellationToken);

        var data = Enumerable.Range(1, 12)
            .Select(mes => new AltasBajasDto
            {
                Mes = mes,
                Altas = altas.FirstOrDefault(item => item.Mes == mes)?.Total ?? 0,
                Bajas = bajas.FirstOrDefault(item => item.Mes == mes)?.Total ?? 0
            })
            .ToList();

        return ApiResponse<IReadOnlyCollection<AltasBajasDto>>.Ok(data);
    }

    public async Task<ApiResponse<UltimosMovimientosDto>> GetUltimosMovimientosAsync(
        CancellationToken cancellationToken = default)
    {
        var ultimosIngresos = await BaseColaboradorQuery()
            .AsNoTracking()
            .OrderByDescending(colaborador => colaborador.FechaIngreso)
            .ThenByDescending(colaborador => colaborador.ColaboradorId)
            .Take(5)
            .ToListAsync(cancellationToken);

        var ultimasSalidas = await BaseColaboradorQuery()
            .AsNoTracking()
            .Where(colaborador => colaborador.FechaSalida.HasValue)
            .OrderByDescending(colaborador => colaborador.FechaSalida)
            .ThenByDescending(colaborador => colaborador.ColaboradorId)
            .Take(5)
            .ToListAsync(cancellationToken);

        var data = new UltimosMovimientosDto
        {
            UltimosIngresos = ultimosIngresos.Select(ToIngresoDto).ToList(),
            UltimasSalidas = ultimasSalidas.Select(ToSalidaDto).ToList()
        };

        return ApiResponse<UltimosMovimientosDto>.Ok(data);
    }

    private IQueryable<Colaborador> BaseColaboradorQuery()
    {
        return dbContext.Colaboradores
            .Include(colaborador => colaborador.Empresa)
            .Include(colaborador => colaborador.Departamento)
            .Include(colaborador => colaborador.Cargo)
            .Include(colaborador => colaborador.Estatus);
    }

    private static IQueryable<Colaborador> ApplyAltasBajasFilters(
        IQueryable<Colaborador> query,
        AltasBajasFilterRequest filters)
    {
        if (filters.EmpresaId.HasValue)
        {
            query = query.Where(colaborador => colaborador.EmpresaId == filters.EmpresaId.Value);
        }

        if (filters.DepartamentoId.HasValue)
        {
            query = query.Where(colaborador => colaborador.DepartamentoId == filters.DepartamentoId.Value);
        }

        return query;
    }

    private static MovimientoColaboradorDto ToIngresoDto(Colaborador colaborador)
    {
        return new MovimientoColaboradorDto
        {
            ColaboradorId = colaborador.ColaboradorId,
            NombreCompleto = GetNombreCompleto(colaborador),
            EmpresaNombre = colaborador.Empresa.Nombre,
            DepartamentoNombre = colaborador.Departamento.Nombre,
            CargoNombre = colaborador.Cargo.Nombre,
            FechaIngreso = colaborador.FechaIngreso,
            FechaSalida = null,
            EstatusNombre = colaborador.Estatus.Nombre
        };
    }

    private static MovimientoColaboradorDto ToSalidaDto(Colaborador colaborador)
    {
        return new MovimientoColaboradorDto
        {
            ColaboradorId = colaborador.ColaboradorId,
            NombreCompleto = GetNombreCompleto(colaborador),
            EmpresaNombre = colaborador.Empresa.Nombre,
            DepartamentoNombre = colaborador.Departamento.Nombre,
            CargoNombre = colaborador.Cargo.Nombre,
            FechaIngreso = null,
            FechaSalida = colaborador.FechaSalida,
            EstatusNombre = colaborador.Estatus.Nombre
        };
    }

    private static int CountPorVencer(
        IReadOnlyCollection<VencimientoAlertaRow> alertas,
        TipoAlerta tipoAlerta,
        DateTime today,
        DateTime limitDate)
    {
        return alertas.Count(alerta =>
            alerta.TipoAlerta == tipoAlerta
            && alerta.EstadoAlerta == EstadoAlerta.Pendiente
            && alerta.FechaVencimiento >= today
            && alerta.FechaVencimiento <= limitDate);
    }

    private static int CountVencidas(
        IReadOnlyCollection<VencimientoAlertaRow> alertas,
        TipoAlerta tipoAlerta,
        DateTime today)
    {
        return alertas.Count(alerta =>
            alerta.TipoAlerta == tipoAlerta
            && (alerta.EstadoAlerta == EstadoAlerta.Vencida
                || (alerta.EstadoAlerta == EstadoAlerta.Pendiente && alerta.FechaVencimiento < today)));
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

    private sealed record VencimientoAlertaRow(
        TipoAlerta TipoAlerta,
        EstadoAlerta EstadoAlerta,
        DateTime FechaVencimiento);
}
