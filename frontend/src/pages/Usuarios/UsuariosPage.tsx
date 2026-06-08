import { useEffect, useMemo, useState } from 'react';
import { getRolesCatalogo } from '../../api/catalogosApi';
import {
  activarUsuario,
  createUsuario,
  desactivarUsuario,
  getUsuarioById,
  getUsuarios,
  resetUsuarioPassword,
  updateUsuario,
} from '../../api/usuariosApi';
import { ApiError } from '../../api/httpClient';
import { ConfirmDialog } from '../../components/colaboradores/ConfirmDialog';
import { ResetPasswordModal } from '../../components/usuarios/ResetPasswordModal';
import { UsuarioDetailModal } from '../../components/usuarios/UsuarioDetailModal';
import { UsuarioFormModal } from '../../components/usuarios/UsuarioFormModal';
import { UsuariosTable } from '../../components/usuarios/UsuariosTable';
import type { RolCatalogo } from '../../types/catalogos';
import type {
  CreateUsuarioRequest,
  UpdateUsuarioRequest,
  UsuarioDetail,
  UsuarioList,
} from '../../types/usuario';

type FormState =
  | {
      mode: 'create';
      usuario: null;
    }
  | {
      mode: 'edit';
      usuario: UsuarioDetail;
    };

type ConfirmState = {
  usuario: UsuarioList;
  action: 'activar' | 'desactivar';
};

function getErrorMessages(error: unknown) {
  if (error instanceof ApiError) {
    return error.errors.length > 0 ? error.errors : [error.message];
  }

  if (error instanceof Error) {
    return [error.message];
  }

  return ['No fue posible completar la operacion.'];
}

export function UsuariosPage() {
  const [usuarios, setUsuarios] = useState<UsuarioList[]>([]);
  const [roles, setRoles] = useState<RolCatalogo[]>([]);
  const [detail, setDetail] = useState<UsuarioDetail | null>(null);
  const [formState, setFormState] = useState<FormState | null>(null);
  const [resetState, setResetState] = useState<UsuarioList | null>(null);
  const [confirmState, setConfirmState] = useState<ConfirmState | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [errors, setErrors] = useState<string[]>([]);
  const [modalErrors, setModalErrors] = useState<string[]>([]);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const totals = useMemo(
    () => ({
      total: usuarios.length,
      activos: usuarios.filter((usuario) => usuario.isActive).length,
      inactivos: usuarios.filter((usuario) => !usuario.isActive).length,
    }),
    [usuarios],
  );

  const loadData = async () => {
    setIsLoading(true);
    setErrors([]);

    try {
      const [usuariosResponse, rolesResponse] = await Promise.all([
        getUsuarios(),
        getRolesCatalogo(),
      ]);

      setUsuarios(usuariosResponse.data ?? []);
      setRoles(rolesResponse.data ?? []);
    } catch (error) {
      setErrors(getErrorMessages(error));
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    document.title = 'Usuarios | Portal RRHH FZ';
    void loadData();
  }, []);

  const openDetail = async (id: number) => {
    setErrors([]);

    try {
      const response = await getUsuarioById(id);

      if (response.data) {
        setDetail(response.data);
      }
    } catch (error) {
      setErrors(getErrorMessages(error));
    }
  };

  const openEdit = async (id: number) => {
    setModalErrors([]);
    setErrors([]);

    try {
      const response = await getUsuarioById(id);

      if (response.data) {
        setFormState({ mode: 'edit', usuario: response.data });
      }
    } catch (error) {
      setErrors(getErrorMessages(error));
    }
  };

  const handleSubmit = async (request: CreateUsuarioRequest | UpdateUsuarioRequest) => {
    if (!formState) {
      return;
    }

    setIsSaving(true);
    setModalErrors([]);
    setSuccessMessage(null);

    try {
      if (formState.mode === 'create') {
        await createUsuario(request as CreateUsuarioRequest);
        setSuccessMessage('Usuario creado correctamente.');
      } else {
        await updateUsuario(formState.usuario.usuarioId, request as UpdateUsuarioRequest);
        setSuccessMessage('Usuario actualizado correctamente.');
      }

      setFormState(null);
      await loadData();
    } catch (error) {
      setModalErrors(getErrorMessages(error));
    } finally {
      setIsSaving(false);
    }
  };

  const handleResetPassword = async (password: string) => {
    if (!resetState) {
      return;
    }

    setIsSaving(true);
    setModalErrors([]);
    setSuccessMessage(null);

    try {
      await resetUsuarioPassword(resetState.usuarioId, { password });
      setResetState(null);
      setSuccessMessage('Password reseteada correctamente.');
      await loadData();
    } catch (error) {
      setModalErrors(getErrorMessages(error));
    } finally {
      setIsSaving(false);
    }
  };

  const handleToggleActive = async () => {
    if (!confirmState) {
      return;
    }

    setIsSaving(true);
    setErrors([]);
    setSuccessMessage(null);

    try {
      if (confirmState.action === 'activar') {
        await activarUsuario(confirmState.usuario.usuarioId);
        setSuccessMessage('Usuario activado correctamente.');
      } else {
        await desactivarUsuario(confirmState.usuario.usuarioId);
        setSuccessMessage('Usuario desactivado correctamente.');
      }

      setConfirmState(null);
      await loadData();
    } catch (error) {
      setErrors(getErrorMessages(error));
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <div className="usuarios-page">
      <section className="page-heading">
        <div>
          <span className="eyebrow">Solo Admin</span>
          <h2>Usuarios</h2>
        </div>
        <button
          className="primary-button"
          disabled={isLoading}
          type="button"
          onClick={() => {
            setModalErrors([]);
            setFormState({ mode: 'create', usuario: null });
          }}
        >
          Nuevo usuario
        </button>
      </section>

      <section className="summary-strip" aria-label="Resumen de usuarios">
        <article>
          <span>Total usuarios</span>
          <strong>{totals.total}</strong>
        </article>
        <article>
          <span>Activos</span>
          <strong>{totals.activos}</strong>
        </article>
        <article>
          <span>Inactivos</span>
          <strong>{totals.inactivos}</strong>
        </article>
      </section>

      {successMessage && <div className="success-message">{successMessage}</div>}
      {errors.length > 0 && (
        <div className="form-error-list">
          {errors.map((error) => (
            <span key={error}>{error}</span>
          ))}
        </div>
      )}

      <UsuariosTable
        isLoading={isLoading}
        usuarios={usuarios}
        onEdit={openEdit}
        onResetPassword={(usuario) => {
          setModalErrors([]);
          setResetState(usuario);
        }}
        onToggleActive={(usuario) =>
          setConfirmState({
            usuario,
            action: usuario.isActive ? 'desactivar' : 'activar',
          })
        }
        onView={openDetail}
      />

      {formState && (
        <UsuarioFormModal
          apiErrors={modalErrors}
          isSubmitting={isSaving}
          mode={formState.mode}
          roles={roles}
          usuario={formState.usuario}
          onClose={() => setFormState(null)}
          onSubmit={handleSubmit}
        />
      )}

      {detail && <UsuarioDetailModal usuario={detail} onClose={() => setDetail(null)} />}

      {resetState && (
        <ResetPasswordModal
          apiErrors={modalErrors}
          isSubmitting={isSaving}
          usuario={resetState}
          onClose={() => setResetState(null)}
          onSubmit={handleResetPassword}
        />
      )}

      {confirmState && (
        <ConfirmDialog
          confirmLabel={confirmState.action === 'activar' ? 'Activar' : 'Desactivar'}
          isBusy={isSaving}
          message={`Confirma que deseas ${confirmState.action} a ${confirmState.usuario.nombreUsuario}.`}
          title={confirmState.action === 'activar' ? 'Activar usuario' : 'Desactivar usuario'}
          onCancel={() => setConfirmState(null)}
          onConfirm={handleToggleActive}
        />
      )}
    </div>
  );
}
