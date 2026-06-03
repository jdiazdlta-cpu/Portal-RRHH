namespace PortalRRHHFZ.Application.DTOs.Catalogos;

public sealed class DepartamentoCatalogoDto
{
    public int DepartamentoId { get; init; }
    public int EmpresaId { get; init; }
    public string EmpresaNombre { get; init; } = string.Empty;
    public string Nombre { get; init; } = string.Empty;
}
