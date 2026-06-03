type ConfirmDialogProps = {
  title: string;
  message: string;
  confirmLabel: string;
  isBusy: boolean;
  onCancel: () => void;
  onConfirm: () => void;
};

export function ConfirmDialog({
  confirmLabel,
  isBusy,
  message,
  onCancel,
  onConfirm,
  title,
}: ConfirmDialogProps) {
  return (
    <div className="modal-backdrop" role="presentation">
      <section className="confirm-dialog" role="dialog" aria-modal="true" aria-label={title}>
        <h3>{title}</h3>
        <p>{message}</p>
        <div className="modal-actions">
          <button className="secondary-button" disabled={isBusy} type="button" onClick={onCancel}>
            Cancelar
          </button>
          <button className="danger-button" disabled={isBusy} type="button" onClick={onConfirm}>
            {isBusy ? 'Procesando...' : confirmLabel}
          </button>
        </div>
      </section>
    </div>
  );
}
