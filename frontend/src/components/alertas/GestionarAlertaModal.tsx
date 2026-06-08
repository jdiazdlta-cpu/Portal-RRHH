import { useState } from 'react';
import type { AlertaList } from '../../types/alerta';

type GestionarAlertaModalProps = {
  alerta: AlertaList;
  action: 'gestionar' | 'ignorar';
  apiErrors: string[];
  isSubmitting: boolean;
  onClose: () => void;
  onSubmit: (observacionGestion: string | null) => Promise<void>;
};

export function GestionarAlertaModal({
  action,
  alerta,
  apiErrors,
  isSubmitting,
  onClose,
  onSubmit,
}: GestionarAlertaModalProps) {
  const [observacionGestion, setObservacionGestion] = useState('');
  const title = action === 'gestionar' ? 'Gestionar alerta' : 'Ignorar alerta';

  return (
    <div className="modal-backdrop" role="presentation">
      <section className="modal" role="dialog" aria-modal="true" aria-label={title}>
        <header className="modal-header">
          <div>
            <span className="eyebrow">{alerta.tipoAlerta}</span>
            <h3>{title}</h3>
          </div>
          <button className="icon-button" type="button" aria-label="Cerrar" onClick={onClose}>
            X
          </button>
        </header>

        {apiErrors.length > 0 && (
          <div className="form-error-list">
            {apiErrors.map((error) => (
              <span key={error}>{error}</span>
            ))}
          </div>
        )}

        <div className="form-sections">
          <section>
            <div className="alerta-modal-summary">
              <strong>{alerta.nombreCompletoColaborador}</strong>
              <span>{alerta.mensaje}</span>
            </div>
            <label className="wide-field">
              ObservacionGestion
              <textarea
                value={observacionGestion}
                onChange={(event) => setObservacionGestion(event.target.value)}
              />
            </label>
          </section>
        </div>

        <footer className="modal-actions sticky-actions">
          <button className="secondary-button" disabled={isSubmitting} type="button" onClick={onClose}>
            Cancelar
          </button>
          <button className="primary-button" disabled={isSubmitting} type="button" onClick={() => onSubmit(observacionGestion.trim() || null)}>
            {isSubmitting ? 'Procesando...' : title}
          </button>
        </footer>
      </section>
    </div>
  );
}
