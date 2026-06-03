namespace PortalRRHHFZ.Application.DTOs.Catalogos;

public sealed class CargoCatalogoDto
{
    public int CargoId { get; init; }
    public int DepartamentoId { get; init; }
    public string DepartamentoNombre { get; init; } = string.Empty;
    public int EmpresaId { get; init; }
    public string EmpresaNombre { get; init; } = string.Empty;
    public string Nombre { get; init; } = string.Empty;
}
