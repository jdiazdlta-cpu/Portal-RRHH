import { FormEvent, useEffect, useState } from 'react';
import { Plus, Save, X } from 'lucide-react';
import { apiGet, apiPost } from '../api/client';
import { useAuth } from '../auth/AuthContext';
import type { RolDto, Usuario } from '../types/api';
import { formatDate, statusClass } from '../utils/format';

type UsuarioFormState = {
  nombreUsuario: string;
  email: string;
  rolId: string;
  password: string;
  confirmPassword: string;
  isActive: boolean;
};

const emptyUsuarioForm: UsuarioFormState = {
  nombreUsuario: '',
  email: '',
  rolId: '',
  password: '',
  confirmPassword: '',
  isActive: true
};

export function UsuariosPage() {
  const { hasRole } = useAuth();
  const canCreate = hasRole(['Admin']);
  const [usuarios, setUsuarios] = useState<Usuario[]>([]);
  const [roles, setRoles] = useState<RolDto[]>([]);
  const [form, setForm] = useState<UsuarioFormState | null>(null);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [formError, setFormError] = useState('');
  const [saving, setSaving] = useState(false);

  const load = () => {
    Promise.all([apiGet<Usuario[]>('/usuarios'), apiGet<RolDto[]>('/catalogos/roles')])
      .then(([users, rolesData]) => {
        setUsuarios(users);
        setRoles(rolesData);
      })
      .catch((err) => setError(err instanceof Error ? err.message : 'No se pudieron cargar usuarios.'));
  };

  useEffect(load, []);

  function openCreate() {
    setForm({ ...emptyUsuarioForm, rolId: String(roles[0]?.rolId ?? '') });
    setFormError('');
    setNotice('');
  }

  function closeModal() {
    setForm(null);
    setFormError('');
  }

  async function create(event: FormEvent) {
    event.preventDefault();
    if (!form) return;

    setSaving(true);
    setFormError('');
    setNotice('');
    try {
      if (form.password !== form.confirmPassword) {
        throw new Error('La confirmacion de contrasena no coincide.');
      }

      await apiPost('/usuarios', {
        nombreUsuario: form.nombreUsuario.trim(),
        email: form.email.trim(),
        rolId: Number(form.rolId),
        password: form.password,
        confirmPassword: form.confirmPassword,
        isActive: form.isActive
      });
      setForm(null);
      setNotice('Usuario creado correctamente.');
      load();
    } catch (err) {
      setFormError(err instanceof Error ? err.message : 'No se pudo crear el usuario.');
    } finally {
      setSaving(false);
    }
  }

  return (
    <section className="page">
      <div className="page-heading">
        <div>
          <h1>Usuarios</h1>
          <p>{usuarios.length} cuentas</p>
        </div>
        {canCreate && (
          <button className="primary-button" onClick={openCreate} type="button">
            <Plus size={18} />
            Nuevo usuario
          </button>
        )}
      </div>
      {error && <div className="error-box">{error}</div>}
      {notice && <div className="success-box">{notice}</div>}

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
            {usuarios.length === 0 && (
              <tr><td colSpan={5}><div className="empty-state">Sin usuarios</div></td></tr>
            )}
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

      {form && (
        <div className="modal-backdrop" role="presentation">
          <section className="modal-panel user-modal" role="dialog" aria-modal="true" aria-labelledby="nuevo-usuario-title">
            <div className="modal-header">
              <div>
                <h2 id="nuevo-usuario-title">Nuevo usuario</h2>
                <p>Cuenta de acceso al portal</p>
              </div>
              <button className="icon-button light" onClick={closeModal} type="button" title="Cerrar" aria-label="Cerrar">
                <X size={18} />
              </button>
            </div>
            {formError && <div className="error-box">{formError}</div>}
            <form className="edit-modal-form" onSubmit={create}>
              <div className="form-section">
                <h3>Datos de acceso</h3>
                <div className="edit-form-grid">
                  <label>
                    Nombre de usuario
                    <input value={form.nombreUsuario} onChange={(event) => setForm((current) => current ? { ...current, nombreUsuario: event.target.value } : current)} required />
                  </label>
                  <label>
                    Email
                    <input type="email" value={form.email} onChange={(event) => setForm((current) => current ? { ...current, email: event.target.value } : current)} required />
                  </label>
                  <label>
                    Rol
                    <select value={form.rolId} onChange={(event) => setForm((current) => current ? { ...current, rolId: event.target.value } : current)} required>
                      <option value="">Seleccione</option>
                      {roles.map((role) => <option key={role.rolId} value={role.rolId}>{role.nombre}</option>)}
                    </select>
                  </label>
                  <label className="check-label compact-check">
                    <input type="checkbox" checked={form.isActive} onChange={(event) => setForm((current) => current ? { ...current, isActive: event.target.checked } : current)} />
                    Activo
                  </label>
                  <label>
                    Contrasena temporal
                    <input type="password" value={form.password} onChange={(event) => setForm((current) => current ? { ...current, password: event.target.value } : current)} required />
                  </label>
                  <label>
                    Confirmar contrasena
                    <input type="password" value={form.confirmPassword} onChange={(event) => setForm((current) => current ? { ...current, confirmPassword: event.target.value } : current)} required />
                  </label>
                </div>
              </div>
              <div className="modal-actions">
                <button className="secondary-button" type="button" onClick={closeModal}>Cancelar</button>
                <button className="primary-button" disabled={saving} type="submit">
                  <Save size={18} />
                  {saving ? 'Guardando...' : 'Crear usuario'}
                </button>
              </div>
            </form>
          </section>
        </div>
      )}
    </section>
  );
}
