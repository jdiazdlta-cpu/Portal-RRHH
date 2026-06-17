using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PortalRRHHFZ.Application.DTOs;
using PortalRRHHFZ.Domain.Entities;

namespace PortalRRHHFZ.Api.Controllers;

internal static class ControllerHelpers
{
    public static int CurrentUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var id) ? id : 0;
    }

    public static string NombreCompleto(this Colaborador colaborador)
    {
        return string.Join(' ', new[]
        {
            colaborador.PrimerNombre,
            colaborador.SegundoNombre,
            colaborador.PrimerApellido,
            colaborador.SegundoApellido
        }.Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    public static UsuarioDto ToDto(this Usuario usuario)
    {
        return new UsuarioDto(usuario.UsuarioId, usuario.NombreUsuario, usuario.Email, usuario.RolId, usuario.Rol.Nombre, usuario.UltimoAcceso, usuario.IsActive);
    }

    public static ColaboradorListDto ToListDto(this Colaborador colaborador)
    {
        return new ColaboradorListDto(
            colaborador.ColaboradorId,
            colaborador.NoEmpleado,
            colaborador.Cedula,
            colaborador.NombreCompleto(),
            colaborador.Empresa.Nombre,
            colaborador.Departamento.Nombre,
            colaborador.Cargo.Nombre,
            colaborador.Estatus.Nombre,
            colaborador.FechaIngreso,
            colaborador.FechaSalida,
            colaborador.IsActive);
    }

    public static DocumentoDto ToDto(this DocumentoColaborador documento)
    {
        return new DocumentoDto(
            documento.DocumentoColaboradorId,
            documento.TipoDocumentoId,
            documento.TipoDocumento.Nombre,
            documento.ColaboradorId,
            documento.NombreArchivo,
            documento.RutaArchivo,
            documento.FechaCarga,
            documento.TieneVencimiento,
            documento.FechaVencimiento,
            documento.Observacion,
            documento.IsActive);
    }

    public static AlertaDto ToDto(this Alerta alerta)
    {
        var dias = (alerta.FechaVencimiento.Date - DateTime.Today).Days;

        return new AlertaDto(
            alerta.AlertaId,
            alerta.TipoAlerta.ToString(),
            alerta.EstadoAlerta.ToString(),
            alerta.ColaboradorId,
            alerta.Colaborador.NombreCompleto(),
            alerta.DocumentoColaboradorId,
            alerta.FechaVencimiento,
            alerta.Mensaje,
            alerta.FechaGeneracion,
            alerta.FechaGestion,
            alerta.ObservacionGestion,
            alerta.Colaborador.Empresa?.Nombre ?? string.Empty,
            Math.Max(0, dias),
            Math.Max(0, -dias));
    }

    public static IQueryable<Colaborador> IncludeDetalle(this IQueryable<Colaborador> query)
    {
        return query
            .Include(x => x.Empresa)
            .Include(x => x.Departamento)
            .Include(x => x.Cargo)
            .Include(x => x.TipoContrato)
            .Include(x => x.Estatus)
            .Include(x => x.MotivoSalida)
            .Include(x => x.JefeInmediato)
            .Include(x => x.Documentos).ThenInclude(x => x.TipoDocumento)
            .Include(x => x.Alertas);
    }
}
