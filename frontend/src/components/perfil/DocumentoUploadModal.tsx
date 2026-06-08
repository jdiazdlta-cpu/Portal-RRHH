import { useMemo, useState } from 'react';
import type { TipoDocumentoCatalogo } from '../../types/catalogos';
import type { UploadDocumentoRequest } from '../../types/documento';

type DocumentoUploadModalProps = {
  tiposDocumento: TipoDocumentoCatalogo[];
  apiErrors: string[];
  isSubmitting: boolean;
  onClose: () => void;
  onSubmit: (request: UploadDocumentoRequest) => Promise<void>;
};

const allowedExtensions = ['.pdf', '.jpg', '.jpeg', '.png', '.doc', '.docx', '.xls', '.xlsx'];
const maxFileSize = 10 * 1024 * 1024;

function extensionOf(fileName: string) {
  const dotIndex = fileName.lastIndexOf('.');
  return dotIndex >= 0 ? fileName.slice(dotIndex).toLowerCase() : '';
}

export function validateDocumentoFile(file: File | null) {
  const errors: string[] = [];

  if (!file) {
    errors.push('Archivo es obligatorio.');
    return errors;
  }

  if (!allowedExtensions.includes(extensionOf(file.name))) {
    errors.push('Extension no permitida.');
  }

  if (file.size > maxFileSize) {
    errors.push('El archivo supera 10 MB.');
  }

  return errors;
}

export function DocumentoUploadModal({
  apiErrors,
  isSubmitting,
  onClose,
  onSubmit,
  tiposDocumento,
}: DocumentoUploadModalProps) {
  const [archivo, setArchivo] = useState<File | null>(null);
  const [tipoDocumentoId, setTipoDocumentoId] = useState('');
  const [tieneVencimiento, setTieneVencimiento] = useState(false);
  const [fechaVencimiento, setFechaVencimiento] = useState('');
  const [observacion, setObservacion] = useState('');
  const [localErrors, setLocalErrors] = useState<string[]>([]);

  const selectedTipo = useMemo(
    () => tiposDocumento.find((tipo) => tipo.tipoDocumentoId === Number(tipoDocumentoId)),
    [tipoDocumentoId, tiposDocumento],
  );

  const validate = () => {
    const errors = validateDocumentoFile(archivo);

    if (!tipoDocumentoId) {
      errors.push('TipoDocumentoId es obligatorio.');
    }

    if (tieneVencimiento && !fechaVencimiento) {
      errors.push('FechaVencimiento es obligatoria cuando TieneVencimiento=true.');
    }

    return errors;
  };

  const handleSubmit = async () => {
    const errors = validate();
    setLocalErrors(errors);

    if (errors.length > 0 || !archivo) {
      return;
    }

    await onSubmit({
      archivo,
      tipoDocumentoId: Number(tipoDocumentoId),
      tieneVencimiento,
      fechaVencimiento: tieneVencimiento ? fechaVencimiento : null,
      observacion: observacion.trim() || null,
    });
  };

  const allErrors = [...localErrors, ...apiErrors];

  return (
    <div className="modal-backdrop" role="presentation">
      <section className="modal" role="dialog" aria-modal="true" aria-label="Subir documento">
        <header className="modal-header">
          <div>
            <span className="eyebrow">Expediente digital</span>
            <h3>Subir documento</h3>
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
                Archivo<span className="required-mark">*</span>
                <input
                  type="file"
                  onChange={(event) => setArchivo(event.target.files?.[0] ?? null)}
                />
              </label>
              <label>
                TipoDocumentoId<span className="required-mark">*</span>
                <select
                  value={tipoDocumentoId}
                  onChange={(event) => {
                    const next = event.target.value;
                    setTipoDocumentoId(next);
                    const tipo = tiposDocumento.find(
                      (item) => item.tipoDocumentoId === Number(next),
                    );
                    setTieneVencimiento(Boolean(tipo?.tieneVencimientoSugerido));
                  }}
                >
                  <option value="">Seleccione</option>
                  {tiposDocumento.map((tipo) => (
                    <option key={tipo.tipoDocumentoId} value={tipo.tipoDocumentoId}>
                      {tipo.nombre}
                    </option>
                  ))}
                </select>
              </label>
              <label className="checkbox-field">
                <input
                  checked={tieneVencimiento}
                  type="checkbox"
                  onChange={(event) => setTieneVencimiento(event.target.checked)}
                />
                Tiene vencimiento
              </label>
              <label>
                FechaVencimiento{tieneVencimiento && <span className="required-mark">*</span>}
                <input
                  disabled={!tieneVencimiento}
                  type="date"
                  value={fechaVencimiento}
                  onChange={(event) => setFechaVencimiento(event.target.value)}
                />
              </label>
            </div>
            {selectedTipo?.tieneVencimientoSugerido && (
              <span className="hint-text">Este tipo de documento sugiere fecha de vencimiento.</span>
            )}
            <label className="wide-field">
              Observacion
              <textarea value={observacion} onChange={(event) => setObservacion(event.target.value)} />
            </label>
          </section>
        </div>

        <footer className="modal-actions sticky-actions">
          <button className="secondary-button" disabled={isSubmitting} type="button" onClick={onClose}>
            Cancelar
          </button>
          <button className="primary-button" disabled={isSubmitting} type="button" onClick={handleSubmit}>
            {isSubmitting ? 'Subiendo...' : 'Subir'}
          </button>
        </footer>
      </section>
    </div>
  );
}
