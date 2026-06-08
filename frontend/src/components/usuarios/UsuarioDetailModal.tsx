import type { UsuarioDetail } from '../../types/usuario';

type UsuarioDetailModalProps = {
  usuario: UsuarioDetail;
  onClose: () => void;
};

function formatDate(value: string | null) {
  if (!value) {
    return '-';
  }

  return new Intl.DateTimeFormat('es-PA', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  }).format(new Date(value));
}

function row(label: string, value: string | number | boolean | null | undefined) {
  let displayValue: string | number = '-';

  if (typeof value === 'boolean') {
    displayValue = value ? 'Si' : 'No';
  } else if (value !== null && value !== undefined && value !== '') {
    displayValue = value;
  }

  return (
    <div className="detail-row">
      <span>{label}</span>
      <strong>{displayValue}</strong>
    </div>
  );
}

export function UsuarioDetailModal({ onClose, usuario }: UsuarioDetailModalProps) {
  return (
    <div className="modal-backdrop" role="presentation">
      <section className="modal" role="dialog" aria-modal="true" aria-label="Detalle de usuario">
        <header className="modal-header">
          <div>
            <span className="eyebrow">Detalle basico</span>
            <h3>{usuario.nombreUsuario}</h3>
          </div>
          <button className="icon-button" type="button" aria-label="Cerrar" onClick={onClose}>
            X
          </button>
        </header>

        <div className="detail-grid single-detail-grid">
          <section>
            <h4>Usuario</h4>
            {row('UsuarioId', usuario.usuarioId)}
            {row('NombreUsuario', usuario.nombreUsuario)}
            {row('Email', usuario.email)}
            {row('Rol', usuario.rol)}
            {row('IsActive', usuario.isActive)}
            {row('UltimoAcceso', formatDate(usuario.ultimoAcceso))}
            {row('CreatedAt', formatDate(usuario.createdAt))}
            {row('UpdatedAt', formatDate(usuario.updatedAt))}
            {row('CreatedBy', usuario.createdBy)}
            {row('UpdatedBy', usuario.updatedBy)}
          </section>
        </div>
      </section>
    </div>
  );
}
