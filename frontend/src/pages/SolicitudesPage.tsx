import { useCallback, useEffect, useMemo, useState } from 'react';
import type { Dispatch, FormEvent, SetStateAction } from 'react';
import {
  Ban,
  CheckCircle2,
  ClipboardList,
  Eye,
  FileText,
  RotateCcw,
  Save,
  Send,
  X,
  XCircle
} from 'lucide-react';
import { apiGet, apiPost, apiPut } from '../api/client';
import type {
  AprobadorSolicitud,
  CatalogoItem,
  RequisicionPersonalRequest,
  SolicitudDetail,
  SolicitudList,
  TipoSolicitudDisponible
} from '../types/api';
import { formatDate, formatMoney, statusClass } from '../utils/format';

type RequisicionFormState = {
  empresaId: string;
  departamentoSolicitadoId: string;
  fechaEfectiva: string;
  justificacion: string;
  observaciones: string;
  liderAprobadorUsuarioId: string;
  liderAprobadorColaboradorId: string;
  departamentoResponsableId: string;
  cargoSolicitado: string;
  numeroPlazas: string;
  dependenciaJerarquica: string;
  principalesResponsabilidades: string;
  funcionesEspecificas: string;
  equipoACargo: string;
  centroTrabajo: string;
  salario: string;
  gastoRepresentacion: string;
  salarioVariable: string;
  otrosConceptos: string;
  esPosicionNueva: boolean;
  esReemplazo: boolean;
  nombrePersonaReemplazada: string;
  tipoContratoId: string;
  periodoPrueba: string;
  formacionRequerida: string;
  formacionComplementaria: string;
  conocimientosTecnicos: string;
  conocimientosValorados: string;
  idiomaNivel: string;
  aniosExperiencia: string;
  funcionesExperiencia: string;
  areaSectorExperiencia: string;
  experienciaValorable: string;
  edadMinima: string;
  edadMaxima: string;
  sexoPreferido: string;
  caracteristicasPersonales: string;
  fechaAperturaProceso: string;
  fechaEntregaCandidatos: string;
};

export function SolicitudesPage() {
  const [tipos, setTipos] = useState<TipoSolicitudDisponible[]>([]);
  const [solicitudes, setSolicitudes] = useState<SolicitudList[]>([]);
  const [empresas, setEmpresas] = useState<CatalogoItem[]>([]);
  const [departamentos, setDepartamentos] = useState<CatalogoItem[]>([]);
  const [formDepartamentos, setFormDepartamentos] = useState<CatalogoItem[]>([]);
  const [tiposContrato, setTiposContrato] = useState<CatalogoItem[]>([]);
  const [aprobadores, setAprobadores] = useState<AprobadorSolicitud[]>([]);
  const [tipo, setTipo] = useState('');
  const [estado, setEstado] = useState('');
  const [empresaId, setEmpresaId] = useState('');
  const [departamentoId, setDepartamentoId] = useState('');
  const [fechaDesde, setFechaDesde] = useState('');
  const [fechaHasta, setFechaHasta] = useState('');
  const [form, setForm] = useState<RequisicionFormState | null>(null);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [detail, setDetail] = useState<SolicitudDetail | null>(null);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [formError, setFormError] = useState('');
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    Promise.all([
      apiGet<TipoSolicitudDisponible[]>('/solicitudes/tipos'),
      apiGet<CatalogoItem[]>('/catalogos/empresas'),
      apiGet<CatalogoItem[]>('/catalogos/tipos-contrato')
    ])
      .then(([requestTypes, companies, contracts]) => {
        setTipos(requestTypes);
        setEmpresas(companies);
        setTiposContrato(contracts);
      })
      .catch((err) => setError(err instanceof Error ? err.message : 'No se pudieron cargar catalogos.'));
  }, []);

  useEffect(() => {
    const params = new URLSearchParams();
    if (empresaId) params.set('empresaId', empresaId);
    apiGet<CatalogoItem[]>(`/catalogos/departamentos${params.toString() ? `?${params}` : ''}`)
      .then(setDepartamentos)
      .catch((err) => setError(err instanceof Error ? err.message : 'No se pudieron cargar departamentos.'));
  }, [empresaId]);

  useEffect(() => {
    if (!form) {
      setAprobadores([]);
      return;
    }

    const params = new URLSearchParams();
    if (form.empresaId) params.set('empresaId', form.empresaId);
    if (form.departamentoSolicitadoId) params.set('departamentoId', form.departamentoSolicitadoId);
    apiGet<AprobadorSolicitud[]>(`/organigrama/aprobadores${params.toString() ? `?${params}` : ''}`)
      .then(setAprobadores)
      .catch((err) => setFormError(err instanceof Error ? err.message : 'No se pudieron cargar aprobadores.'));
  }, [form?.empresaId, form?.departamentoSolicitadoId]);

  const query = useMemo(() => {
    const params = new URLSearchParams();
    if (tipo) params.set('tipo', tipo);
    if (estado) params.set('estado', estado);
    if (empresaId) params.set('empresaId', empresaId);
    if (departamentoId) params.set('departamentoId', departamentoId);
    if (fechaDesde) params.set('fechaDesde', fechaDesde);
    if (fechaHasta) params.set('fechaHasta', fechaHasta);
    return params.toString();
  }, [departamentoId, empresaId, estado, fechaDesde, fechaHasta, tipo]);

  const loadSolicitudes = useCallback(() => {
    setLoading(true);
    apiGet<SolicitudList[]>(`/solicitudes${query ? `?${query}` : ''}`)
      .then(setSolicitudes)
      .catch((err) => setError(err instanceof Error ? err.message : 'No se pudieron cargar solicitudes.'))
      .finally(() => setLoading(false));
  }, [query]);

  useEffect(loadSolicitudes, [loadSolicitudes]);

  function applyFilters(event: FormEvent) {
    event.preventDefault();
    loadSolicitudes();
  }

  function changeEmpresaFilter(value: string) {
    setEmpresaId(value);
    setDepartamentoId('');
  }

  async function openCreateRequisicion() {
    setForm(emptyFormState());
    setEditingId(null);
    setDetail(null);
    setFormError('');
    setNotice('');
    setError('');
    setFormDepartamentos([]);
  }

  async function openDetail(id: number) {
    setError('');
    setNotice('');
    try {
      setDetail(await apiGet<SolicitudDetail>(`/solicitudes/${id}`));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'No se pudo cargar la solicitud.');
    }
  }

  async function openEditFromDetail(current: SolicitudDetail) {
    if (!current.requisicion) return;
    setForm(toFormState(current));
    setEditingId(current.solicitudId);
    setDetail(null);
    setFormError('');
    if (current.empresaId) {
      try {
        setFormDepartamentos(await apiGet<CatalogoItem[]>(`/catalogos/departamentos?empresaId=${current.empresaId}`));
      } catch (err) {
        setFormError(err instanceof Error ? err.message : 'No se pudieron cargar departamentos.');
      }
    }
  }

  function closeForm() {
    setForm(null);
    setEditingId(null);
    setFormError('');
  }

  function closeDetail() {
    setDetail(null);
  }

  async function changeFormEmpresa(value: string) {
    updateForm(setForm, 'empresaId', value);
    updateForm(setForm, 'departamentoSolicitadoId', '');
    clearApprover();
    if (!value) {
      setFormDepartamentos([]);
      return;
    }

    try {
      setFormDepartamentos(await apiGet<CatalogoItem[]>(`/catalogos/departamentos?empresaId=${value}`));
    } catch (err) {
      setFormError(err instanceof Error ? err.message : 'No se pudieron cargar departamentos.');
    }
  }

  function changeFormDepartamento(value: string) {
    updateForm(setForm, 'departamentoSolicitadoId', value);
    clearApprover();
  }

  function clearApprover() {
    setForm((current) => current ? {
      ...current,
      liderAprobadorUsuarioId: '',
      liderAprobadorColaboradorId: '',
      departamentoResponsableId: ''
    } : current);
  }

  function changeApprover(value: string) {
    const selected = aprobadores.find((item) => String(item.departamentoResponsableId) === value);
    setForm((current) => current ? {
      ...current,
      departamentoResponsableId: value,
      liderAprobadorUsuarioId: selected?.usuarioResponsableId ? String(selected.usuarioResponsableId) : '',
      liderAprobadorColaboradorId: selected ? String(selected.colaboradorResponsableId) : ''
    } : current);
  }

  async function saveRequisicion(enviar: boolean) {
    if (!form) return;

    setSaving(true);
    setFormError('');
    setNotice('');
    try {
      const payload = toPayload(form, enviar);
      if (editingId) {
        await apiPut<{ solicitudId: number; codigoSolicitud: string }>(`/solicitudes/requisicion-personal/${editingId}`, payload);
        if (enviar) {
          await apiPost<SolicitudDetail>(`/solicitudes/${editingId}/enviar`, { comentario: 'Envio desde formulario.' });
        }
      } else {
        await apiPost<{ solicitudId: number; codigoSolicitud: string }>('/solicitudes/requisicion-personal', payload);
      }

      closeForm();
      setNotice(enviar ? 'Solicitud enviada.' : 'Solicitud guardada.');
      loadSolicitudes();
    } catch (err) {
      setFormError(err instanceof Error ? err.message : 'No se pudo guardar la solicitud.');
    } finally {
      setSaving(false);
    }
  }

  async function executeAction(action: string, current: SolicitudDetail) {
    if (action === 'editar') {
      await openEditFromDetail(current);
      return;
    }

    const comentario = getActionComment(action);
    if (comentario === null) return;

    setError('');
    setNotice('');
    try {
      const updated = await apiPost<SolicitudDetail>(`/solicitudes/${current.solicitudId}/${action}`, { comentario });
      setDetail(updated);
      setNotice(actionMessage(action));
      loadSolicitudes();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'No se pudo ejecutar la accion.');
    }
  }

  function renderRequestCard(item: TipoSolicitudDisponible) {
    const isRequisition = item.tipo === 'RequisicionPersonal';
    return (
      <button
        key={item.tipo}
        className={`request-type-card ${item.disponible ? '' : 'disabled'}`}
        type="button"
        disabled={!item.disponible}
        onClick={isRequisition ? openCreateRequisicion : undefined}
      >
        <span className="request-type-icon"><ClipboardList size={22} /></span>
        <strong>{item.nombre}</strong>
        <small>{item.estado}</small>
      </button>
    );
  }

  return (
    <section className="page">
      <div className="page-heading">
        <div>
          <h1>Solicitudes</h1>
          <p>{solicitudes.length} registros</p>
        </div>
      </div>

      {error && <div className="error-box">{error}</div>}
      {notice && <div className="success-box">{notice}</div>}

      <div className="request-type-grid">
        {tipos.map(renderRequestCard)}
      </div>

      <form className="filter-row solicitudes-filter-row" onSubmit={applyFilters}>
        <Select label="Tipo" value={tipo} onChange={setTipo} options={tipos.map((item) => ({ id: 0, nombre: item.nombre, codigo: item.tipo }))} emptyText="Todos" valueFromCode />
        <Select label="Estado" value={estado} onChange={setEstado} options={estadoOptions} emptyText="Todos" valueFromCode />
        <Select label="Empresa" value={empresaId} onChange={changeEmpresaFilter} options={empresas} emptyText="Todas" />
        <Select label="Departamento" value={departamentoId} onChange={setDepartamentoId} options={departamentos} emptyText="Todos" />
        <TextField label="Desde" type="date" value={fechaDesde} onChange={setFechaDesde} />
        <TextField label="Hasta" type="date" value={fechaHasta} onChange={setFechaHasta} />
        <button className="secondary-button" type="submit">Filtrar</button>
      </form>

      {loading && <div className="info-strip">Cargando solicitudes...</div>}

      <div className="table-wrap">
        <table>
          <thead>
            <tr>
              <th>Codigo</th>
              <th>Tipo</th>
              <th>Estado</th>
              <th>Solicitante</th>
              <th>Empresa</th>
              <th>Departamento</th>
              <th>Fecha</th>
              <th>Actualizacion</th>
              <th>Acciones</th>
            </tr>
          </thead>
          <tbody>
            {solicitudes.length === 0 && (
              <tr><td colSpan={9}><div className="empty-state">Sin solicitudes</div></td></tr>
            )}
            {solicitudes.map((item) => (
              <tr key={item.solicitudId}>
                <td>{item.codigoSolicitud}</td>
                <td>{formatSolicitudType(item.tipoSolicitud)}</td>
                <td><span className={`badge ${statusClass(item.estado)}`}>{formatEstado(item.estado)}</span></td>
                <td>{item.solicitante}</td>
                <td>{item.empresa ?? 'N/D'}</td>
                <td>{item.departamento ?? 'N/D'}</td>
                <td>{formatDate(item.fechaSolicitud)}</td>
                <td>{formatDate(item.ultimaActualizacion)}</td>
                <td>
                  <button className="icon-text-button" onClick={() => openDetail(item.solicitudId)} type="button">
                    <Eye size={16} />
                    Ver
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {form && (
        <div className="modal-backdrop" role="presentation">
          <section className="modal-panel solicitud-modal" role="dialog" aria-modal="true" aria-labelledby="solicitud-form-title">
            <div className="modal-header">
              <div>
                <h2 id="solicitud-form-title">{editingId ? 'Editar requisicion' : 'Requisicion de Personal'}</h2>
                <p>{editingId ? `Solicitud ${editingId}` : 'Nueva solicitud'}</p>
              </div>
              <button className="icon-button light" onClick={closeForm} type="button" title="Cerrar" aria-label="Cerrar">
                <X size={18} />
              </button>
            </div>
            {formError && <div className="error-box">{formError}</div>}
            <form className="edit-modal-form">
              <div className="form-section">
                <h3>Datos generales</h3>
                <div className="edit-form-grid">
                  <Select label="Empresa" value={form.empresaId} onChange={changeFormEmpresa} options={empresas} emptyText="Seleccione" required />
                  <Select label="Departamento" value={form.departamentoSolicitadoId} onChange={changeFormDepartamento} options={formDepartamentos} emptyText="Sin definir" />
                  <TextField label="Fecha efectiva" type="date" value={form.fechaEfectiva} onChange={(value) => updateForm(setForm, 'fechaEfectiva', value)} />
                  <AprobadorSelect label="Lider aprobador" value={form.departamentoResponsableId} onChange={changeApprover} options={aprobadores} />
                  <TextField label="Cargo solicitado" value={form.cargoSolicitado} onChange={(value) => updateForm(setForm, 'cargoSolicitado', value)} required />
                  <TextField label="Plazas" type="number" value={form.numeroPlazas} onChange={(value) => updateForm(setForm, 'numeroPlazas', value)} required />
                  <Select label="Tipo contrato" value={form.tipoContratoId} onChange={(value) => updateForm(setForm, 'tipoContratoId', value)} options={tiposContrato} emptyText="Sin definir" />
                  <TextField label="Periodo prueba" value={form.periodoPrueba} onChange={(value) => updateForm(setForm, 'periodoPrueba', value)} />
                  <TextareaField label="Justificacion" value={form.justificacion} onChange={(value) => updateForm(setForm, 'justificacion', value)} span />
                  <TextareaField label="Observaciones" value={form.observaciones} onChange={(value) => updateForm(setForm, 'observaciones', value)} span />
                  {form.empresaId && form.departamentoSolicitadoId && aprobadores.length === 0 && (
                    <div className="form-note warning span-2">No hay aprobadores configurados para la empresa y departamento seleccionados.</div>
                  )}
                </div>
              </div>

              <div className="form-section">
                <h3>Puesto</h3>
                <div className="edit-form-grid">
                  <TextField label="Dependencia jerarquica" value={form.dependenciaJerarquica} onChange={(value) => updateForm(setForm, 'dependenciaJerarquica', value)} />
                  <TextField label="Centro trabajo" value={form.centroTrabajo} onChange={(value) => updateForm(setForm, 'centroTrabajo', value)} />
                  <label className="check-label compact-check">
                    <input
                      type="checkbox"
                      checked={form.esPosicionNueva}
                      onChange={(event) => setForm((current) => current ? { ...current, esPosicionNueva: event.target.checked, esReemplazo: event.target.checked ? false : current.esReemplazo } : current)}
                    />
                    Posicion nueva
                  </label>
                  <label className="check-label compact-check">
                    <input
                      type="checkbox"
                      checked={form.esReemplazo}
                      onChange={(event) => setForm((current) => current ? { ...current, esReemplazo: event.target.checked, esPosicionNueva: event.target.checked ? false : current.esPosicionNueva } : current)}
                    />
                    Reemplazo
                  </label>
                  {form.esReemplazo && (
                    <TextField label="Persona reemplazada" value={form.nombrePersonaReemplazada} onChange={(value) => updateForm(setForm, 'nombrePersonaReemplazada', value)} />
                  )}
                  <TextareaField label="Responsabilidades" value={form.principalesResponsabilidades} onChange={(value) => updateForm(setForm, 'principalesResponsabilidades', value)} span />
                  <TextareaField label="Funciones especificas" value={form.funcionesEspecificas} onChange={(value) => updateForm(setForm, 'funcionesEspecificas', value)} span />
                  <TextareaField label="Equipo a cargo" value={form.equipoACargo} onChange={(value) => updateForm(setForm, 'equipoACargo', value)} span />
                </div>
              </div>

              <div className="form-section">
                <h3>Compensacion</h3>
                <div className="edit-form-grid">
                  <TextField label="Salario" type="number" value={form.salario} onChange={(value) => updateForm(setForm, 'salario', value)} />
                  <TextField label="Gasto representacion" type="number" value={form.gastoRepresentacion} onChange={(value) => updateForm(setForm, 'gastoRepresentacion', value)} />
                  <TextField label="Salario variable" type="number" value={form.salarioVariable} onChange={(value) => updateForm(setForm, 'salarioVariable', value)} />
                  <TextField label="Otros conceptos" value={form.otrosConceptos} onChange={(value) => updateForm(setForm, 'otrosConceptos', value)} />
                </div>
              </div>

              <div className="form-section">
                <h3>Perfil requerido</h3>
                <div className="edit-form-grid">
                  <TextField label="Anios experiencia" type="number" value={form.aniosExperiencia} onChange={(value) => updateForm(setForm, 'aniosExperiencia', value)} />
                  <TextField label="Edad minima" type="number" value={form.edadMinima} onChange={(value) => updateForm(setForm, 'edadMinima', value)} />
                  <TextField label="Edad maxima" type="number" value={form.edadMaxima} onChange={(value) => updateForm(setForm, 'edadMaxima', value)} />
                  <TextField label="Sexo preferido" value={form.sexoPreferido} onChange={(value) => updateForm(setForm, 'sexoPreferido', value)} />
                  <TextareaField label="Formacion requerida" value={form.formacionRequerida} onChange={(value) => updateForm(setForm, 'formacionRequerida', value)} span />
                  <TextareaField label="Formacion complementaria" value={form.formacionComplementaria} onChange={(value) => updateForm(setForm, 'formacionComplementaria', value)} span />
                  <TextareaField label="Conocimientos tecnicos" value={form.conocimientosTecnicos} onChange={(value) => updateForm(setForm, 'conocimientosTecnicos', value)} span />
                  <TextareaField label="Conocimientos valorados" value={form.conocimientosValorados} onChange={(value) => updateForm(setForm, 'conocimientosValorados', value)} span />
                  <TextField label="Idioma / nivel" value={form.idiomaNivel} onChange={(value) => updateForm(setForm, 'idiomaNivel', value)} />
                  <TextareaField label="Funciones experiencia" value={form.funcionesExperiencia} onChange={(value) => updateForm(setForm, 'funcionesExperiencia', value)} span />
                  <TextareaField label="Area / sector experiencia" value={form.areaSectorExperiencia} onChange={(value) => updateForm(setForm, 'areaSectorExperiencia', value)} span />
                  <TextareaField label="Experiencia valorable" value={form.experienciaValorable} onChange={(value) => updateForm(setForm, 'experienciaValorable', value)} span />
                  <TextareaField label="Caracteristicas personales" value={form.caracteristicasPersonales} onChange={(value) => updateForm(setForm, 'caracteristicasPersonales', value)} span />
                </div>
              </div>

              <div className="form-section">
                <h3>Proceso</h3>
                <div className="edit-form-grid">
                  <TextField label="Fecha apertura" type="date" value={form.fechaAperturaProceso} onChange={(value) => updateForm(setForm, 'fechaAperturaProceso', value)} />
                  <TextField label="Entrega candidatos" type="date" value={form.fechaEntregaCandidatos} onChange={(value) => updateForm(setForm, 'fechaEntregaCandidatos', value)} />
                </div>
              </div>

              <div className="modal-actions">
                <button className="secondary-button" type="button" onClick={closeForm}>Cancelar</button>
                <button className="secondary-button" disabled={saving} type="button" onClick={() => saveRequisicion(false)}>
                  <Save size={18} />
                  {saving ? 'Guardando...' : 'Guardar'}
                </button>
                <button className="primary-button" disabled={saving} type="button" onClick={() => saveRequisicion(true)}>
                  <Send size={18} />
                  {saving ? 'Enviando...' : 'Enviar'}
                </button>
              </div>
            </form>
          </section>
        </div>
      )}

      {detail && (
        <div className="modal-backdrop" role="presentation">
          <section className="modal-panel solicitud-modal" role="dialog" aria-modal="true" aria-labelledby="solicitud-detail-title">
            <div className="modal-header">
              <div>
                <h2 id="solicitud-detail-title">{detail.codigoSolicitud}</h2>
                <p>{formatSolicitudType(detail.tipoSolicitud)} · {formatEstado(detail.estado)}</p>
              </div>
              <button className="icon-button light" onClick={closeDetail} type="button" title="Cerrar" aria-label="Cerrar">
                <X size={18} />
              </button>
            </div>
            <div className="solicitud-detail-body">
              <div className="detail-action-row">
                {detail.accionesDisponibles.map((action) => (
                  <button
                    key={action}
                    className={action === 'confirmar-rrhh' || action === 'aprobar' ? 'primary-button' : 'secondary-button'}
                    onClick={() => executeAction(action, detail)}
                    type="button"
                  >
                    {actionIcon(action)}
                    {actionLabel(action)}
                  </button>
                ))}
              </div>

              <section className="detail-section">
                <h3>Datos generales</h3>
                <div className="detail-grid">
                  <DetailField label="Estado" value={formatEstado(detail.estado)} />
                  <DetailField label="Solicitante" value={detail.solicitante} />
                  <DetailField label="Empresa" value={detail.empresa ?? 'N/D'} />
                  <DetailField label="Departamento" value={detail.departamento ?? 'N/D'} />
                  <DetailField label="Fecha solicitud" value={formatDate(detail.fechaSolicitud)} />
                  <DetailField label="Fecha efectiva" value={formatDate(detail.fechaEfectiva)} />
                  <DetailField label="Justificacion" value={detail.justificacion ?? 'N/D'} span />
                  <DetailField label="Observaciones" value={detail.observaciones ?? 'N/D'} span />
                </div>
              </section>

              {detail.requisicion && (
                <>
                  <section className="detail-section">
                    <h3>Requisicion</h3>
                    <div className="detail-grid">
                      <DetailField label="Cargo" value={detail.requisicion.cargoSolicitado} />
                      <DetailField label="Plazas" value={String(detail.requisicion.numeroPlazas)} />
                      <DetailField label="Tipo contrato" value={detail.requisicion.tipoContrato ?? 'N/D'} />
                      <DetailField label="Centro trabajo" value={detail.requisicion.centroTrabajo ?? 'N/D'} />
                      <DetailField label="Posicion nueva" value={detail.requisicion.esPosicionNueva ? 'Si' : 'No'} />
                      <DetailField label="Reemplazo" value={detail.requisicion.esReemplazo ? 'Si' : 'No'} />
                      <DetailField label="Persona reemplazada" value={detail.requisicion.colaboradorReemplazado ?? detail.requisicion.nombrePersonaReemplazada ?? 'N/D'} />
                      <DetailField label="Salario" value={formatMoney(detail.requisicion.salario)} />
                      <DetailField label="Responsabilidades" value={detail.requisicion.principalesResponsabilidades ?? 'N/D'} span />
                      <DetailField label="Funciones" value={detail.requisicion.funcionesEspecificas ?? 'N/D'} span />
                    </div>
                  </section>

                  <section className="detail-section">
                    <h3>Perfil y proceso</h3>
                    <div className="detail-grid">
                      <DetailField label="Formacion" value={detail.requisicion.formacionRequerida ?? 'N/D'} span />
                      <DetailField label="Conocimientos" value={detail.requisicion.conocimientosTecnicos ?? 'N/D'} span />
                      <DetailField label="Experiencia" value={detail.requisicion.aniosExperiencia ? `${detail.requisicion.aniosExperiencia} anios` : 'N/D'} />
                      <DetailField label="Edad" value={formatAge(detail.requisicion.edadMinima, detail.requisicion.edadMaxima)} />
                      <DetailField label="Fecha apertura" value={formatDate(detail.requisicion.fechaAperturaProceso)} />
                      <DetailField label="Entrega candidatos" value={formatDate(detail.requisicion.fechaEntregaCandidatos)} />
                    </div>
                  </section>
                </>
              )}

              <section className="detail-section">
                <h3>Aprobaciones</h3>
                <div className="timeline-list">
                  {detail.aprobaciones.map((item) => (
                    <div key={item.solicitudAprobacionId} className="timeline-item">
                      <span className={`badge ${statusClass(item.estado)}`}>{item.estado}</span>
                      <strong>{item.etapa}</strong>
                      <small>{item.colaboradorAprobador ?? item.usuarioAprobador ?? item.rolAprobador}</small>
                      <em>{formatDate(item.fechaDecision)}</em>
                      {item.comentario && <p>{item.comentario}</p>}
                    </div>
                  ))}
                </div>
              </section>

              <section className="detail-section">
                <h3>Historial</h3>
                <div className="timeline-list">
                  {detail.historial.map((item) => (
                    <div key={item.solicitudHistorialId} className="timeline-item">
                      <span>{item.accion}</span>
                      <strong>{item.usuario}</strong>
                      <small>{formatDate(item.fecha)}</small>
                      <em>{item.estadoAnterior ? `${item.estadoAnterior} -> ${item.estadoNuevo}` : item.estadoNuevo ?? ''}</em>
                      {item.comentario && <p>{item.comentario}</p>}
                    </div>
                  ))}
                </div>
              </section>
            </div>
          </section>
        </div>
      )}
    </section>
  );
}

const estadoOptions: CatalogoItem[] = [
  { id: 0, nombre: 'Borrador', codigo: 'Borrador' },
  { id: 0, nombre: 'Enviada', codigo: 'Enviada' },
  { id: 0, nombre: 'Pendiente lider', codigo: 'PendienteAprobacionLider' },
  { id: 0, nombre: 'Pendiente RRHH', codigo: 'PendienteRevisionRRHH' },
  { id: 0, nombre: 'Devuelta', codigo: 'Devuelta' },
  { id: 0, nombre: 'Rechazada', codigo: 'Rechazada' },
  { id: 0, nombre: 'Aprobada', codigo: 'Aprobada' },
  { id: 0, nombre: 'Cancelada', codigo: 'Cancelada' },
  { id: 0, nombre: 'Cerrada', codigo: 'Cerrada' }
];

function Select({
  label,
  value,
  onChange,
  options,
  emptyText,
  required,
  valueFromCode
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  options: CatalogoItem[];
  emptyText: string;
  required?: boolean;
  valueFromCode?: boolean;
}) {
  return (
    <label>
      {label}
      <select value={value} onChange={(event) => onChange(event.target.value)} required={required}>
        <option value="">{emptyText}</option>
        {options.map((item, index) => {
          const optionValue = valueFromCode ? item.codigo ?? item.nombre : String(item.id);
          return <option key={`${optionValue}-${index}`} value={optionValue}>{item.nombre}</option>;
        })}
      </select>
    </label>
  );
}

function AprobadorSelect({
  label,
  value,
  onChange,
  options
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  options: AprobadorSolicitud[];
}) {
  return (
    <label>
      {label}
      <select value={value} onChange={(event) => onChange(event.target.value)}>
        <option value="">Seleccione aprobador</option>
        {options.map((item) => (
          <option key={item.departamentoResponsableId} value={item.departamentoResponsableId}>
            {item.nombreCompleto} - {item.cargo} - {item.departamento}
          </option>
        ))}
      </select>
    </label>
  );
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

function TextareaField({
  label,
  value,
  onChange,
  span = false
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  span?: boolean;
}) {
  return (
    <label className={span ? 'span-2' : undefined}>
      {label}
      <textarea value={value} onChange={(event) => onChange(event.target.value)} />
    </label>
  );
}

function DetailField({ label, value, span = false }: { label: string; value: string; span?: boolean }) {
  return (
    <div className={`readonly-field ${span ? 'span-2' : ''}`}>
      <span>{label}</span>
      <strong>{value}</strong>
    </div>
  );
}

function updateForm<K extends keyof RequisicionFormState>(
  setter: Dispatch<SetStateAction<RequisicionFormState | null>>,
  key: K,
  value: RequisicionFormState[K]
) {
  setter((current) => current ? { ...current, [key]: value } : current);
}

function emptyFormState(): RequisicionFormState {
  return {
    empresaId: '',
    departamentoSolicitadoId: '',
    fechaEfectiva: '',
    justificacion: '',
    observaciones: '',
    liderAprobadorUsuarioId: '',
    liderAprobadorColaboradorId: '',
    departamentoResponsableId: '',
    cargoSolicitado: '',
    numeroPlazas: '1',
    dependenciaJerarquica: '',
    principalesResponsabilidades: '',
    funcionesEspecificas: '',
    equipoACargo: '',
    centroTrabajo: '',
    salario: '',
    gastoRepresentacion: '',
    salarioVariable: '',
    otrosConceptos: '',
    esPosicionNueva: true,
    esReemplazo: false,
    nombrePersonaReemplazada: '',
    tipoContratoId: '',
    periodoPrueba: '',
    formacionRequerida: '',
    formacionComplementaria: '',
    conocimientosTecnicos: '',
    conocimientosValorados: '',
    idiomaNivel: '',
    aniosExperiencia: '',
    funcionesExperiencia: '',
    areaSectorExperiencia: '',
    experienciaValorable: '',
    edadMinima: '',
    edadMaxima: '',
    sexoPreferido: '',
    caracteristicasPersonales: '',
    fechaAperturaProceso: '',
    fechaEntregaCandidatos: ''
  };
}

function toFormState(detail: SolicitudDetail): RequisicionFormState {
  const requisicion = detail.requisicion;
  const leader = detail.aprobaciones.find((item) => item.etapa === 'Lider');
  return {
    ...emptyFormState(),
    empresaId: detail.empresaId ? String(detail.empresaId) : '',
    departamentoSolicitadoId: requisicion?.departamentoSolicitadoId ? String(requisicion.departamentoSolicitadoId) : '',
    fechaEfectiva: toDateInput(detail.fechaEfectiva),
    justificacion: detail.justificacion ?? '',
    observaciones: detail.observaciones ?? '',
    liderAprobadorUsuarioId: leader?.usuarioAprobadorId ? String(leader.usuarioAprobadorId) : '',
    liderAprobadorColaboradorId: leader?.colaboradorAprobadorId ? String(leader.colaboradorAprobadorId) : '',
    departamentoResponsableId: leader?.departamentoResponsableId ? String(leader.departamentoResponsableId) : '',
    cargoSolicitado: requisicion?.cargoSolicitado ?? '',
    numeroPlazas: String(requisicion?.numeroPlazas ?? 1),
    dependenciaJerarquica: requisicion?.dependenciaJerarquica ?? '',
    principalesResponsabilidades: requisicion?.principalesResponsabilidades ?? '',
    funcionesEspecificas: requisicion?.funcionesEspecificas ?? '',
    equipoACargo: requisicion?.equipoACargo ?? '',
    centroTrabajo: requisicion?.centroTrabajo ?? '',
    salario: requisicion?.salario ? String(requisicion.salario) : '',
    gastoRepresentacion: requisicion?.gastoRepresentacion ? String(requisicion.gastoRepresentacion) : '',
    salarioVariable: requisicion?.salarioVariable ? String(requisicion.salarioVariable) : '',
    otrosConceptos: requisicion?.otrosConceptos ?? '',
    esPosicionNueva: requisicion?.esPosicionNueva ?? true,
    esReemplazo: requisicion?.esReemplazo ?? false,
    nombrePersonaReemplazada: requisicion?.nombrePersonaReemplazada ?? '',
    tipoContratoId: requisicion?.tipoContratoId ? String(requisicion.tipoContratoId) : '',
    periodoPrueba: requisicion?.periodoPrueba ?? '',
    formacionRequerida: requisicion?.formacionRequerida ?? '',
    formacionComplementaria: requisicion?.formacionComplementaria ?? '',
    conocimientosTecnicos: requisicion?.conocimientosTecnicos ?? '',
    conocimientosValorados: requisicion?.conocimientosValorados ?? '',
    idiomaNivel: requisicion?.idiomaNivel ?? '',
    aniosExperiencia: requisicion?.aniosExperiencia ? String(requisicion.aniosExperiencia) : '',
    funcionesExperiencia: requisicion?.funcionesExperiencia ?? '',
    areaSectorExperiencia: requisicion?.areaSectorExperiencia ?? '',
    experienciaValorable: requisicion?.experienciaValorable ?? '',
    edadMinima: requisicion?.edadMinima ? String(requisicion.edadMinima) : '',
    edadMaxima: requisicion?.edadMaxima ? String(requisicion.edadMaxima) : '',
    sexoPreferido: requisicion?.sexoPreferido ?? '',
    caracteristicasPersonales: requisicion?.caracteristicasPersonales ?? '',
    fechaAperturaProceso: toDateInput(requisicion?.fechaAperturaProceso),
    fechaEntregaCandidatos: toDateInput(requisicion?.fechaEntregaCandidatos)
  };
}

function toPayload(form: RequisicionFormState, enviar: boolean): RequisicionPersonalRequest {
  return {
    empresaId: requiredNumber(form.empresaId, 'Empresa'),
    departamentoSolicitadoId: optionalNumber(form.departamentoSolicitadoId),
    fechaEfectiva: optionalDate(form.fechaEfectiva),
    justificacion: optionalText(form.justificacion),
    observaciones: optionalText(form.observaciones),
    liderAprobadorUsuarioId: optionalNumber(form.liderAprobadorUsuarioId),
    liderAprobadorColaboradorId: optionalNumber(form.liderAprobadorColaboradorId),
    departamentoResponsableId: optionalNumber(form.departamentoResponsableId),
    cargoSolicitado: form.cargoSolicitado.trim(),
    numeroPlazas: requiredNumber(form.numeroPlazas, 'Plazas'),
    dependenciaJerarquica: optionalText(form.dependenciaJerarquica),
    principalesResponsabilidades: optionalText(form.principalesResponsabilidades),
    funcionesEspecificas: optionalText(form.funcionesEspecificas),
    equipoACargo: optionalText(form.equipoACargo),
    centroTrabajo: optionalText(form.centroTrabajo),
    salario: optionalNumber(form.salario),
    gastoRepresentacion: optionalNumber(form.gastoRepresentacion),
    salarioVariable: optionalNumber(form.salarioVariable),
    otrosConceptos: optionalText(form.otrosConceptos),
    esPosicionNueva: form.esPosicionNueva,
    esReemplazo: form.esReemplazo,
    nombrePersonaReemplazada: optionalText(form.nombrePersonaReemplazada),
    tipoContratoId: optionalNumber(form.tipoContratoId),
    periodoPrueba: optionalText(form.periodoPrueba),
    formacionRequerida: optionalText(form.formacionRequerida),
    formacionComplementaria: optionalText(form.formacionComplementaria),
    conocimientosTecnicos: optionalText(form.conocimientosTecnicos),
    conocimientosValorados: optionalText(form.conocimientosValorados),
    idiomaNivel: optionalText(form.idiomaNivel),
    aniosExperiencia: optionalNumber(form.aniosExperiencia),
    funcionesExperiencia: optionalText(form.funcionesExperiencia),
    areaSectorExperiencia: optionalText(form.areaSectorExperiencia),
    experienciaValorable: optionalText(form.experienciaValorable),
    edadMinima: optionalNumber(form.edadMinima),
    edadMaxima: optionalNumber(form.edadMaxima),
    sexoPreferido: optionalText(form.sexoPreferido),
    caracteristicasPersonales: optionalText(form.caracteristicasPersonales),
    fechaAperturaProceso: optionalDate(form.fechaAperturaProceso),
    fechaEntregaCandidatos: optionalDate(form.fechaEntregaCandidatos),
    enviar
  };
}

function actionIcon(action: string) {
  if (action === 'aprobar' || action === 'confirmar-rrhh') return <CheckCircle2 size={17} />;
  if (action === 'rechazar') return <XCircle size={17} />;
  if (action === 'devolver') return <RotateCcw size={17} />;
  if (action === 'cancelar') return <Ban size={17} />;
  if (action === 'enviar') return <Send size={17} />;
  return <FileText size={17} />;
}

function actionLabel(action: string) {
  const labels: Record<string, string> = {
    editar: 'Editar',
    enviar: 'Enviar',
    aprobar: 'Aprobar',
    rechazar: 'Rechazar',
    devolver: 'Devolver',
    cancelar: 'Cancelar',
    'confirmar-rrhh': 'Confirmar RRHH',
    cerrar: 'Cerrar'
  };
  return labels[action] ?? action;
}

function actionMessage(action: string) {
  const labels: Record<string, string> = {
    enviar: 'Solicitud enviada.',
    aprobar: 'Solicitud aprobada.',
    rechazar: 'Solicitud rechazada.',
    devolver: 'Solicitud devuelta.',
    cancelar: 'Solicitud cancelada.',
    'confirmar-rrhh': 'Solicitud confirmada por RRHH.',
    cerrar: 'Solicitud cerrada.'
  };
  return labels[action] ?? 'Accion completada.';
}

function getActionComment(action: string) {
  const needsComment = ['rechazar', 'devolver', 'cancelar'].includes(action);
  const value = window.prompt(needsComment ? 'Comentario' : 'Comentario opcional', '');
  if (value === null) return null;
  return value.trim();
}

function formatSolicitudType(value: string) {
  if (value === 'RequisicionPersonal') return 'Requisicion de Personal';
  if (value === 'AccionPersonal') return 'Accion de Personal';
  if (value === 'Vacaciones') return 'Vacaciones';
  return value;
}

function formatEstado(value: string) {
  const labels: Record<string, string> = {
    Borrador: 'Borrador',
    Enviada: 'Enviada',
    PendienteAprobacionLider: 'Pendiente lider',
    PendienteRevisionRRHH: 'Pendiente RRHH',
    Devuelta: 'Devuelta',
    Rechazada: 'Rechazada',
    Aprobada: 'Aprobada',
    Cancelada: 'Cancelada',
    Cerrada: 'Cerrada'
  };
  return labels[value] ?? value;
}

function formatAge(min?: number | null, max?: number | null) {
  if (min && max) return `${min} - ${max}`;
  if (min) return `Desde ${min}`;
  if (max) return `Hasta ${max}`;
  return 'N/D';
}

function optionalText(value: string) {
  const trimmed = value.trim();
  return trimmed ? trimmed : null;
}

function optionalDate(value: string) {
  return value ? value : null;
}

function optionalNumber(value: string) {
  return value ? Number(value) : null;
}

function requiredNumber(value: string, label: string) {
  const parsed = Number(value);
  if (!parsed) {
    throw new Error(`${label} es obligatorio.`);
  }

  return parsed;
}

function toDateInput(value?: string | null) {
  return value ? value.slice(0, 10) : '';
}
