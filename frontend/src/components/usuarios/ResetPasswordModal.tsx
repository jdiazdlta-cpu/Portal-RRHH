import { useState } from 'react';
import { validatePassword } from './UsuarioFormModal';
import type { UsuarioList } from '../../types/usuario';

type ResetPasswordModalProps = {
  usuario: UsuarioList;
  apiErrors: string[];
  isSubmitting: boolean;
  onClose: () => void;
  onSubmit: (password: string) => Promise<void>;
};

export function ResetPasswordModal({
  apiErrors,
  isSubmitting,
  onClose,
  onSubmit,
  usuario,
}: ResetPasswordModalProps) {
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [localErrors, setLocalErrors] = useState<string[]>([]);

  const handleSubmit = async () => {
    const errors: string[] = [];

    if (!password) {
      errors.push('NuevaPassword es obligatoria.');
    } else {
      errors.push(...validatePassword(password));
    }

    if (password !== confirmPassword) {
      errors.push('NuevaPassword y ConfirmarPassword deben coincidir.');
    }

    setLocalErrors(errors);

    if (errors.length > 0) {
      return;
    }

    await onSubmit(password);
  };

  const allErrors = [...localErrors, ...apiErrors];

  return (
    <div className="modal-backdrop" role="presentation">
      <section className="modal" role="dialog" aria-modal="true" aria-label="Resetear contraseña">
        <header className="modal-header">
          <div>
            <span className="eyebrow">Reset password</span>
            <h3>{usuario.nombreUsuario}</h3>
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
                NuevaPassword<span className="required-mark">*</span>
                <input
                  type="password"
                  value={password}
                  onChange={(event) => setPassword(event.target.value)}
                />
              </label>
              <label>
                ConfirmarPassword<span className="required-mark">*</span>
                <input
                  type="password"
                  value={confirmPassword}
                  onChange={(event) => setConfirmPassword(event.target.value)}
                />
              </label>
            </div>
          </section>
        </div>

        <footer className="modal-actions sticky-actions">
          <button className="secondary-button" disabled={isSubmitting} type="button" onClick={onClose}>
            Cancelar
          </button>
          <button className="primary-button" disabled={isSubmitting} type="button" onClick={handleSubmit}>
            {isSubmitting ? 'Guardando...' : 'Resetear'}
          </button>
        </footer>
      </section>
    </div>
  );
}
