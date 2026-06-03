namespace PortalRRHHFZ.Domain.Entities;

public sealed class Usuario : AuditableEntity
{
    public int UsuarioId { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int RolId { get; set; }
    public DateTime? UltimoAcceso { get; set; }

    public Rol Rol { get; set; } = null!;
    public ICollection<DocumentoColaborador> DocumentosSubidos { get; set; } = [];
    public ICollection<Alerta> AlertasGestionadas { get; set; } = [];
    public ICollection<HistorialColaborador> Historiales { get; set; } = [];
}
