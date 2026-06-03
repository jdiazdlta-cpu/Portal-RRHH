using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortalRRHHFZ.Application.Interfaces.Catalogos;

namespace PortalRRHHFZ.Api.Controllers;

[ApiController]
[Authorize(Policy = "RequireAdminOrRRHH")]
[Route("api/catalogos")]
public sealed class CatalogosController(ICatalogoService catalogoService) : ControllerBase
{
    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles(CancellationToken cancellationToken)
    {
        var response = await catalogoService.GetRolesAsync(cancellationToken);
        return Ok(response);
    }

    [HttpGet("empresas")]
    public async Task<IActionResult> GetEmpresas(CancellationToken cancellationToken)
    {
        var response = await catalogoService.GetEmpresasAsync(cancellationToken);
        return Ok(response);
    }

    [HttpGet("departamentos")]
    public async Task<IActionResult> GetDepartamentos(
        [FromQuery] int? empresaId,
        CancellationToken cancellationToken)
    {
        var response = await catalogoService.GetDepartamentosAsync(empresaId, cancellationToken);
        return Ok(response);
    }

    [HttpGet("cargos")]
    public async Task<IActionResult> GetCargos(
        [FromQuery] int? departamentoId,
        CancellationToken cancellationToken)
    {
        var response = await catalogoService.GetCargosAsync(departamentoId, cancellationToken);
        return Ok(response);
    }

    [HttpGet("tipos-contrato")]
    public async Task<IActionResult> GetTiposContrato(CancellationToken cancellationToken)
    {
        var response = await catalogoService.GetTiposContratoAsync(cancellationToken);
        return Ok(response);
    }

    [HttpGet("estatus-colaborador")]
    public async Task<IActionResult> GetEstatusColaborador(CancellationToken cancellationToken)
    {
        var response = await catalogoService.GetEstatusColaboradorAsync(cancellationToken);
        return Ok(response);
    }

    [HttpGet("motivos-salida")]
    public async Task<IActionResult> GetMotivosSalida(CancellationToken cancellationToken)
    {
        var response = await catalogoService.GetMotivosSalidaAsync(cancellationToken);
        return Ok(response);
    }

    [HttpGet("tipos-documento")]
    public async Task<IActionResult> GetTiposDocumento(CancellationToken cancellationToken)
    {
        var response = await catalogoService.GetTiposDocumentoAsync(cancellationToken);
        return Ok(response);
    }
}
