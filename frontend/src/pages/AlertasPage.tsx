import { useEffect, useMemo, useState } from 'react';
import type { Dispatch, FormEvent, SetStateAction } from 'react';
import { Check, MoreVertical, RefreshCw, Save, X } from 'lucide-react';
import { Link } from 'react-router-dom';
import { apiGet, apiPatch, apiPost } from '../api/client';
import type { Alerta, AlertaGestionCorreccion, CatalogoItem, ColaboradorDetalle, Documento } from '../types/api';
import { formatDate, statusClass } from '../utils/format';

type ResumenAlertas = {
  pendientes: number;
  vencidas: number;
  gestionadas: number;
  ignoradas: number;
};

const tipoAlertaOptions = [
  { value: '', label: 'Todos' },
  { value: 'Cedula', label: 'Cedula' },
  { value: 'Licencia', label: 'Licencia' },
  { value: 'Contrato', label: 'Contrato' },
  { value: 'PeriodoProbatorio', label: 'Periodo probatorio' },
  { value: 'Documento', label: 'Documento' }
];

type GestionMode = 'gestionar' | 'ignorar';
type ResultadoGestionContrato = '' | 'RenovoEventual' | 'PasoPermanente' | 'PasoCesante' | 'Excepcion';

const resultadoContratoOptions: Array<{ value: ResultadoGestionContrato; label: string }> = [
  { value: '', label: 'Seleccione resultado' },
  { value: 'RenovoEventual', label: 'Renovo contrato eventual' },
  { value: 'PasoPermanente', label: 'Paso a permanente' },
  { value: 'PasoCesante', label: 'Paso a cesante' },
  { value: 'Excepcion', label: 'Gestionar sin cambio por excepcion' }
];

type GestionState = {
  alerta: Alerta;
  mode: GestionMode;
  colaborador: ColaboradorDetalle | null;
  documento: Documento | null;
  observacionGestion: string;
  gestionarSinCambio: boolean;
  resultadoGestionContrato: ResultadoGestionContrato;
  fechaVencimientoCedula: string;
  tieneLicencia: boolean;
  numeroLicencia: string;
  tipoLicencia: string;
  fechaVencimientoLicencia: string;
  tipoContratoId: string;
  fechaVencimientoContrato: string;
  estatusId: string;
  fechaSalida: string;
  motivoSalidaId: string;
  fechaVencimientoPeriodoProbatorio: string;
  fechaVencimientoDocumento: string;
  observacionDocumento: string;
};

export function AlertasPage() {
  const [items, setItems] = useState<Alerta[]>([]);
  const [resumen, setResumen] = useState<ResumenAlertas | null>(null);
  const [estado, setEstado] = useState('');
  const [tipoAlerta, setTipoAlerta] = useState('');
  const [tiposContrato, setTiposContrato] = useState<CatalogoItem[]>([]);
  const [estatus, setEstatus] = useState<CatalogoItem[]>([]);
  const [motivosSalida, setMotivosSalida] = useState<CatalogoItem[]>([]);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [busy, setBusy] = useState(false);
  const [modal, setModal] = useState<GestionState | null>(null);
  const [modalError, setModalError] = useState('');

  const load = () => {
    const params = new URLSearchParams();
    if (estado) params.set('estado', estado);
    if (tipoAlerta) params.set('tipoAlerta', tipoAlerta);
    const qs = params.toString() ? `?${params.toString()}` : '';
    Promise.all([apiGet<Alerta[]>(`/alertas${qs}`), apiGet<ResumenAlertas>('/alertas/resumen')])
      .then(([alerts, summary]) => {
        setItems(alerts);
        setResumen(summary);
      })
      .catch((err) => setError(err instanceof Error ? err.message : 'No se pudieron cargar alertas.'));
  };

  useEffect(load, [estado, tipoAlerta]);

  useEffect(() => {
    Promise.all([
      apiGet<CatalogoItem[]>('/catalogos/tipos-contrato'),
      apiGet<CatalogoItem[]>('/catalogos/estatus-colaborador'),
      apiGet<CatalogoItem[]>('/catalogos/motivos-salida')
    ])
      .then(([contracts, statuses, exitReasons]) => {
        setTiposContrato(contracts);
        setEstatus(statuses);
        setMotivosSalida(exitReasons);
      })
      .catch((err) => setError(err instanceof Error ? err.message : 'No se pudieron cargar catalogos.'));
  }, []);

  async function recalcular() {
    setBusy(true);
    setError('');
    setNotice('');
    try {
      await apiPost('/alertas/recalcular');
      setNotice('Alertas recalculadas.');
      load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'No se pudieron recalcular alertas.');
    } finally {
      setBusy(false);
    }
  }

  async function openModal(alerta: Alerta, mode: GestionMode) {
    setError('');
    setNotice('');
    setModalError('');
    try {
      const colaborador = await apiGet<ColaboradorDetalle>(`/colaboradores/${alerta.colaboradorId}/perfil`);
      const documento = alerta.documentoColaboradorId
        ? colaborador.documentos.find((item) => item.documentoColaboradorId === alerta.documentoColaboradorId) ?? null
        : null;
      setModal(toGestionState(alerta, mode, colaborador, documento));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'No se pudo abrir la gestion de alerta.');
    }
  }

  async function submitGestion(event: FormEvent) {
    event.preventDefault();
    if (!modal) return;

    if (!modal.observacionGestion.trim()) {
      setModalError('La observacion es obligatoria.');
      return;
    }

    const contratoValidation = validateContratoModal(modal);
    if (contratoValidation) {
      setModalError(contratoValidation);
      return;
    }

    setBusy(true);
    setModalError('');
    try {
      if (modal.mode === 'ignorar') {
        await apiPatch(`/alertas/${modal.alerta.alertaId}/ignorar`, { observacionGestion: modal.observacionGestion.trim() });
        setNotice('Alerta ignorada.');
      } else {
        await apiPatch(`/alertas/${modal.alerta.alertaId}/gestionar-con-correccion`, buildPayload(modal));
        setNotice('Alerta gestionada y dato actualizado.');
      }

      setModal(null);
      load();
    } catch (err) {
      setModalError(err instanceof Error ? err.message : 'No se pudo actualizar la alerta.');
    } finally {
      setBusy(false);
    }
  }

  return (
    <section className="page">
      <div className="page-heading">
        <div>
          <h1>Alertas</h1>
          <p>{items.length} registros visibles</p>
        </div>
        <button className="primary-button" onClick={recalcular} disabled={busy}>
          <RefreshCw size={18} />
          Recalcular
        </button>
      </div>
      {error && <div className="error-box">{error}</div>}
      {notice && <div className="success-box">{notice}</div>}
      <div className="metric-grid four">
        <Stat label="Pendientes" value={resumen?.pendientes ?? 0} />
        <Stat label="Vencidas" value={resumen?.vencidas ?? 0} />
        <Stat label="Gestionadas" value={resumen?.gestionadas ?? 0} />
        <Stat label="Ignoradas" value={resumen?.ignoradas ?? 0} />
      </div>
      <div className="filter-row compact alertas-filter-row">
        <label>
          Estado
          <select value={estado} onChange={(event) => setEstado(event.target.value)}>
            <option value="">Todos</option>
            <option value="Pendiente">Pendiente</option>
            <option value="Vencida">Vencida</option>
            <option value="Gestionada">Gestionada</option>
            <option value="Ignorada">Ignorada</option>
          </select>
        </label>
        <label>
          Tipo de vencimiento
          <select value={tipoAlerta} onChange={(event) => setTipoAlerta(event.target.value)}>
            {tipoAlertaOptions.map((item) => <option key={item.value || 'all'} value={item.value}>{item.label}</option>)}
          </select>
        </label>
      </div>
      <div className="table-wrap">
        <table>
          <thead>
            <tr>
              <th>Estado</th>
              <th>Tipo</th>
              <th>Colaborador</th>
              <th>Empresa</th>
              <th>Vencimiento</th>
              <th>Plazo</th>
              <th>Mensaje</th>
              <th>Acciones</th>
            </tr>
          </thead>
          <tbody>
            {items.length === 0 && <tr><td colSpan={8}><div className="empty-state">Sin alertas</div></td></tr>}
            {items.map((item) => (
              <tr key={item.alertaId}>
                <td><span className={`badge ${statusClass(item.estadoAlerta)}`}>{item.estadoAlerta}</span></td>
                <td>{item.tipoAlerta}</td>
                <td><Link className="inline-link" to={`/colaboradores/${item.colaboradorId}`}>{item.colaborador}</Link></td>
                <td>{item.empresa || 'N/D'}</td>
                <td>{formatDate(item.fechaVencimiento)}</td>
                <td><span className={`alert-timing ${alertToneClass(item)}`}>{alertTimingText(item)}</span></td>
                <td>{item.mensaje}</td>
                <td>
                  <details className="action-menu">
                    <summary aria-label="Abrir acciones"><MoreVertical size={18} /></summary>
                    <div className="action-menu-popover">
                      <button type="button" onClick={() => openModal(item, 'gestionar')}><Check size={16} />Gestionar</button>
                      <button type="button" onClick={() => openModal(item, 'ignorar')}><X size={16} />Ignorar</button>
                    </div>
                  </details>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {modal && (
        <div className="modal-backdrop" role="presentation">
          <section className="modal-panel alerta-modal" role="dialog" aria-modal="true" aria-labelledby="gestion-alerta-title">
            <div className="modal-header">
              <div>
                <h2 id="gestion-alerta-title">{modal.mode === 'gestionar' ? 'Gestionar alerta' : 'Ignorar alerta'}</h2>
                <p>{modal.alerta.tipoAlerta} - {modal.alerta.colaborador}</p>
              </div>
              <button className="icon-button light" onClick={() => setModal(null)} type="button" title="Cerrar" aria-label="Cerrar">
                <X size={18} />
              </button>
            </div>
            {modalError && <div className="error-box">{modalError}</div>}
            <form className="edit-modal-form" onSubmit={submitGestion}>
              <div className="alert-context">
                <span><strong>Estado actual</strong>{modal.alerta.estadoAlerta}</span>
                <span><strong>Vencimiento alerta</strong>{formatDate(modal.alerta.fechaVencimiento)}</span>
                <span><strong>Mensaje</strong>{modal.alerta.mensaje}</span>
              </div>

              {modal.mode === 'gestionar' && (
                <div className="form-section">
                  <h3>Dato a corregir</h3>
                  <GestionFields modal={modal} setModal={setModal} tiposContrato={tiposContrato} estatus={estatus} motivosSalida={motivosSalida} />
                  {modal.alerta.tipoAlerta !== 'Contrato' && (
                    <label className="check-label compact-check">
                      <input
                        type="checkbox"
                        checked={modal.gestionarSinCambio}
                        onChange={(event) => updateModal(setModal, 'gestionarSinCambio', event.target.checked)}
                      />
                      Gestionar sin cambio por excepcion
                    </label>
                  )}
                </div>
              )}

              <div className="form-section">
                <h3>Observacion</h3>
                <label>
                  Observacion de gestion
                  <textarea
                    value={modal.observacionGestion}
                    onChange={(event) => updateModal(setModal, 'observacionGestion', event.target.value)}
                    placeholder={modal.mode === 'ignorar' ? 'Explique por que se ignora la alerta' : 'Indique la correccion realizada o la excepcion aprobada'}
                    required
                  />
                </label>
              </div>

              <div className="modal-actions">
                <button className="secondary-button" type="button" onClick={() => setModal(null)}>Cancelar</button>
                <button className="primary-button" disabled={busy} type="submit">
                  <Save size={18} />
                  {busy ? 'Guardando...' : modal.mode === 'gestionar' ? 'Guardar gestion' : 'Ignorar alerta'}
                </button>
              </div>
            </form>
          </section>
        </div>
      )}
    </section>
  );
}

function GestionFields({
  modal,
  setModal,
  tiposContrato,
  estatus,
  motivosSalida
}: {
  modal: GestionState;
  setModal: Dispatch<SetStateAction<GestionState | null>>;
  tiposContrato: CatalogoItem[];
  estatus: CatalogoItem[];
  motivosSalida: CatalogoItem[];
}) {
  const current = useMemo(() => modal.colaborador, [modal.colaborador]);

  if (!current) {
    return <div className="empty-state">Sin datos del colaborador</div>;
  }

  switch (modal.alerta.tipoAlerta) {
    case 'Cedula':
      return (
        <div className="edit-form-grid">
          <ReadOnlyField label="Fecha actual" value={formatDate(current.fechaVencimientoCedula)} />
          <TextField label="Nueva fecha cedula" type="date" value={modal.fechaVencimientoCedula} onChange={(value) => updateModal(setModal, 'fechaVencimientoCedula', value)} required />
        </div>
      );
    case 'Licencia':
      return (
        <div className="edit-form-grid">
          <ReadOnlyField label="Fecha actual" value={formatDate(current.fechaVencimientoLicencia)} />
          <label className="check-label compact-check">
            <input type="checkbox" checked={modal.tieneLicencia} onChange={(event) => updateModal(setModal, 'tieneLicencia', event.target.checked)} />
            Tiene licencia
          </label>
          <TextField label="Numero licencia" value={modal.numeroLicencia} onChange={(value) => updateModal(setModal, 'numeroLicencia', value)} />
          <TextField label="Tipo licencia" value={modal.tipoLicencia} onChange={(value) => updateModal(setModal, 'tipoLicencia', value)} />
          <TextField label="Nueva fecha licencia" type="date" value={modal.fechaVencimientoLicencia} onChange={(value) => updateModal(setModal, 'fechaVencimientoLicencia', value)} />
        </div>
      );
    case 'Contrato':
      return (
        <div className="edit-form-grid">
          <ReadOnlyField label="Fecha actual" value={formatDate(current.fechaVencimientoContrato)} />
          <ReadOnlyField label="Tipo actual" value={current.tipoContrato} />
          <ReadOnlyField label="Estatus actual" value={current.estatus} />
          <label>
            Resultado de la gestion
            <select
              value={modal.resultadoGestionContrato}
              onChange={(event) => updateResultadoContrato(setModal, event.target.value as ResultadoGestionContrato)}
              required
            >
              {resultadoContratoOptions.map((item) => <option key={item.value || 'empty'} value={item.value}>{item.label}</option>)}
            </select>
          </label>
          {modal.resultadoGestionContrato === 'RenovoEventual' && (
            <>
              <ReadOnlyField label="Tipo contrato destino" value={tiposContrato.find((item) => item.requiereFechaVencimiento)?.nombre ?? 'Eventual'} />
              <TextField label="Nueva fecha contrato" type="date" value={modal.fechaVencimientoContrato} onChange={(value) => updateModal(setModal, 'fechaVencimientoContrato', value)} required />
            </>
          )}
          {modal.resultadoGestionContrato === 'PasoPermanente' && (
            <div className="info-strip span-2">No se requiere nueva fecha de vencimiento porque el contrato pasa a termino indefinido.</div>
          )}
          {modal.resultadoGestionContrato === 'PasoCesante' && (
            <>
              <ReadOnlyField label="Estatus destino" value={estatus.find((item) => item.codigo === 'C')?.nombre ?? 'Cesante'} />
              <TextField label="Fecha de salida" type="date" value={modal.fechaSalida} onChange={(value) => updateModal(setModal, 'fechaSalida', value)} required />
              <label>
                Motivo de salida
                <select value={modal.motivoSalidaId} onChange={(event) => updateModal(setModal, 'motivoSalidaId', event.target.value)} required>
                  <option value="">Seleccione motivo</option>
                  {motivosSalida.map((item) => <option key={item.id} value={item.id}>{item.nombre}</option>)}
                </select>
              </label>
            </>
          )}
          {modal.resultadoGestionContrato === 'Excepcion' && (
            <div className="info-strip span-2">La alerta se gestionara sin cambios en contrato, estatus ni fechas. La observacion es obligatoria.</div>
          )}
        </div>
      );
    case 'PeriodoProbatorio':
      return (
        <div className="edit-form-grid">
          <ReadOnlyField label="Fecha actual" value={formatDate(current.fechaVencimientoPeriodoProbatorio)} />
          <TextField label="Nueva fecha periodo" type="date" value={modal.fechaVencimientoPeriodoProbatorio} onChange={(value) => updateModal(setModal, 'fechaVencimientoPeriodoProbatorio', value)} required />
        </div>
      );
    case 'Documento':
      return (
        <div className="edit-form-grid">
          <ReadOnlyField label="Documento" value={modal.documento?.nombreArchivo ?? 'N/D'} />
          <ReadOnlyField label="Fecha actual" value={formatDate(modal.documento?.fechaVencimiento)} />
          <TextField label="Nueva fecha documento" type="date" value={modal.fechaVencimientoDocumento} onChange={(value) => updateModal(setModal, 'fechaVencimientoDocumento', value)} />
          <label className="span-2">
            Observacion documento
            <input value={modal.observacionDocumento} onChange={(event) => updateModal(setModal, 'observacionDocumento', event.target.value)} />
          </label>
        </div>
      );
    default:
      return <div className="empty-state">Tipo de alerta no soportado</div>;
  }
}

function Stat({ label, value }: { label: string; value: number }) {
  return <div className="metric-card static"><small>{label}</small><strong>{value}</strong></div>;
}

function alertTimingText(alerta: Alerta) {
  if (alerta.estadoAlerta === 'Vencida') {
    return `${alerta.diasVencidos} dias vencidos`;
  }

  return alerta.diasRestantes === 0 ? 'Vence hoy' : `${alerta.diasRestantes} dias restantes`;
}

function alertToneClass(alerta: Alerta) {
  return alerta.estadoAlerta === 'Vencida' ? 'danger' : 'warning';
}

function TextField({
  label,
  value,
  onChange,
  type = 'text',
  required = false
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  type?: string;
  required?: boolean;
}) {
  return (
    <label>
      {label}
      <input type={type} value={value} onChange={(event) => onChange(event.target.value)} required={required} />
    </label>
  );
}

function ReadOnlyField({ label, value }: { label: string; value: string }) {
  return (
    <div className="readonly-field">
      <span>{label}</span>
      <strong>{value}</strong>
    </div>
  );
}

function toGestionState(alerta: Alerta, mode: GestionMode, colaborador: ColaboradorDetalle, documento: Documento | null): GestionState {
  return {
    alerta,
    mode,
    colaborador,
    documento,
    observacionGestion: '',
    gestionarSinCambio: false,
    resultadoGestionContrato: '',
    fechaVencimientoCedula: toDateInput(colaborador.fechaVencimientoCedula),
    tieneLicencia: colaborador.tieneLicencia,
    numeroLicencia: colaborador.numeroLicencia ?? '',
    tipoLicencia: colaborador.tipoLicencia ?? '',
    fechaVencimientoLicencia: toDateInput(colaborador.fechaVencimientoLicencia),
    tipoContratoId: String(colaborador.tipoContratoId),
    fechaVencimientoContrato: toDateInput(colaborador.fechaVencimientoContrato),
    estatusId: String(colaborador.estatusId),
    fechaSalida: toDateInput(colaborador.fechaSalida),
    motivoSalidaId: colaborador.motivoSalidaId ? String(colaborador.motivoSalidaId) : '',
    fechaVencimientoPeriodoProbatorio: toDateInput(colaborador.fechaVencimientoPeriodoProbatorio),
    fechaVencimientoDocumento: toDateInput(documento?.fechaVencimiento),
    observacionDocumento: documento?.observacion ?? ''
  };
}

function buildPayload(modal: GestionState): AlertaGestionCorreccion {
  const payload: AlertaGestionCorreccion = {
    observacionGestion: modal.observacionGestion.trim(),
    gestionarSinCambio: modal.gestionarSinCambio
  };

  switch (modal.alerta.tipoAlerta) {
    case 'Cedula':
      payload.fechaVencimientoCedula = optionalDate(modal.fechaVencimientoCedula);
      break;
    case 'Licencia':
      payload.tieneLicencia = modal.tieneLicencia;
      payload.numeroLicencia = optionalText(modal.numeroLicencia);
      payload.tipoLicencia = optionalText(modal.tipoLicencia);
      payload.fechaVencimientoLicencia = optionalDate(modal.fechaVencimientoLicencia);
      break;
    case 'Contrato':
      payload.resultadoGestionContrato = modal.resultadoGestionContrato;
      payload.gestionarSinCambio = modal.resultadoGestionContrato === 'Excepcion';
      if (modal.resultadoGestionContrato === 'RenovoEventual') {
        payload.nuevaFechaVencimientoContrato = optionalDate(modal.fechaVencimientoContrato);
      }
      if (modal.resultadoGestionContrato === 'PasoCesante') {
        payload.fechaSalida = optionalDate(modal.fechaSalida);
        payload.motivoSalidaId = optionalNumber(modal.motivoSalidaId);
      }
      break;
    case 'PeriodoProbatorio':
      payload.fechaVencimientoPeriodoProbatorio = optionalDate(modal.fechaVencimientoPeriodoProbatorio);
      break;
    case 'Documento':
      payload.fechaVencimientoDocumento = optionalDate(modal.fechaVencimientoDocumento);
      payload.observacionDocumento = modal.observacionDocumento;
      break;
  }

  return payload;
}

function validateContratoModal(modal: GestionState) {
  if (modal.mode !== 'gestionar' || modal.alerta.tipoAlerta !== 'Contrato') {
    return '';
  }

  if (!modal.resultadoGestionContrato) {
    return 'Debe seleccionar el resultado de la gestion del contrato.';
  }

  if (modal.resultadoGestionContrato === 'RenovoEventual' && !modal.fechaVencimientoContrato) {
    return 'Debe indicar la nueva fecha de vencimiento del contrato eventual.';
  }

  if (modal.resultadoGestionContrato === 'PasoCesante') {
    if (!modal.fechaSalida) {
      return 'Debe indicar la fecha de salida.';
    }

    if (!modal.motivoSalidaId) {
      return 'Debe indicar el motivo de salida.';
    }
  }

  return '';
}

function updateModal<K extends keyof GestionState>(
  setter: Dispatch<SetStateAction<GestionState | null>>,
  key: K,
  value: GestionState[K]
) {
  setter((current) => current ? { ...current, [key]: value } : current);
}

function updateResultadoContrato(
  setter: Dispatch<SetStateAction<GestionState | null>>,
  resultadoGestionContrato: ResultadoGestionContrato
) {
  setter((current) => {
    if (!current) {
      return current;
    }

    return {
      ...current,
      resultadoGestionContrato,
      gestionarSinCambio: resultadoGestionContrato === 'Excepcion',
      fechaVencimientoContrato: resultadoGestionContrato === 'RenovoEventual' ? '' : current.fechaVencimientoContrato,
      fechaSalida: resultadoGestionContrato === 'PasoCesante' ? '' : current.fechaSalida,
      motivoSalidaId: resultadoGestionContrato === 'PasoCesante' ? '' : current.motivoSalidaId
    };
  });
}

function toDateInput(value?: string | null) {
  return value ? value.slice(0, 10) : '';
}

function optionalDate(value: string) {
  return value ? value : null;
}

function optionalText(value: string) {
  const trimmed = value.trim();
  return trimmed ? trimmed : null;
}

function optionalNumber(value: string) {
  return value ? Number(value) : null;
}
