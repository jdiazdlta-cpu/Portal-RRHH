import { useEffect, useState } from 'react';
import type { TipoDocumentoCatalogo } from '../../types/catalogos';
import type { DocumentoColaboradorDetail, DocumentoFormValues, UpdateDocumentoRequest } from '../../types/documento';

type DocumentoEditModalProps = {
  documento: DocumentoColaboradorDetail;
  tiposDocumento: TipoDocumentoCatalogo[];
  apiErrors: string[];
  isSubmitting: boolean;
  onClose: () => void;
  onSubmit: (request: UpdateDocumentoRequest) => Promise<void>;
};

function toFormValues(documento: DocumentoColaboradorDetail): DocumentoFormValues {
  return {
    tipoDocumentoId: String(documento.tipoDocumentoId),
    tieneVencimiento: documento.tieneVencimiento,
    fechaVencimiento: documento.fechaVencimiento ? documento.fechaVencimiento.slice(0, 10) : '',
    observacion: documento.observacion ?? '',
    isActive: documento.isActive,
  };
}

export function DocumentoEditModal({
  apiErrors,
  documento,
  isSubmitting,
  onClose,
  onSubmit,
  tiposDocumento,
}: DocumentoEditModalProps) {
  const [values, setValues] = useState<DocumentoFormValues>(() => toFormValues(documento));
  const [localErrors, setLocalErrors] = useState<string[]>([]);

  useEffect(() => {
    setValues(toFormValues(documento));
    setLocalErrors([]);
  }, [documento]);

  const validate = () => {
    const errors: string[] = [];

    if (!values.tipoDocumentoId) {
      errors.push('TipoDocumentoId es obligatorio.');
    }

    if (values.tieneVencimiento && !values.fechaVencimiento) {
      errors.push('FechaVencimiento es obligatoria cuando TieneVencimiento=true.');
    }

    return errors;
  };

  const handleSubmit = async () => {
    const errors = validate();
    setLocalErrors(errors);

    if (errors.length > 0) {
      return;
    }

    await onSubmit({
      tipoDocumentoId: Number(values.tipoDocumentoId),
      tieneVencimiento: values.tieneVencimiento,
      fechaVencimiento: values.tieneVencimiento ? values.fechaVencimiento : null,
      observacion: values.observacion.trim() || null,
      isActive: values.isActive,
    });
  };

  const allErrors = [...localErrors, ...apiErrors];

  return (
    <div className="modal-backdrop" role="presentation">
      <section className="modal" role="dialog" aria-modal="true" aria-label="Editar documento">
        <header className="modal-header">
          <div>
            <span className="eyebrow">Metadata</span>
            <h3>{documento.nombreArchivo}</h3>
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
                TipoDocumentoId<span className="required-mark">*</span>
                <select
                  value={values.tipoDocumentoId}
                  onChange={(event) =>
                    setValues((current) => ({ ...current, tipoDocumentoId: event.target.value }))
                  }
                >
                  <option value="">Seleccione</option>
                  {tiposDocumento.map((tipo) => (
                    <option key={tipo.tipoDocumentoId} value={tipo.tipoDocumentoId}>
                      {tipo.nombre}
                    </option>
                  ))}
                </select>
              </label>
              <label>
                FechaVencimiento{values.tieneVencimiento && <span className="required-mark">*</span>}
                <input
                  disabled={!values.tieneVencimiento}
                  type="date"
                  value={values.fechaVencimiento}
                  onChange={(event) =>
                    setValues((current) => ({ ...current, fechaVencimiento: event.target.value }))
                  }
                />
              </label>
              <label className="checkbox-field">
                <input
                  checked={values.tieneVencimiento}
                  type="checkbox"
                  onChange={(event) =>
                    setValues((current) => ({
                      ...current,
                      tieneVencimiento: event.target.checked,
                      fechaVencimiento: event.target.checked ? current.fechaVencimiento : '',
                    }))
                  }
                />
                Tiene vencimiento
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
            <label className="wide-field">
              Observacion
              <textarea
                value={values.observacion}
                onChange={(event) =>
                  setValues((current) => ({ ...current, observacion: event.target.value }))
                }
              />
            </label>
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
