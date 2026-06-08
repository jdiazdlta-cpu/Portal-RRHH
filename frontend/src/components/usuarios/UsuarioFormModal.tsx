import { useEffect, useState } from 'react';
import type { RolCatalogo } from '../../types/catalogos';
import type {
  CreateUsuarioRequest,
  UpdateUsuarioRequest,
  UsuarioDetail,
  UsuarioFormValues,
} from '../../types/usuario';

type UsuarioFormModalProps = {
  mode: 'create' | 'edit';
  usuario: UsuarioDetail | null;
  roles: RolCatalogo[];
  apiErrors: string[];
  isSubmitting: boolean;
  onClose: () => void;
  onSubmit: (request: CreateUsuarioRequest | UpdateUsuarioRequest) => Promise<void>;
};

const emptyValues: UsuarioFormValues = {
  nombreUsuario: '',
  email: '',
  password: '',
  rolId: '',
  isActive: true,
};

function toFormValues(usuario: UsuarioDetail | null): UsuarioFormValues {
  if (!usuario) {
    return emptyValues;
  }

  return {
    nombreUsuario: usuario.nombreUsuario,
    email: usuario.email,
    password: '',
    rolId: String(usuario.rolId),
    isActive: usuario.isActive,
  };
}

export function validatePassword(password: string) {
  const errors: string[] = [];

  if (password.length < 8) {
    errors.push('Password debe tener al menos 8 caracteres.');
  }

  if (!/[A-Z]/.test(password)) {
    errors.push('Password debe tener al menos una mayuscula.');
  }

  if (!/[0-9]/.test(password)) {
    errors.push('Password debe tener al menos un numero.');
  }

  if (!/[^A-Za-z0-9]/.test(password)) {
    errors.push('Password debe tener al menos un simbolo.');
  }

  return errors;
}

function isValidEmail(email: string) {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
}

export function UsuarioFormModal({
  apiErrors,
  isSubmitting,
  mode,
  onClose,
  onSubmit,
  roles,
  usuario,
}: UsuarioFormModalProps) {
  const [values, setValues] = useState<UsuarioFormValues>(() => toFormValues(usuario));
  const [localErrors, setLocalErrors] = useState<string[]>([]);

  useEffect(() => {
    setValues(toFormValues(usuario));
    setLocalErrors([]);
  }, [usuario]);

  const validate = () => {
    const errors: string[] = [];

    if (!values.nombreUsuario.trim()) errors.push('NombreUsuario es obligatorio.');
    if (!values.email.trim()) errors.push('Email es obligatorio.');
    if (values.email.trim() && !isValidEmail(values.email.trim())) {
      errors.push('Email debe tener formato valido.');
    }
    if (!values.rolId) errors.push('RolId es obligatorio.');

    if (mode === 'create') {
      if (!values.password) {
        errors.push('Password es obligatoria al crear.');
      } else {
        errors.push(...validatePassword(values.password));
      }
    }

    return errors;
  };

  const handleSubmit = async () => {
    const errors = validate();
    setLocalErrors(errors);

    if (errors.length > 0) {
      return;
    }

    const baseRequest = {
      nombreUsuario: values.nombreUsuario.trim(),
      email: values.email.trim(),
      rolId: Number(values.rolId),
      isActive: values.isActive,
    };

    await onSubmit(
      mode === 'create'
        ? { ...baseRequest, password: values.password }
        : baseRequest,
    );
  };

  const allErrors = [...localErrors, ...apiErrors];

  return (
    <div className="modal-backdrop" role="presentation">
      <section className="modal" role="dialog" aria-modal="true" aria-label="Formulario de usuario">
        <header className="modal-header">
          <div>
            <span className="eyebrow">{mode === 'create' ? 'Nuevo usuario' : 'Editar usuario'}</span>
            <h3>{mode === 'create' ? 'Crear usuario' : 'Editar usuario'}</h3>
          </div>
          <button className="icon-button" type="button" aria-label="Cerrar" onClick={onClose}>
            X
          </button>
        </header>

        {allErrors.length > 0 && (
          <div className="form-error-list">
            {allErrors.map((error) => (
              <span key={error}>{error}</span>
            ))}
          </div>
        )}

        <div className="form-sections">
          <section>
            <div className="form-grid two-columns">
              <label>
                NombreUsuario<span className="required-mark">*</span>
                <input
                  value={values.nombreUsuario}
                  onChange={(event) =>
                    setValues((current) => ({ ...current, nombreUsuario: event.target.value }))
                  }
                />
              </label>
              <label>
                Email<span className="required-mark">*</span>
                <input
                  type="email"
                  value={values.email}
                  onChange={(event) =>
                    setValues((current) => ({ ...current, email: event.target.value }))
                  }
                />
              </label>
              {mode === 'create' && (
                <label>
                  Password<span className="required-mark">*</span>
                  <input
                    type="password"
                    value={values.password}
                    onChange={(event) =>
                      setValues((current) => ({ ...current, password: event.target.value }))
                    }
                  />
                </label>
              )}
              <label>
                RolId<span className="required-mark">*</span>
                <select
                  value={values.rolId}
                  onChange={(event) =>
                    setValues((current) => ({ ...current, rolId: event.target.value }))
                  }
                >
                  <option value="">Seleccione</option>
                  {roles.map((rol) => (
                    <option key={rol.rolId} value={rol.rolId}>
                      {rol.nombre}
                    </option>
                  ))}
                </select>
              </label>
              <label className="checkbox-field">
                <input
                  checked={values.isActive}
                  type="checkbox"
                  onChange={(event) =>
                    setValues((current) => ({ ...current, isActive: event.target.checked }))
                  }
                />
                IsActive
              </label>
            </div>
          </section>
        </div>

        <footer className="modal-actions sticky-actions">
          <button className="secondary-button" disabled={isSubmitting} type="button" onClick={onClose}>
            Cancelar
          </button>
          <button className="primary-button" disabled={isSubmitting} type="button" onClick={handleSubmit}>
            {isSubmitting ? 'Guardando...' : 'Guardar'}
          </button>
        </footer>
      </section>
    </div>
  );
}
