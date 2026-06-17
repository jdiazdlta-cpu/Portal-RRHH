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
[Route("api/alertas")]
public sealed class AlertasController(AppDbContext db) : ControllerBase
{
    private static readonly string[] EstatusOperativos = ["A", "V", "S"];
    private const string ResultadoRenovoEventual = "RenovoEventual";
    private const string ResultadoPasoPermanente = "PasoPermanente";
    private const string ResultadoPasoCesante = "PasoCesante";
    private const string ResultadoExcepcion = "Excepcion";

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? estado, [FromQuery] string? tipo, [FromQuery] string? tipoAlerta, CancellationToken cancellationToken)
    {
        var query = db.Alertas
            .Include(x => x.Colaborador)
            .ThenInclude(x => x.Empresa)
            .Where(x => x.IsActive)
            .AsNoTracking()
            .AsQueryable();

        if (Enum.TryParse<EstadoAlerta>(estado, true, out var estadoEnum))
        {
            query = query.Where(x => x.EstadoAlerta == estadoEnum);
        }

        var tipoFiltro = tipoAlerta ?? tipo;
        if (Enum.TryParse<TipoAlerta>(tipoFiltro, true, out var tipoEnum))
        {
            query = query.Where(x => x.TipoAlerta == tipoEnum);
        }

        var alertas = await query.OrderBy(x => x.FechaVencimiento).Take(500).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<AlertaDto>>.Ok(alertas.Select(x => x.ToDto()).ToList()));
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen(CancellationToken cancellationToken)
    {
        var data = new
        {
            pendientes = await db.Alertas.CountAsync(x => x.IsActive && x.EstadoAlerta == EstadoAlerta.Pendiente, cancellationToken),
            vencidas = await db.Alertas.CountAsync(x => x.IsActive && x.EstadoAlerta == EstadoAlerta.Vencida, cancellationToken),
            gestionadas = await db.Alertas.CountAsync(x => x.IsActive && x.EstadoAlerta == EstadoAlerta.Gestionada, cancellationToken),
            ignoradas = await db.Alertas.CountAsync(x => x.IsActive && x.EstadoAlerta == EstadoAlerta.Ignorada, cancellationToken)
        };

        return Ok(ApiResponse<object>.Ok(data));
    }

    [HttpPatch("{id:int}/gestionar")]
    public Task<IActionResult> Gestionar(int id, [FromBody] AlertaGestionRequest request, CancellationToken cancellationToken) => CambiarEstado(id, EstadoAlerta.Gestionada, request.ObservacionGestion, cancellationToken);

    [HttpPatch("{id:int}/gestionar-con-correccion")]
    public async Task<IActionResult> GestionarConCorreccion(int id, [FromBody] AlertaGestionCorreccionRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ObservacionGestion))
        {
            return BadRequest(ApiResponse<object>.Fail("La observacion de gestion es obligatoria."));
        }

        var alerta = await db.Alertas
            .Include(x => x.Colaborador)
            .Include(x => x.DocumentoColaborador)
            .FirstOrDefaultAsync(x => x.AlertaId == id, cancellationToken);

        if (alerta is null)
        {
            return NotFound(ApiResponse<object>.Fail("Alerta no encontrada."));
        }

        if (alerta.EstadoAlerta is EstadoAlerta.Gestionada or EstadoAlerta.Ignorada)
        {
            return BadRequest(ApiResponse<object>.Fail("La alerta ya fue cerrada."));
        }

        var validation = await ValidateCorrectionAsync(alerta, request, cancellationToken);
        if (validation is not null)
        {
            return BadRequest(ApiResponse<object>.Fail(validation));
        }

        var changed = await ApplyCorrectionAsync(alerta, request, cancellationToken);
        if (!changed && !request.GestionarSinCambio)
        {
            return BadRequest(ApiResponse<object>.Fail("Debe corregir al menos un dato o marcar Gestionar sin cambio por excepcion."));
        }

        alerta.EstadoAlerta = EstadoAlerta.Gestionada;
        alerta.FechaGestion = DateTime.UtcNow;
        alerta.GestionadaPor = User.CurrentUserId();
        alerta.ObservacionGestion = request.ObservacionGestion.Trim();
        alerta.UpdatedBy = User.Identity?.Name;

        if (!changed && request.GestionarSinCambio)
        {
            AddHistorial(alerta.ColaboradorId, "GESTION_ALERTA_SIN_CAMBIO", "Alerta", null, alerta.TipoAlerta.ToString(), request.ObservacionGestion);
        }

        await db.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<AlertaDto>.Ok(alerta.ToDto(), "Alerta gestionada con correccion."));
    }

    [HttpPatch("{id:int}/ignorar")]
    public Task<IActionResult> Ignorar(int id, [FromBody] AlertaGestionRequest request, CancellationToken cancellationToken) => CambiarEstado(id, EstadoAlerta.Ignorada, request.ObservacionGestion, cancellationToken);

    [HttpPost("recalcular")]
    public async Task<IActionResult> Recalcular(CancellationToken cancellationToken)
    {
        var creadas = await RecalcularAlertasAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { creadas }, "Alertas recalculadas."));
    }

    private async Task<IActionResult> CambiarEstado(int id, EstadoAlerta estado, string? observacion, CancellationToken cancellationToken)
    {
        if (estado == EstadoAlerta.Ignorada && string.IsNullOrWhiteSpace(observacion))
        {
            return BadRequest(ApiResponse<object>.Fail("La observacion es obligatoria para ignorar una alerta."));
        }

        var alerta = await db.Alertas.Include(x => x.Colaborador).FirstOrDefaultAsync(x => x.AlertaId == id, cancellationToken);
        if (alerta is null)
        {
            return NotFound(ApiResponse<object>.Fail("Alerta no encontrada."));
        }

        alerta.EstadoAlerta = estado;
        alerta.FechaGestion = DateTime.UtcNow;
        alerta.GestionadaPor = User.CurrentUserId();
        alerta.ObservacionGestion = observacion?.Trim();
        alerta.UpdatedBy = User.Identity?.Name;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<AlertaDto>.Ok(alerta.ToDto(), estado == EstadoAlerta.Gestionada ? "Alerta gestionada." : "Alerta ignorada."));
    }

    private async Task<string?> ValidateCorrectionAsync(Alerta alerta, AlertaGestionCorreccionRequest request, CancellationToken cancellationToken)
    {
        if (request.GestionarSinCambio && alerta.TipoAlerta != TipoAlerta.Contrato)
        {
            return null;
        }

        return alerta.TipoAlerta switch
        {
            TipoAlerta.Cedula => request.FechaVencimientoCedula.HasValue ? null : "Debe indicar la nueva fecha de vencimiento de cedula.",
            TipoAlerta.Licencia => ValidateLicencia(request),
            TipoAlerta.Contrato => await ValidateContratoAsync(request, cancellationToken),
            TipoAlerta.PeriodoProbatorio => request.FechaVencimientoPeriodoProbatorio.HasValue ? null : "Debe indicar la nueva fecha del periodo probatorio.",
            TipoAlerta.Documento => ValidateDocumento(alerta, request),
            _ => "Tipo de alerta no soportado."
        };
    }

    private static string? ValidateLicencia(AlertaGestionCorreccionRequest request)
    {
        if (request.TieneLicencia == true && (string.IsNullOrWhiteSpace(request.TipoLicencia) || !request.FechaVencimientoLicencia.HasValue))
        {
            return "Si tiene licencia, debe indicar tipo y fecha de vencimiento.";
        }

        return request.TieneLicencia.HasValue ||
            request.FechaVencimientoLicencia.HasValue ||
            request.NumeroLicencia is not null ||
            request.TipoLicencia is not null
                ? null
                : "Debe corregir los datos de licencia.";
    }

    private async Task<string?> ValidateContratoAsync(AlertaGestionCorreccionRequest request, CancellationToken cancellationToken)
    {
        var resultado = NormalizeResultadoContrato(request);
        if (resultado is null)
        {
            return "Debe indicar el resultado de la gestion del contrato.";
        }

        if (resultado == ResultadoRenovoEventual)
        {
            if (!ContratoFechaVencimiento(request).HasValue)
            {
                return "Debe indicar la nueva fecha de vencimiento del contrato eventual.";
            }

            if (!await db.TiposContrato.AnyAsync(x => x.IsActive && x.RequiereFechaVencimiento, cancellationToken))
            {
                return "No existe un tipo de contrato eventual activo.";
            }
        }

        if (resultado == ResultadoPasoPermanente && !await db.TiposContrato.AnyAsync(x => x.IsActive && !x.RequiereFechaVencimiento, cancellationToken))
        {
            return "No existe un tipo de contrato permanente activo.";
        }

        if (resultado == ResultadoPasoCesante)
        {
            if (!request.FechaSalida.HasValue)
            {
                return "Debe indicar la fecha de salida.";
            }

            if (!request.MotivoSalidaId.HasValue)
            {
                return "Debe indicar el motivo de salida.";
            }

            if (!await db.EstatusColaborador.AnyAsync(x => x.IsActive && x.Codigo == "C", cancellationToken))
            {
                return "No existe un estatus Cesante activo.";
            }

            if (!await db.MotivosSalida.AnyAsync(x => x.MotivoSalidaId == request.MotivoSalidaId.Value && x.IsActive, cancellationToken))
            {
                return "Motivo de salida no valido.";
            }
        }

        return null;
    }

    private static string? ValidateDocumento(Alerta alerta, AlertaGestionCorreccionRequest request)
    {
        if (alerta.DocumentoColaborador is null)
        {
            return "La alerta no tiene documento asociado.";
        }

        return request.FechaVencimientoDocumento.HasValue || request.ObservacionDocumento is not null
            ? null
            : "Debe corregir la fecha u observacion del documento.";
    }

    private async Task<bool> ApplyCorrectionAsync(Alerta alerta, AlertaGestionCorreccionRequest request, CancellationToken cancellationToken)
    {
        return alerta.TipoAlerta switch
        {
            TipoAlerta.Cedula => ApplyDate(
                alerta.Colaborador,
                nameof(Colaborador.FechaVencimientoCedula),
                alerta.Colaborador.FechaVencimientoCedula,
                request.FechaVencimientoCedula,
                value => alerta.Colaborador.FechaVencimientoCedula = value,
                request.ObservacionGestion),

            TipoAlerta.Licencia => ApplyLicencia(alerta.Colaborador, request),
            TipoAlerta.Contrato => await ApplyContratoAsync(alerta.Colaborador, request, cancellationToken),
            TipoAlerta.PeriodoProbatorio => ApplyDate(
                alerta.Colaborador,
                nameof(Colaborador.FechaVencimientoPeriodoProbatorio),
                alerta.Colaborador.FechaVencimientoPeriodoProbatorio,
                request.FechaVencimientoPeriodoProbatorio,
                value => alerta.Colaborador.FechaVencimientoPeriodoProbatorio = value,
                request.ObservacionGestion),
            TipoAlerta.Documento => ApplyDocumento(alerta, request),
            _ => false
        };
    }

    private bool ApplyLicencia(Colaborador colaborador, AlertaGestionCorreccionRequest request)
    {
        var changed = false;

        if (request.TieneLicencia.HasValue && colaborador.TieneLicencia != request.TieneLicencia.Value)
        {
            AddHistorial(colaborador.ColaboradorId, "CORRECCION_ALERTA", nameof(Colaborador.TieneLicencia), colaborador.TieneLicencia.ToString(), request.TieneLicencia.Value.ToString(), request.ObservacionGestion);
            colaborador.TieneLicencia = request.TieneLicencia.Value;
            changed = true;
        }

        changed |= ApplyText(colaborador, nameof(Colaborador.NumeroLicencia), colaborador.NumeroLicencia, request.NumeroLicencia, value => colaborador.NumeroLicencia = value, request.ObservacionGestion);
        changed |= ApplyText(colaborador, nameof(Colaborador.TipoLicencia), colaborador.TipoLicencia, request.TipoLicencia, value => colaborador.TipoLicencia = value, request.ObservacionGestion);
        changed |= ApplyDate(colaborador, nameof(Colaborador.FechaVencimientoLicencia), colaborador.FechaVencimientoLicencia, request.FechaVencimientoLicencia, value => colaborador.FechaVencimientoLicencia = value, request.ObservacionGestion);

        return changed;
    }

    private async Task<bool> ApplyContratoAsync(Colaborador colaborador, AlertaGestionCorreccionRequest request, CancellationToken cancellationToken)
    {
        var resultado = NormalizeResultadoContrato(request);
        return resultado switch
        {
            ResultadoRenovoEventual => await ApplyContratoRenovacionEventualAsync(colaborador, request, cancellationToken),
            ResultadoPasoPermanente => await ApplyContratoPasoPermanenteAsync(colaborador, request, cancellationToken),
            ResultadoPasoCesante => await ApplyContratoPasoCesanteAsync(colaborador, request, cancellationToken),
            ResultadoExcepcion => ApplyContratoExcepcion(colaborador, request),
            _ => false
        };
    }

    private async Task<bool> ApplyContratoRenovacionEventualAsync(Colaborador colaborador, AlertaGestionCorreccionRequest request, CancellationToken cancellationToken)
    {
        var changed = false;
        var eventual = await db.TiposContrato.AsNoTracking().Where(x => x.IsActive && x.RequiereFechaVencimiento).OrderBy(x => x.TipoContratoId).FirstAsync(cancellationToken);

        if (colaborador.TipoContratoId != eventual.TipoContratoId)
        {
            AddHistorial(colaborador.ColaboradorId, "CORRECCION_ALERTA", nameof(Colaborador.TipoContratoId), colaborador.TipoContratoId.ToString(), eventual.TipoContratoId.ToString(), request.ObservacionGestion);
            colaborador.TipoContratoId = eventual.TipoContratoId;
            changed = true;
        }

        changed |= ApplyDate(
            colaborador,
            nameof(Colaborador.FechaVencimientoContrato),
            colaborador.FechaVencimientoContrato,
            ContratoFechaVencimiento(request),
            value => colaborador.FechaVencimientoContrato = value,
            request.ObservacionGestion);

        return changed;
    }

    private async Task<bool> ApplyContratoPasoPermanenteAsync(Colaborador colaborador, AlertaGestionCorreccionRequest request, CancellationToken cancellationToken)
    {
        var permanente = await db.TiposContrato.AsNoTracking().Where(x => x.IsActive && !x.RequiereFechaVencimiento).OrderBy(x => x.TipoContratoId).FirstAsync(cancellationToken);
        if (colaborador.TipoContratoId == permanente.TipoContratoId)
        {
            AddHistorial(colaborador.ColaboradorId, "GESTION_ALERTA_CONTRATO", nameof(Colaborador.TipoContratoId), colaborador.TipoContratoId.ToString(), permanente.TipoContratoId.ToString(), request.ObservacionGestion);
            return true;
        }

        AddHistorial(colaborador.ColaboradorId, "CORRECCION_ALERTA", nameof(Colaborador.TipoContratoId), colaborador.TipoContratoId.ToString(), permanente.TipoContratoId.ToString(), request.ObservacionGestion);
        colaborador.TipoContratoId = permanente.TipoContratoId;
        return true;
    }

    private async Task<bool> ApplyContratoPasoCesanteAsync(Colaborador colaborador, AlertaGestionCorreccionRequest request, CancellationToken cancellationToken)
    {
        var changed = false;
        var cesante = await db.EstatusColaborador.AsNoTracking().FirstAsync(x => x.IsActive && x.Codigo == "C", cancellationToken);

        if (colaborador.EstatusId != cesante.EstatusId)
        {
            AddHistorial(colaborador.ColaboradorId, "CORRECCION_ALERTA", nameof(Colaborador.EstatusId), colaborador.EstatusId.ToString(), cesante.EstatusId.ToString(), request.ObservacionGestion);
            colaborador.EstatusId = cesante.EstatusId;
            changed = true;
        }

        if (request.MotivoSalidaId.HasValue && colaborador.MotivoSalidaId != request.MotivoSalidaId.Value)
        {
            AddHistorial(colaborador.ColaboradorId, "CORRECCION_ALERTA", nameof(Colaborador.MotivoSalidaId), colaborador.MotivoSalidaId?.ToString(), request.MotivoSalidaId.Value.ToString(), request.ObservacionGestion);
            colaborador.MotivoSalidaId = request.MotivoSalidaId.Value;
            changed = true;
        }

        changed |= ApplyDate(colaborador, nameof(Colaborador.FechaSalida), colaborador.FechaSalida, request.FechaSalida, value => colaborador.FechaSalida = value, request.ObservacionGestion);
        return changed;
    }

    private bool ApplyContratoExcepcion(Colaborador colaborador, AlertaGestionCorreccionRequest request)
    {
        AddHistorial(colaborador.ColaboradorId, "GESTION_ALERTA_SIN_CAMBIO", "Contrato", null, ResultadoExcepcion, request.ObservacionGestion);
        return true;
    }

    private bool ApplyDocumento(Alerta alerta, AlertaGestionCorreccionRequest request)
    {
        var documento = alerta.DocumentoColaborador;
        if (documento is null)
        {
            return false;
        }

        var changed = false;
        if (request.FechaVencimientoDocumento.HasValue && documento.FechaVencimiento?.Date != request.FechaVencimientoDocumento.Value.Date)
        {
            AddHistorial(alerta.ColaboradorId, "CORRECCION_ALERTA", "Documento.FechaVencimiento", FormatDate(documento.FechaVencimiento), FormatDate(request.FechaVencimientoDocumento), request.ObservacionGestion);
            documento.FechaVencimiento = request.FechaVencimientoDocumento.Value.Date;
            changed = true;
        }

        changed |= ApplyText(alerta.Colaborador, "Documento.Observacion", documento.Observacion, request.ObservacionDocumento, value => documento.Observacion = value, request.ObservacionGestion);
        return changed;
    }

    private bool ApplyDate(Colaborador colaborador, string field, DateTime? current, DateTime? next, Action<DateTime?> assign, string? observation)
    {
        if (!next.HasValue || current?.Date == next.Value.Date)
        {
            return false;
        }

        AddHistorial(colaborador.ColaboradorId, "CORRECCION_ALERTA", field, FormatDate(current), FormatDate(next), observation);
        assign(next.Value.Date);
        return true;
    }

    private bool ApplyText(Colaborador colaborador, string field, string? current, string? next, Action<string?> assign, string? observation)
    {
        if (next is null)
        {
            return false;
        }

        var normalized = string.IsNullOrWhiteSpace(next) ? null : next.Trim();
        if (string.Equals(current, normalized, StringComparison.Ordinal))
        {
            return false;
        }

        AddHistorial(colaborador.ColaboradorId, "CORRECCION_ALERTA", field, current, normalized, observation);
        assign(normalized);
        return true;
    }

    private void AddHistorial(int colaboradorId, string accion, string campo, string? anterior, string? nuevo, string? observacion)
    {
        db.HistorialColaborador.Add(new HistorialColaborador
        {
            ColaboradorId = colaboradorId,
            UsuarioId = User.CurrentUserId(),
            Accion = accion,
            Campo = campo,
            ValorAnterior = anterior,
            ValorNuevo = nuevo,
            Observacion = observacion?.Trim(),
            CreatedBy = User.Identity?.Name
        });
    }

    private static string? NormalizeResultadoContrato(AlertaGestionCorreccionRequest request)
    {
        if (request.GestionarSinCambio)
        {
            return ResultadoExcepcion;
        }

        var value = request.ResultadoGestionContrato?.Trim();
        return value?.ToUpperInvariant() switch
        {
            "RENOVOEVENTUAL" or "RENOVO_EVENTUAL" or "RENOVO EVENTUAL" => ResultadoRenovoEventual,
            "PASOPERMANENTE" or "PASO_PERMANENTE" or "PASO PERMANENTE" => ResultadoPasoPermanente,
            "PASOCESANTE" or "PASO_CESANTE" or "PASO CESANTE" => ResultadoPasoCesante,
            "EXCEPCION" => ResultadoExcepcion,
            _ => null
        };
    }

    private static DateTime? ContratoFechaVencimiento(AlertaGestionCorreccionRequest request) =>
        request.NuevaFechaVencimientoContrato ?? request.FechaVencimientoContrato;

    private static string? FormatDate(DateTime? value) => value?.ToString("yyyy-MM-dd");

    private async Task<int> RecalcularAlertasAsync(CancellationToken cancellationToken)
    {
        var hoy = DateTime.Today;
        var limite = hoy.AddDays(7);
        var creadas = 0;

        await DesactivarAlertasFueraDeReglaAsync(limite, cancellationToken);

        var colaboradores = await db.Colaboradores
            .Include(x => x.Estatus)
            .Include(x => x.TipoContrato)
            .Include(x => x.Documentos.Where(d => d.IsActive))
            .Where(x => x.IsActive && EstatusOperativos.Contains(x.Estatus.Codigo))
            .ToListAsync(cancellationToken);

        foreach (var colaborador in colaboradores)
        {
            creadas += await UpsertAlertaAsync(colaborador, TipoAlerta.Cedula, null, colaborador.FechaVencimientoCedula, $"Cedula de {colaborador.NombreCompleto()} por vencer", hoy, limite, cancellationToken);

            if (colaborador.TieneLicencia)
            {
                creadas += await UpsertAlertaAsync(colaborador, TipoAlerta.Licencia, null, colaborador.FechaVencimientoLicencia, $"Licencia de {colaborador.NombreCompleto()} por vencer", hoy, limite, cancellationToken);
            }

            if (colaborador.TipoContrato.RequiereFechaVencimiento)
            {
                creadas += await UpsertAlertaAsync(colaborador, TipoAlerta.Contrato, null, colaborador.FechaVencimientoContrato, $"Contrato de {colaborador.NombreCompleto()} por vencer", hoy, limite, cancellationToken);
            }

            creadas += await UpsertAlertaAsync(colaborador, TipoAlerta.PeriodoProbatorio, null, colaborador.FechaVencimientoPeriodoProbatorio, $"Periodo probatorio de {colaborador.NombreCompleto()} por vencer", hoy, limite, cancellationToken);

            foreach (var documento in colaborador.Documentos.Where(x => x.TieneVencimiento))
            {
                creadas += await UpsertAlertaAsync(colaborador, TipoAlerta.Documento, documento.DocumentoColaboradorId, documento.FechaVencimiento, $"Documento {documento.NombreArchivo} de {colaborador.NombreCompleto()} por vencer", hoy, limite, cancellationToken);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return creadas;
    }

    private async Task DesactivarAlertasFueraDeReglaAsync(DateTime limite, CancellationToken cancellationToken)
    {
        var alertas = await db.Alertas
            .Include(x => x.Colaborador)
            .ThenInclude(x => x.TipoContrato)
            .Include(x => x.Colaborador)
            .ThenInclude(x => x.Estatus)
            .Include(x => x.DocumentoColaborador)
            .Where(x =>
                x.IsActive &&
                x.EstadoAlerta != EstadoAlerta.Gestionada &&
                x.EstadoAlerta != EstadoAlerta.Ignorada)
            .ToListAsync(cancellationToken);

        foreach (var alerta in alertas)
        {
            if (DebeDesactivarse(alerta, limite))
            {
                alerta.IsActive = false;
                alerta.UpdatedAt = DateTime.UtcNow;
                alerta.UpdatedBy = User.Identity?.Name;
            }
        }
    }

    private static bool DebeDesactivarse(Alerta alerta, DateTime limite)
    {
        if (!alerta.Colaborador.IsActive || !EstatusOperativos.Contains(alerta.Colaborador.Estatus.Codigo))
        {
            return true;
        }

        if (alerta.FechaVencimiento.Date > limite)
        {
            return true;
        }

        return alerta.TipoAlerta switch
        {
            TipoAlerta.Contrato => !alerta.Colaborador.TipoContrato.RequiereFechaVencimiento,
            TipoAlerta.Licencia => !alerta.Colaborador.TieneLicencia,
            TipoAlerta.Documento => alerta.DocumentoColaborador is null || !alerta.DocumentoColaborador.IsActive || !alerta.DocumentoColaborador.TieneVencimiento,
            _ => false
        };
    }

    private async Task<int> UpsertAlertaAsync(
        Colaborador colaborador,
        TipoAlerta tipo,
        int? documentoId,
        DateTime? fechaVencimiento,
        string mensaje,
        DateTime hoy,
        DateTime limite,
        CancellationToken cancellationToken)
    {
        if (!fechaVencimiento.HasValue)
        {
            return 0;
        }

        var fecha = fechaVencimiento.Value.Date;
        if (fecha > limite)
        {
            return 0;
        }

        var estado = fecha < hoy ? EstadoAlerta.Vencida : EstadoAlerta.Pendiente;
        var existente = await db.Alertas.FirstOrDefaultAsync(x =>
            x.TipoAlerta == tipo &&
            x.ColaboradorId == colaborador.ColaboradorId &&
            x.DocumentoColaboradorId == documentoId &&
            x.FechaVencimiento == fecha,
            cancellationToken);

        if (existente is not null)
        {
            if (existente.EstadoAlerta is EstadoAlerta.Gestionada or EstadoAlerta.Ignorada)
            {
                return 0;
            }

            existente.EstadoAlerta = estado;
            existente.Mensaje = mensaje;
            existente.IsActive = true;
            return 0;
        }

        db.Alertas.Add(new Alerta
        {
            TipoAlerta = tipo,
            EstadoAlerta = estado,
            ColaboradorId = colaborador.ColaboradorId,
            DocumentoColaboradorId = documentoId,
            FechaVencimiento = fecha,
            Mensaje = mensaje,
            FechaGeneracion = DateTime.UtcNow,
            CreatedBy = User.Identity?.Name
        });
        return 1;
    }
}
