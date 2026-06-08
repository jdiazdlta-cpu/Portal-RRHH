import { ActionsMenu } from '../common/ActionsMenu';
import type { UsuarioList } from '../../types/usuario';

type UsuariosTableProps = {
  usuarios: UsuarioList[];
  isLoading: boolean;
  onView: (id: number) => void;
  onEdit: (id: number) => void;
  onResetPassword: (usuario: UsuarioList) => void;
  onToggleActive: (usuario: UsuarioList) => void;
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

export function UsuariosTable({
  isLoading,
  onEdit,
  onResetPassword,
  onToggleActive,
  onView,
  usuarios,
}: UsuariosTableProps) {
  return (
    <div className="table-panel">
      <div className="table-scroll">
        <table className="data-table usuarios-table">
          <thead>
            <tr>
              <th>UsuarioId</th>
              <th>NombreUsuario</th>
              <th>Email</th>
              <th>Rol</th>
              <th>IsActive</th>
              <th>UltimoAcceso</th>
              <th>CreatedAt</th>
              <th>Acciones</th>
            </tr>
          </thead>
          <tbody>
            {isLoading && (
              <tr>
                <td colSpan={8}>Cargando usuarios...</td>
              </tr>
            )}
            {!isLoading && usuarios.length === 0 && (
              <tr>
                <td colSpan={8}>No hay usuarios registrados.</td>
              </tr>
            )}
            {!isLoading &&
              usuarios.map((usuario) => (
                <tr key={usuario.usuarioId}>
                  <td>{usuario.usuarioId}</td>
                  <td>{usuario.nombreUsuario}</td>
                  <td>{usuario.email}</td>
                  <td>{usuario.rol}</td>
                  <td>
                    <span className={usuario.isActive ? 'status active' : 'status inactive'}>
                      {usuario.isActive ? 'Activo' : 'Inactivo'}
                    </span>
                  </td>
                  <td>{formatDate(usuario.ultimoAcceso)}</td>
                  <td>{formatDate(usuario.createdAt)}</td>
                  <td>
                    <ActionsMenu
                      items={[
                        { label: 'Ver detalle', onClick: () => onView(usuario.usuarioId) },
                        { label: 'Editar', onClick: () => onEdit(usuario.usuarioId) },
                        { label: 'Resetear password', onClick: () => onResetPassword(usuario) },
                        {
                          label: usuario.isActive ? 'Desactivar' : 'Activar',
                          onClick: () => onToggleActive(usuario),
                        },
                      ]}
                    />
                  </td>
                </tr>
              ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
