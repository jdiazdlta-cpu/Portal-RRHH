import { FormEvent, useEffect, useState } from 'react';
import { Save } from 'lucide-react';
import { apiGet, apiPost } from '../api/client';
import type { RolDto, Usuario } from '../types/api';
import { formatDate, statusClass } from '../utils/format';

export function UsuariosPage() {
  const [usuarios, setUsuarios] = useState<Usuario[]>([]);
  const [roles, setRoles] = useState<RolDto[]>([]);
  const [nombreUsuario, setNombreUsuario] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [rolId, setRolId] = useState('');
  const [error, setError] = useState('');

  const load = () => {
    Promise.all([apiGet<Usuario[]>('/usuarios'), apiGet<RolDto[]>('/catalogos/roles')])
      .then(([users, rolesData]) => {
        setUsuarios(users);
        setRoles(rolesData);
        setRolId((current) => current || String(rolesData[0]?.rolId ?? ''));
      })
      .catch((err) => setError(err instanceof Error ? err.message : 'No se pudieron cargar usuarios.'));
  };

  useEffect(load, []);

  async function create(event: FormEvent) {
    event.preventDefault();
    setError('');
    try {
      await apiPost('/usuarios', { nombreUsuario, email, password, rolId: Number(rolId), isActive: true });
      setNombreUsuario('');
      setEmail('');
      setPassword('');
      load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'No se pudo crear el usuario.');
    }
  }

  return (
    <section className="page">
      <div className="page-heading">
        <div>
          <h1>Usuarios</h1>
          <p>{usuarios.length} cuentas</p>
        </div>
      </div>
      {error && <div className="error-box">{error}</div>}
      <form className="panel form-grid" onSubmit={create}>
        <label>Usuario<input value={nombreUsuario} onChange={(event) => setNombreUsuario(event.target.value)} /></label>
        <label>Email<input type="email" value={email} onChange={(event) => setEmail(event.target.value)} /></label>
        <label>Contrasena<input type="password" value={password} onChange={(event) => setPassword(event.target.value)} /></label>
        <label>Rol<select value={rolId} onChange={(event) => setRolId(event.target.value)}>{roles.map((role) => <option key={role.rolId} value={role.rolId}>{role.nombre}</option>)}</select></label>
        <button className="primary-button"><Save size={18} />Guardar</button>
      </form>
      <div className="table-wrap">
        <table>
          <thead>
            <tr>
              <th>Usuario</th>
              <th>Email</th>
              <th>Rol</th>
              <th>Ultimo acceso</th>
              <th>Estado</th>
            </tr>
          </thead>
          <tbody>
            {usuarios.map((item) => (
              <tr key={item.usuarioId}>
                <td>{item.nombreUsuario}</td>
                <td>{item.email}</td>
                <td>{item.rol}</td>
                <td>{formatDate(item.ultimoAcceso)}</td>
                <td><span className={`badge ${statusClass(item.isActive ? 'Activo' : 'Suspendido')}`}>{item.isActive ? 'Activo' : 'Inactivo'}</span></td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  );
}
