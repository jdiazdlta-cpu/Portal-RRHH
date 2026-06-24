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
import { useAuth } from '../auth/AuthContext';
import type {
  AccionPersonal,
  AccionPersonalRequest,
  AprobadorSolicitud,
  CatalogoItem,
  ColaboradorResumenLaboral,
  ColaboradorSelect,
  RequisicionPersonalRequest,
  SolicitudDetail,
  SolicitudList,
  TipoAccionPersonalDisponible,
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

type AccionPersonalFormState = {
  tipoAccion: string;
  colaboradorId: string;
  empresaId: string;
  departamentoId: string;
  cargoId: string;
  fechaEfectiva: string;
  justificacion: string;
  observaciones: string;
  liderAprobadorUsuarioId: string;
  liderAprobadorColaboradorId: string;
  departamentoResponsableId: string;
  diasVacaciones: string;
  fechaInicioVacaciones: string;
  fechaFinVacaciones: string;
  periodoVacacionesDesde: string;
  periodoVacacionesHasta: string;
  quienReemplaza: string;
  tipoContratoNuevoId: string;
  fechaInicioContrato: string;
  fechaFinContrato: string;
  esReemplazo: string;
  esPosicionNueva: string;
  salarioNuevo: string;
  viaticosNuevo: string;
  gastosRepresentacionNuevo: string;
  otrosBeneficios: string;
  salarioNuevoAjuste: string;
  ajustePorMes: string;
  motivoAjuste: string;
  cargoNuevoId: string;
  departamentoNuevoId: string;
  empresaNuevaId: string;
  jefeNuevoId: string;
  cargoTrasladoNuevoId: string;
  departamentoTrasladoNuevoId: string;
  empresaTrasladoNuevaId: string;
  jefeTrasladoNuevoId: string;
  tipoLicenciaAccion: string;
  licenciaRemunerada: string;
  fechaInicioLicencia: string;
  fechaFinLicencia: string;
  especificacionLicencia: string;
  tipoFinalizacion: string;
  fechaSalida: string;
  motivoSalidaId: string;
  menosDeDosAnios: boolean;
  terminacionPeriodoPrueba: boolean;
  causaJustificada: boolean;
  mutuoAcuerdo: boolean;
  renovacionExtensionContrato: boolean;
  continuidadLaboral: boolean;
  loRecomienda: string;
  puntualidad: string;
  honestidad: string;
  trabajoEquipo: string;
  productividad: string;
  iniciativa: string;
  respetoJefe: string;
  respetoCompaneros: string;
};

export function SolicitudesPage() {
  const { user } = useAuth();
  const [tipos, setTipos] = useState<TipoSolicitudDisponible[]>([]);
  const [tiposAccion, setTiposAccion] = useState<TipoAccionPersonalDisponible[]>([]);
  const [solicitudes, setSolicitudes] = useState<SolicitudList[]>([]);
  const [empresas, setEmpresas] = useState<CatalogoItem[]>([]);
  const [departamentos, setDepartamentos] = useState<CatalogoItem[]>([]);
  const [formDepartamentos, setFormDepartamentos] = useState<CatalogoItem[]>([]);
  const [tiposContrato, setTiposContrato] = useState<CatalogoItem[]>([]);
  const [motivosSalida, setMotivosSalida] = useState<CatalogoItem[]>([]);
  const [accionDepartamentos, setAccionDepartamentos] = useState<CatalogoItem[]>([]);
  const [accionCargos, setAccionCargos] = useState<CatalogoItem[]>([]);
  const [accionColaboradores, setAccionColaboradores] = useState<ColaboradorSelect[]>([]);
  const [accionColaboradorSearch, setAccionColaboradorSearch] = useState('');
  const [colaboradorActual, setColaboradorActual] = useState<ColaboradorResumenLaboral | null>(null);
  const [destinoDepartamentos, setDestinoDepartamentos] = useState<CatalogoItem[]>([]);
  const [destinoCargos, setDestinoCargos] = useState<CatalogoItem[]>([]);
  const [destinoJefes, setDestinoJefes] = useState<ColaboradorSelect[]>([]);
  const [aprobadores, setAprobadores] = useState<AprobadorSolicitud[]>([]);
  const [tipo, setTipo] = useState('');
  const [estado, setEstado] = useState('');
  const [empresaId, setEmpresaId] = useState('');
  const [departamentoId, setDepartamentoId] = useState('');
  const [fechaDesde, setFechaDesde] = useState('');
  const [fechaHasta, setFechaHasta] = useState('');
  const [form, setForm] = useState<RequisicionFormState | null>(null);
  const [accionForm, setAccionForm] = useState<AccionPersonalFormState | null>(null);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [detail, setDetail] = useState<SolicitudDetail | null>(null);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [formError, setFormError] = useState('');
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const canViewCompensation = user?.rol === 'Admin' || user?.rol === 'RRHH';

  useEffect(() => {
    Promise.all([
      apiGet<TipoSolicitudDisponible[]>('/solicitudes/tipos'),
      apiGet<TipoAccionPersonalDisponible[]>('/solicitudes/accion-personal/tipos'),
      apiGet<CatalogoItem[]>('/catalogos/empresas'),
      apiGet<CatalogoItem[]>('/catalogos/tipos-contrato'),
      apiGet<CatalogoItem[]>('/catalogos/motivos-salida')
    ])
      .then(([requestTypes, actionTypes, companies, contracts, exitReasons]) => {
        setTipos(requestTypes);
        setTiposAccion(actionTypes);
        setEmpresas(companies);
        setTiposContrato(contracts);
        setMotivosSalida(exitReasons);
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
    const currentEmpresaId = accionForm?.empresaId ?? '';
    const currentDepartamentoId = accionForm?.departamentoId ?? '';
    const targetEmpresaId = accionForm && usesDestinationFields(accionForm.tipoAccion) ? destinationEmpresaId(accionForm) : '';
    const targetDepartamentoId = accionForm && usesDestinationFields(accionForm.tipoAccion) ? destinationDepartamentoId(accionForm) : '';
    const activeEmpresaId = form?.empresaId ?? currentEmpresaId;
    const activeDepartamentoId = form?.departamentoSolicitadoId ?? currentDepartamentoId;
    if (!form && !accionForm) {
      setAprobadores([]);
      return;
    }

    if (!activeEmpresaId || !activeDepartamentoId) {
      setAprobadores([]);
      return;
    }

    const contexts = [{ empresaId: activeEmpresaId, departamentoId: activeDepartamentoId }];
    if (accionForm && usesDestinationFields(accionForm.tipoAccion) && targetEmpresaId && targetDepartamentoId) {
      contexts.push({ empresaId: targetEmpresaId, departamentoId: targetDepartamentoId });
    }

    Promise.all(contexts.map((context) => {
      const params = new URLSearchParams({ empresaId: context.empresaId, departamentoId: context.departamentoId });
      return apiGet<AprobadorSolicitud[]>(`/organigrama/aprobadores?${params}`);
    }))
      .then((groups) => {
        const unique = new Map<number, AprobadorSolicitud>();
        groups.flat().forEach((item) => unique.set(item.departamentoResponsableId, item));
        setAprobadores(Array.from(unique.values()));
      })
      .catch((err) => setFormError(err instanceof Error ? err.message : 'No se pudieron cargar aprobadores.'));
  }, [
    accionForm?.departamentoId,
    accionForm?.departamentoNuevoId,
    accionForm?.departamentoTrasladoNuevoId,
    accionForm?.empresaId,
    accionForm?.empresaNuevaId,
    accionForm?.empresaTrasladoNuevaId,
    accionForm?.tipoAccion,
    form?.departamentoSolicitadoId,
    form?.empresaId
  ]);

  useEffect(() => {
    if (!accionForm?.empresaId) {
      setAccionDepartamentos([]);
      return;
    }

    apiGet<CatalogoItem[]>(`/catalogos/departamentos?empresaId=${accionForm.empresaId}`)
      .then(setAccionDepartamentos)
      .catch((err) => setFormError(err instanceof Error ? err.message : 'No se pudieron cargar departamentos.'));
  }, [accionForm?.empresaId]);

  useEffect(() => {
    if (!accionForm?.empresaId || !accionForm.departamentoId) {
      setAccionCargos([]);
      return;
    }

    const params = new URLSearchParams({ empresaId: accionForm.empresaId, departamentoId: accionForm.departamentoId });
    apiGet<CatalogoItem[]>(`/catalogos/cargos?${params}`)
      .then(setAccionCargos)
      .catch((err) => setFormError(err instanceof Error ? err.message : 'No se pudieron cargar cargos.'));
  }, [accionForm?.departamentoId, accionForm?.empresaId]);

  useEffect(() => {
    if (!accionForm || !requiresExistingCollaborator(accionForm.tipoAccion) || !accionForm.empresaId || !accionForm.departamentoId || !accionForm.cargoId) {
      setAccionColaboradores([]);
      return;
    }

    const params = new URLSearchParams({
      empresaId: accionForm.empresaId,
      departamentoId: accionForm.departamentoId,
      cargoId: accionForm.cargoId,
      soloActivos: 'true',
      take: '100'
    });
    if (accionColaboradorSearch.trim()) {
      params.set('search', accionColaboradorSearch.trim());
    }
    apiGet<ColaboradorSelect[]>(`/colaboradores/select?${params}`)
      .then(setAccionColaboradores)
      .catch((err) => setFormError(err instanceof Error ? err.message : 'No se pudieron cargar colaboradores.'));
  }, [accionColaboradorSearch, accionForm?.cargoId, accionForm?.departamentoId, accionForm?.empresaId, accionForm?.tipoAccion]);

  useEffect(() => {
    if (!accionForm?.colaboradorId) {
      setColaboradorActual(null);
      return;
    }

    apiGet<ColaboradorResumenLaboral>(`/colaboradores/${accionForm.colaboradorId}/resumen-laboral`)
      .then((item) => {
        setColaboradorActual(item);
        setAccionForm((current) => current ? {
          ...current,
          empresaId: String(item.empresaId),
          departamentoId: String(item.departamentoId),
          cargoId: String(item.cargoId)
        } : current);
      })
      .catch((err) => setFormError(err instanceof Error ? err.message : 'No se pudo cargar resumen laboral.'));
  }, [accionForm?.colaboradorId]);

  useEffect(() => {
    if (!accionForm || !usesDestinationFields(accionForm.tipoAccion)) {
      setDestinoDepartamentos([]);
      return;
    }

    const empresaDestino = destinationEmpresaId(accionForm);
    if (!empresaDestino) {
      setDestinoDepartamentos([]);
      return;
    }

    apiGet<CatalogoItem[]>(`/catalogos/departamentos?empresaId=${empresaDestino}`)
      .then(setDestinoDepartamentos)
      .catch((err) => setFormError(err instanceof Error ? err.message : 'No se pudieron cargar departamentos destino.'));
  }, [accionForm?.empresaNuevaId, accionForm?.empresaTrasladoNuevaId, accionForm?.tipoAccion]);

  useEffect(() => {
    if (!accionForm || !usesDestinationFields(accionForm.tipoAccion)) {
      setDestinoCargos([]);
      return;
    }

    const empresaDestino = destinationEmpresaId(accionForm);
    const departamentoDestino = destinationDepartamentoId(accionForm);
    if (!empresaDestino || !departamentoDestino) {
      setDestinoCargos([]);
      return;
    }

    const params = new URLSearchParams({ empresaId: empresaDestino, departamentoId: departamentoDestino });
    apiGet<CatalogoItem[]>(`/catalogos/cargos?${params}`)
      .then(setDestinoCargos)
      .catch((err) => setFormError(err instanceof Error ? err.message : 'No se pudieron cargar cargos destino.'));
  }, [accionForm?.departamentoNuevoId, accionForm?.departamentoTrasladoNuevoId, accionForm?.empresaNuevaId, accionForm?.empresaTrasladoNuevaId, accionForm?.tipoAccion]);

  useEffect(() => {
    if (!accionForm || !usesDestinationFields(accionForm.tipoAccion)) {
      setDestinoJefes([]);
      return;
    }

    const empresaDestino = destinationEmpresaId(accionForm);
    const departamentoDestino = destinationDepartamentoId(accionForm);
    const cargoDestino = destinationCargoId(accionForm);
    if (!empresaDestino || !departamentoDestino || !cargoDestino) {
      setDestinoJefes([]);
      return;
    }

    const params = new URLSearchParams({
      empresaId: empresaDestino,
      departamentoId: departamentoDestino,
      cargoId: cargoDestino,
      soloActivos: 'true',
      take: '100'
    });
    apiGet<ColaboradorSelect[]>(`/colaboradores/select?${params}`)
      .then(setDestinoJefes)
      .catch((err) => setFormError(err instanceof Error ? err.message : 'No se pudieron cargar jefes destino.'));
  }, [accionForm?.cargoNuevoId, accionForm?.cargoTrasladoNuevoId, accionForm?.departamentoNuevoId, accionForm?.departamentoTrasladoNuevoId, accionForm?.empresaNuevaId, accionForm?.empresaTrasladoNuevaId, accionForm?.tipoAccion]);

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
    setAccionForm(null);
    setEditingId(null);
    setDetail(null);
    setFormError('');
    setNotice('');
    setError('');
    setFormDepartamentos([]);
  }

  async function openCreateAccionPersonal() {
    setAccionForm(emptyAccionFormState());
    setForm(null);
    setEditingId(null);
    setDetail(null);
    setFormError('');
    setNotice('');
    setError('');
    setAccionColaboradorSearch('');
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
    if (current.accionPersonal) {
      openEditAccionFromDetail(current);
      return;
    }

    if (!current.requisicion) return;
    setForm(toFormState(current));
    setAccionForm(null);
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

  function openEditAccionFromDetail(current: SolicitudDetail) {
    if (!current.accionPersonal) return;
    setAccionForm(toAccionFormState(current));
    setForm(null);
    setEditingId(current.solicitudId);
    setDetail(null);
    setFormError('');
    setAccionColaboradorSearch('');
  }

  function closeForm() {
    setForm(null);
    setAccionForm(null);
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
    setAccionForm((current) => current ? {
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
    setAccionForm((current) => current ? {
      ...current,
      departamentoResponsableId: value,
      liderAprobadorUsuarioId: selected?.usuarioResponsableId ? String(selected.usuarioResponsableId) : '',
      liderAprobadorColaboradorId: selected ? String(selected.colaboradorResponsableId) : ''
    } : current);
  }

  function changeAccionField<K extends keyof AccionPersonalFormState>(key: K, value: AccionPersonalFormState[K]) {
    setAccionForm((current) => current ? { ...current, [key]: value } : current);
  }

  function changeAccionTipo(value: string) {
    setAccionForm((current) => current ? {
      ...current,
      tipoAccion: value,
      colaboradorId: value === 'ContratacionIngreso' ? '' : current.colaboradorId,
      departamentoResponsableId: '',
      liderAprobadorUsuarioId: '',
      liderAprobadorColaboradorId: ''
    } : current);
    setAccionColaboradorSearch('');

    if (value === 'ContratacionIngreso') {
      setColaboradorActual(null);
      setAccionColaboradores([]);
    }
  }

  function changeAccionColaborador(value: string) {
    const selected = accionColaboradores.find((item) => String(item.colaboradorId) === value);
    setAccionForm((current) => current ? {
      ...current,
      colaboradorId: value,
      empresaId: selected ? String(selected.empresaId) : current.empresaId,
      departamentoId: selected ? String(selected.departamentoId) : current.departamentoId,
      cargoId: selected ? String(selected.cargoId) : current.cargoId,
      departamentoNuevoId: current.departamentoNuevoId,
      cargoNuevoId: current.cargoNuevoId,
      departamentoResponsableId: '',
      liderAprobadorUsuarioId: '',
      liderAprobadorColaboradorId: ''
    } : current);
  }

  function changeAccionEmpresa(value: string) {
    setAccionForm((current) => current ? {
      ...current,
      empresaId: value,
      departamentoId: '',
      cargoId: '',
      colaboradorId: '',
      departamentoResponsableId: '',
      liderAprobadorUsuarioId: '',
      liderAprobadorColaboradorId: ''
    } : current);
    setAccionCargos([]);
    setAccionColaboradores([]);
    setAccionColaboradorSearch('');
    setColaboradorActual(null);
  }

  function changeAccionDepartamento(value: string) {
    setAccionForm((current) => current ? {
      ...current,
      departamentoId: value,
      cargoId: '',
      colaboradorId: '',
      departamentoResponsableId: '',
      liderAprobadorUsuarioId: '',
      liderAprobadorColaboradorId: ''
    } : current);
    setAccionColaboradores([]);
    setAccionColaboradorSearch('');
    setColaboradorActual(null);
  }

  function changeAccionCargo(value: string) {
    setAccionForm((current) => current ? {
      ...current,
      cargoId: value,
      colaboradorId: ''
    } : current);
    setAccionColaboradorSearch('');
    setColaboradorActual(null);
  }

  function changeDestinoEmpresa(value: string) {
    setAccionForm((current) => {
      if (!current) return current;
      return current.tipoAccion === 'CambioPosicion'
        ? { ...current, empresaNuevaId: value, departamentoNuevoId: '', cargoNuevoId: '', jefeNuevoId: '' }
        : { ...current, empresaTrasladoNuevaId: value, departamentoTrasladoNuevoId: '', cargoTrasladoNuevoId: '', jefeTrasladoNuevoId: '' };
    });
    setDestinoCargos([]);
    setDestinoJefes([]);
  }

  function changeDestinoDepartamento(value: string) {
    setAccionForm((current) => {
      if (!current) return current;
      return current.tipoAccion === 'CambioPosicion'
        ? { ...current, departamentoNuevoId: value, cargoNuevoId: '', jefeNuevoId: '' }
        : { ...current, departamentoTrasladoNuevoId: value, cargoTrasladoNuevoId: '', jefeTrasladoNuevoId: '' };
    });
    setDestinoJefes([]);
  }

  function changeDestinoCargo(value: string) {
    setAccionForm((current) => {
      if (!current) return current;
      return current.tipoAccion === 'CambioPosicion'
        ? { ...current, cargoNuevoId: value, jefeNuevoId: '' }
        : { ...current, cargoTrasladoNuevoId: value, jefeTrasladoNuevoId: '' };
    });
  }

  async function saveRequisicion(enviar: boolean) {
    if (!form) return;

    if (enviar && user?.rol === 'Supervisor' && !form.departamentoResponsableId) {
      setFormError('Debe seleccionar un aprobador configurado en Organigrama antes de enviar.');
      return;
    }

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

  async function saveAccionPersonal(enviar: boolean) {
    if (!accionForm) return;

    if (enviar && user?.rol === 'Supervisor' && !accionForm.departamentoResponsableId) {
      setFormError('Debe seleccionar un aprobador configurado en Organigrama antes de enviar.');
      return;
    }

    setSaving(true);
    setFormError('');
    setNotice('');
    try {
      if (requiresExistingCollaborator(accionForm.tipoAccion) && !accionForm.colaboradorId) {
        throw new Error('Debe seleccionar un colaborador para este tipo de accion.');
      }

      const payload = toAccionPayload(accionForm, enviar);
      if (editingId) {
        await apiPut<{ solicitudId: number; codigoSolicitud: string }>(`/solicitudes/accion-personal/${editingId}`, payload);
        if (enviar) {
          await apiPost<SolicitudDetail>(`/solicitudes/${editingId}/enviar`, { comentario: 'Envio desde formulario.' });
        }
      } else {
        await apiPost<{ solicitudId: number; codigoSolicitud: string }>('/solicitudes/accion-personal', payload);
      }

      closeForm();
      setNotice(enviar ? 'Accion de personal enviada.' : 'Accion de personal guardada.');
      loadSolicitudes();
    } catch (err) {
      setFormError(err instanceof Error ? err.message : 'No se pudo guardar la accion de personal.');
    } finally {
      setSaving(false);
    }
  }

  async function executeAction(action: string, current: SolicitudDetail) {
    if (action === 'editar') {
      await openEditFromDetail(current);
      return;
    }

    if (action === 'ejecutar-accion') {
      const confirmed = window.confirm('Esta accion puede modificar datos reales del colaborador. ¿Desea continuar?');
      if (!confirmed) return;

      const comentario = window.prompt('Comentario opcional', '');
      if (comentario === null) return;

      setError('');
      setNotice('');
      try {
        const updated = await apiPost<SolicitudDetail>(`/solicitudes/accion-personal/${current.solicitudId}/ejecutar`, { comentario });
        setDetail(updated);
        setNotice('Accion de personal ejecutada.');
        loadSolicitudes();
      } catch (err) {
        setError(err instanceof Error ? err.message : 'No se pudo ejecutar la accion de personal.');
      }
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
    const isAccionPersonal = item.tipo === 'AccionPersonal';
    return (
      <button
        key={item.tipo}
        className={`request-type-card ${item.disponible ? '' : 'disabled'}`}
        type="button"
        disabled={!item.disponible}
        onClick={isRequisition ? openCreateRequisicion : isAccionPersonal ? openCreateAccionPersonal : undefined}
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
              <th>Pendiente de</th>
              <th>Acciones</th>
            </tr>
          </thead>
          <tbody>
            {solicitudes.length === 0 && (
              <tr><td colSpan={10}><div className="empty-state">Sin solicitudes</div></td></tr>
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
                <td>{item.pendienteDe ?? 'N/D'}</td>
                <td>
                  <div className="table-actions-stack">
                    <ActionChips actions={item.accionesDisponibles} />
                    <button className="icon-text-button" onClick={() => openDetail(item.solicitudId)} type="button">
                      <Eye size={16} />
                      Ver
                    </button>
                  </div>
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
                    <div className="form-note warning span-2">{approvalWarningText(canViewCompensation)}</div>
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

      {accionForm && (
        <div className="modal-backdrop" role="presentation">
          <section className="modal-panel solicitud-modal" role="dialog" aria-modal="true" aria-labelledby="accion-form-title">
            <div className="modal-header">
              <div>
                <h2 id="accion-form-title">{editingId ? 'Editar accion de personal' : 'Accion de Personal'}</h2>
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
                  <Select label="Tipo de accion" value={accionForm.tipoAccion} onChange={changeAccionTipo} options={tiposAccion.map((item) => ({ id: 0, nombre: item.nombre, codigo: item.tipo }))} emptyText="Seleccione" required valueFromCode />
                  <Select label="Empresa" value={accionForm.empresaId} onChange={changeAccionEmpresa} options={empresas} emptyText="Seleccione" required />
                  <Select label="Departamento" value={accionForm.departamentoId} onChange={changeAccionDepartamento} options={accionDepartamentos} emptyText="Seleccione" />
                  <Select label={accionForm.tipoAccion === 'ContratacionIngreso' ? 'Cargo' : 'Cargo actual'} value={accionForm.cargoId} onChange={changeAccionCargo} options={accionCargos} emptyText="Seleccione" />
                  {requiresExistingCollaborator(accionForm.tipoAccion) ? (
                    <>
                      <TextField label="Buscar colaborador" value={accionColaboradorSearch} onChange={setAccionColaboradorSearch} />
                      <Select
                        label="Colaborador"
                        value={accionForm.colaboradorId}
                        onChange={changeAccionColaborador}
                        options={accionColaboradores.map((item) => ({ id: item.colaboradorId, nombre: `${item.nombreCompleto} - ${item.noEmpleado} - ${item.cargo} - ${item.departamento} - ${item.empresa}` }))}
                        emptyText="Seleccione colaborador"
                        required
                      />
                    </>
                  ) : (
                    <div className="form-note span-2">Nuevo ingreso / sin colaborador existente.</div>
                  )}
                  <TextField label="Fecha efectiva" type="date" value={accionForm.fechaEfectiva} onChange={(value) => changeAccionField('fechaEfectiva', value)} required />
                  <AprobadorSelect label="Lider aprobador" value={accionForm.departamentoResponsableId} onChange={changeApprover} options={aprobadores} />
                  <TextareaField label="Justificacion" value={accionForm.justificacion} onChange={(value) => changeAccionField('justificacion', value)} span />
                  <TextareaField label="Observaciones" value={accionForm.observaciones} onChange={(value) => changeAccionField('observaciones', value)} span />
                  {accionForm.empresaId && accionForm.departamentoId && aprobadores.length === 0 && (
                    <div className="form-note warning span-2">{approvalWarningText(canViewCompensation)}</div>
                  )}
                </div>
              </div>

              {accionForm.colaboradorId && colaboradorActual && (
                <div className="form-section action-current-section">
                  <h3>Datos actuales del colaborador</h3>
                  <div className="detail-grid">
                    {renderSelectedCollaborator(colaboradorActual, canViewCompensation)}
                  </div>
                </div>
              )}

              {accionForm.tipoAccion === 'Vacaciones' && (
                <div className="form-section action-specific-section">
                  <h3>Vacaciones</h3>
                  <div className="edit-form-grid">
                    <TextField label="Dias" type="number" value={accionForm.diasVacaciones} onChange={(value) => changeAccionField('diasVacaciones', value)} required />
                    <TextField label="Fecha inicio" type="date" value={accionForm.fechaInicioVacaciones} onChange={(value) => changeAccionField('fechaInicioVacaciones', value)} required />
                    <TextField label="Fecha fin" type="date" value={accionForm.fechaFinVacaciones} onChange={(value) => changeAccionField('fechaFinVacaciones', value)} required />
                    <TextField label="Periodo desde" type="date" value={accionForm.periodoVacacionesDesde} onChange={(value) => changeAccionField('periodoVacacionesDesde', value)} />
                    <TextField label="Periodo hasta" type="date" value={accionForm.periodoVacacionesHasta} onChange={(value) => changeAccionField('periodoVacacionesHasta', value)} />
                    <TextField label="Quien reemplaza" value={accionForm.quienReemplaza} onChange={(value) => changeAccionField('quienReemplaza', value)} />
                  </div>
                </div>
              )}

              {accionForm.tipoAccion === 'ContratacionIngreso' && (
                <div className="form-section action-specific-section">
                  <h3>Contratacion / Ingreso</h3>
                  <div className="edit-form-grid">
                    <Select label="Tipo contrato" value={accionForm.tipoContratoNuevoId} onChange={(value) => changeAccionField('tipoContratoNuevoId', value)} options={tiposContrato} emptyText="Seleccione" required />
                    <TextField label="Fecha inicio contrato" type="date" value={accionForm.fechaInicioContrato} onChange={(value) => changeAccionField('fechaInicioContrato', value)} />
                    <TextField label="Fecha fin contrato" type="date" value={accionForm.fechaFinContrato} onChange={(value) => changeAccionField('fechaFinContrato', value)} />
                    <BooleanSelect label="Es reemplazo" value={accionForm.esReemplazo} onChange={(value) => changeAccionField('esReemplazo', value)} />
                    <BooleanSelect label="Posicion nueva" value={accionForm.esPosicionNueva} onChange={(value) => changeAccionField('esPosicionNueva', value)} />
                    <TextField label="Salario" type="number" value={accionForm.salarioNuevo} onChange={(value) => changeAccionField('salarioNuevo', value)} required />
                    <TextField label="Viaticos" type="number" value={accionForm.viaticosNuevo} onChange={(value) => changeAccionField('viaticosNuevo', value)} />
                    <TextField label="Gastos representacion" type="number" value={accionForm.gastosRepresentacionNuevo} onChange={(value) => changeAccionField('gastosRepresentacionNuevo', value)} />
                    <TextareaField label="Otros beneficios" value={accionForm.otrosBeneficios} onChange={(value) => changeAccionField('otrosBeneficios', value)} span />
                  </div>
                </div>
              )}

              {accionForm.tipoAccion === 'AjusteSalario' && (
                <div className="form-section action-specific-section">
                  <h3>Ajuste salarial</h3>
                  {canViewCompensation && colaboradorActual && (
                    <div className="detail-grid action-readonly-strip">
                      <DetailField label="Salario actual" value={formatMoney(colaboradorActual.salarioActual)} />
                      <DetailField label="Viaticos actuales" value={formatMoney(colaboradorActual.viaticosActual)} />
                      <DetailField label="Gastos representacion actuales" value={formatMoney(colaboradorActual.gastosRepresentacionActual)} />
                    </div>
                  )}
                  <div className="edit-form-grid">
                    <TextField label="Nuevo salario" type="number" value={accionForm.salarioNuevoAjuste} onChange={(value) => changeAccionField('salarioNuevoAjuste', value)} required />
                    <TextField label="Ajuste por mes" type="number" value={accionForm.ajustePorMes} onChange={(value) => changeAccionField('ajustePorMes', value)} />
                    <TextareaField label="Motivo" value={accionForm.motivoAjuste} onChange={(value) => changeAccionField('motivoAjuste', value)} span />
                  </div>
                </div>
              )}

              {accionForm.tipoAccion === 'CambioPosicion' && (
                <div className="form-section action-target-section">
                  <h3>Datos nuevos / destino</h3>
                  <div className="edit-form-grid">
                    <Select label="Empresa nueva" value={accionForm.empresaNuevaId} onChange={changeDestinoEmpresa} options={empresas} emptyText="Seleccione" required />
                    <Select label="Departamento nuevo" value={accionForm.departamentoNuevoId} onChange={changeDestinoDepartamento} options={destinoDepartamentos} emptyText="Seleccione" required />
                    <Select label="Cargo nuevo" value={accionForm.cargoNuevoId} onChange={changeDestinoCargo} options={destinoCargos} emptyText="Seleccione" required />
                    <Select label="Jefe nuevo" value={accionForm.jefeNuevoId} onChange={(value) => changeAccionField('jefeNuevoId', value)} options={colaboradorOptions(destinoJefes, accionForm.colaboradorId)} emptyText="Sin cambio" />
                  </div>
                </div>
              )}

              {accionForm.tipoAccion === 'TrasladoCambioArea' && (
                <div className="form-section action-target-section">
                  <h3>Datos nuevos / destino</h3>
                  <div className="edit-form-grid">
                    <Select label="Empresa traslado" value={accionForm.empresaTrasladoNuevaId} onChange={changeDestinoEmpresa} options={empresas} emptyText="Seleccione" required />
                    <Select label="Departamento traslado" value={accionForm.departamentoTrasladoNuevoId} onChange={changeDestinoDepartamento} options={destinoDepartamentos} emptyText="Seleccione" required />
                    <Select label="Cargo traslado" value={accionForm.cargoTrasladoNuevoId} onChange={changeDestinoCargo} options={destinoCargos} emptyText="Sin cambio" />
                    <Select label="Jefe traslado" value={accionForm.jefeTrasladoNuevoId} onChange={(value) => changeAccionField('jefeTrasladoNuevoId', value)} options={colaboradorOptions(destinoJefes, accionForm.colaboradorId)} emptyText="Sin cambio" />
                  </div>
                </div>
              )}

              {accionForm.tipoAccion === 'Licencia' && (
                <div className="form-section action-specific-section">
                  <h3>Licencia</h3>
                  <div className="edit-form-grid">
                    <TextField label="Tipo licencia" value={accionForm.tipoLicenciaAccion} onChange={(value) => changeAccionField('tipoLicenciaAccion', value)} />
                    <BooleanSelect label="Remunerada" value={accionForm.licenciaRemunerada} onChange={(value) => changeAccionField('licenciaRemunerada', value)} required />
                    <TextField label="Fecha inicio" type="date" value={accionForm.fechaInicioLicencia} onChange={(value) => changeAccionField('fechaInicioLicencia', value)} required />
                    <TextField label="Fecha fin" type="date" value={accionForm.fechaFinLicencia} onChange={(value) => changeAccionField('fechaFinLicencia', value)} required />
                    <TextareaField label="Especificacion" value={accionForm.especificacionLicencia} onChange={(value) => changeAccionField('especificacionLicencia', value)} span />
                  </div>
                </div>
              )}

              {accionForm.tipoAccion === 'FinalizacionDesvinculacion' && (
                <div className="form-section action-specific-section">
                  <h3>Finalizacion / Desvinculacion</h3>
                  <div className="edit-form-grid">
                    <TextField label="Tipo finalizacion" value={accionForm.tipoFinalizacion} onChange={(value) => changeAccionField('tipoFinalizacion', value)} required />
                    <TextField label="Fecha salida" type="date" value={accionForm.fechaSalida} onChange={(value) => changeAccionField('fechaSalida', value)} required />
                    <Select label="Motivo salida" value={accionForm.motivoSalidaId} onChange={(value) => changeAccionField('motivoSalidaId', value)} options={motivosSalida} emptyText="Seleccione" required />
                    <CheckField label="Menos de dos anios" checked={accionForm.menosDeDosAnios} onChange={(value) => changeAccionField('menosDeDosAnios', value)} />
                    <CheckField label="Terminacion periodo prueba" checked={accionForm.terminacionPeriodoPrueba} onChange={(value) => changeAccionField('terminacionPeriodoPrueba', value)} />
                    <CheckField label="Causa justificada" checked={accionForm.causaJustificada} onChange={(value) => changeAccionField('causaJustificada', value)} />
                    <CheckField label="Mutuo acuerdo" checked={accionForm.mutuoAcuerdo} onChange={(value) => changeAccionField('mutuoAcuerdo', value)} />
                    <CheckField label="Renovacion / extension" checked={accionForm.renovacionExtensionContrato} onChange={(value) => changeAccionField('renovacionExtensionContrato', value)} />
                    <CheckField label="Continuidad laboral" checked={accionForm.continuidadLaboral} onChange={(value) => changeAccionField('continuidadLaboral', value)} />
                    <BooleanSelect label="Lo recomienda" value={accionForm.loRecomienda} onChange={(value) => changeAccionField('loRecomienda', value)} />
                  </div>
                  <div className="edit-form-grid">
                    <Select label="Puntualidad" value={accionForm.puntualidad} onChange={(value) => changeAccionField('puntualidad', value)} options={evaluationOptions} emptyText="N/D" valueFromCode />
                    <Select label="Honestidad" value={accionForm.honestidad} onChange={(value) => changeAccionField('honestidad', value)} options={evaluationOptions} emptyText="N/D" valueFromCode />
                    <Select label="Trabajo equipo" value={accionForm.trabajoEquipo} onChange={(value) => changeAccionField('trabajoEquipo', value)} options={evaluationOptions} emptyText="N/D" valueFromCode />
                    <Select label="Productividad" value={accionForm.productividad} onChange={(value) => changeAccionField('productividad', value)} options={evaluationOptions} emptyText="N/D" valueFromCode />
                    <Select label="Iniciativa" value={accionForm.iniciativa} onChange={(value) => changeAccionField('iniciativa', value)} options={evaluationOptions} emptyText="N/D" valueFromCode />
                    <Select label="Respeto jefe" value={accionForm.respetoJefe} onChange={(value) => changeAccionField('respetoJefe', value)} options={evaluationOptions} emptyText="N/D" valueFromCode />
                    <Select label="Respeto companeros" value={accionForm.respetoCompaneros} onChange={(value) => changeAccionField('respetoCompaneros', value)} options={evaluationOptions} emptyText="N/D" valueFromCode />
                  </div>
                </div>
              )}

              {['RenovacionExtensionContrato', 'ContinuidadLaboral', 'Otro'].includes(accionForm.tipoAccion) && (
                <div className="form-section action-specific-section">
                  <h3>Detalle</h3>
                  <div className="edit-form-grid">
                    <TextareaField label="Observaciones especificas" value={accionForm.observaciones} onChange={(value) => changeAccionField('observaciones', value)} span />
                  </div>
                </div>
              )}

              <div className="modal-actions">
                <button className="secondary-button" type="button" onClick={closeForm}>Cancelar</button>
                <button className="secondary-button" disabled={saving} type="button" onClick={() => saveAccionPersonal(false)}>
                  <Save size={18} />
                  {saving ? 'Guardando...' : 'Guardar'}
                </button>
                <button className="primary-button" disabled={saving} type="button" onClick={() => saveAccionPersonal(true)}>
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

              {detail.accionPersonal && (
                <>
                  <section className="detail-section">
                    <h3>Accion de Personal</h3>
                    <div className="detail-grid">
                      <DetailField label="Tipo" value={detail.accionPersonal.tipoAccionNombre} />
                      <DetailField label="Colaborador" value={detail.accionPersonal.colaborador ?? 'N/D'} />
                      <DetailField label="No empleado" value={detail.accionPersonal.noEmpleadoSnapshot ?? 'N/D'} />
                      <DetailField label="Cedula" value={detail.accionPersonal.cedulaSnapshot ?? 'N/D'} />
                      <DetailField label="Empresa actual" value={detail.accionPersonal.empresaActual ?? detail.empresa ?? 'N/D'} />
                      <DetailField label="Departamento actual" value={detail.accionPersonal.departamentoActual ?? detail.departamento ?? 'N/D'} />
                      <DetailField label="Cargo actual" value={detail.accionPersonal.cargoActual ?? detail.cargo ?? 'N/D'} />
                      <DetailField label="Jefe actual" value={detail.accionPersonal.jefeActual ?? 'N/D'} />
                      <DetailField label="Salario actual" value={formatMoney(detail.accionPersonal.salarioActual)} />
                      <DetailField label="Ejecutada" value={detail.accionPersonal.ejecutada ? 'Si' : 'No'} />
                      <DetailField label="Resultado ejecucion" value={detail.accionPersonal.resultadoEjecucion ?? 'N/D'} span />
                    </div>
                  </section>

                  {detail.accionPersonal.alertaOrigenId && (
                    <section className="detail-section">
                      <h3>Alerta origen</h3>
                      <div className="detail-grid">
                        <DetailField label="Alerta" value={`#${detail.accionPersonal.alertaOrigenId}`} />
                        <DetailField label="Tipo alerta" value={detail.accionPersonal.alertaOrigenTipo ?? 'N/D'} />
                        <DetailField label="Fecha vencimiento" value={formatDate(detail.accionPersonal.alertaOrigenFechaVencimiento)} />
                        <DetailField label="Mensaje" value={detail.accionPersonal.alertaOrigenMensaje ?? 'N/D'} span />
                      </div>
                    </section>
                  )}

                  <section className="detail-section">
                    <h3>Datos especificos</h3>
                    <div className="detail-grid">
                      {renderAccionDetail(detail.accionPersonal)}
                    </div>
                  </section>

                  {detail.accionPersonal.cambiosAplicados.length > 0 && (
                    <section className="detail-section">
                      <h3>Cambios aplicados</h3>
                      <div className="timeline-list">
                        {detail.accionPersonal.cambiosAplicados.map((item) => (
                          <div key={item.accionPersonalCambioAplicadoId} className="timeline-item">
                            <span>{item.campo}</span>
                            <strong>{item.usuario}</strong>
                            <small>{formatDate(item.fecha)}</small>
                            <em>{item.valorAnterior ?? 'N/D'} {'->'} {item.valorNuevo ?? 'N/D'}</em>
                          </div>
                        ))}
                      </div>
                    </section>
                  )}
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
  { id: 0, nombre: 'Cerrada', codigo: 'Cerrada' },
  { id: 0, nombre: 'Ejecutada', codigo: 'Ejecutada' }
];

const evaluationOptions: CatalogoItem[] = [
  { id: 0, nombre: 'Deficiente', codigo: 'Deficiente' },
  { id: 0, nombre: 'Regular', codigo: 'Regular' },
  { id: 0, nombre: 'Bueno', codigo: 'Bueno' },
  { id: 0, nombre: 'Excelente', codigo: 'Excelente' }
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

function ActionChips({ actions }: { actions: string[] }) {
  if (actions.length === 0) {
    return <span className="muted-text">Sin acciones</span>;
  }

  return (
    <div className="action-chip-row">
      {actions.map((action) => (
        <span className="badge neutral" key={action}>{actionLabel(action)}</span>
      ))}
    </div>
  );
}

function approvalWarningText(canSendWithoutApprover: boolean) {
  return canSendWithoutApprover
    ? 'No hay aprobadores configurados para la empresa y departamento seleccionados. Admin/RRHH pueden continuar si el flujo actual lo permite.'
    : 'No hay aprobadores configurados para la empresa y departamento seleccionados. Debe seleccionar un aprobador antes de enviar.';
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

function BooleanSelect({
  label,
  value,
  onChange,
  required = false
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  required?: boolean;
}) {
  return (
    <label>
      {label}
      <select value={value} onChange={(event) => onChange(event.target.value)} required={required}>
        <option value="">Sin definir</option>
        <option value="true">Si</option>
        <option value="false">No</option>
      </select>
    </label>
  );
}

function CheckField({ label, checked, onChange }: { label: string; checked: boolean; onChange: (value: boolean) => void }) {
  return (
    <label className="check-label compact-check">
      <input type="checkbox" checked={checked} onChange={(event) => onChange(event.target.checked)} />
      {label}
    </label>
  );
}

function renderSelectedCollaborator(selected: ColaboradorResumenLaboral, showCompensation: boolean) {
  return (
    <>
      <DetailField label="Colaborador" value={selected.nombreCompleto} />
      <DetailField label="No empleado" value={selected.noEmpleado} />
      <DetailField label="Cedula" value={selected.cedula} />
      <DetailField label="Empresa actual" value={selected.empresa} />
      <DetailField label="Departamento actual" value={selected.departamento} />
      <DetailField label="Cargo actual" value={selected.cargo} />
      <DetailField label="Jefe actual" value={selected.jefeInmediato ?? 'N/D'} />
      <DetailField label="Tipo contrato actual" value={selected.tipoContrato} />
      <DetailField label="Estatus actual" value={selected.estatus} />
      {showCompensation && (
        <>
          <DetailField label="Salario actual" value={formatMoney(selected.salarioActual)} />
          <DetailField label="Viaticos actuales" value={formatMoney(selected.viaticosActual)} />
          <DetailField label="Gastos representacion actuales" value={formatMoney(selected.gastosRepresentacionActual)} />
        </>
      )}
    </>
  );
}

function colaboradorOptions(colaboradores: ColaboradorSelect[], excludeId: string): CatalogoItem[] {
  return colaboradores
    .filter((item) => String(item.colaboradorId) !== excludeId)
    .map((item) => ({ id: item.colaboradorId, nombre: `${item.nombreCompleto} - ${item.cargo} - ${item.departamento}` }));
}

function requiresExistingCollaborator(tipoAccion: string) {
  return [
    'Vacaciones',
    'AjusteSalario',
    'CambioPosicion',
    'TrasladoCambioArea',
    'Licencia',
    'FinalizacionDesvinculacion',
    'RenovacionExtensionContrato',
    'ContinuidadLaboral'
  ].includes(tipoAccion);
}

function usesDestinationFields(tipoAccion: string) {
  return tipoAccion === 'CambioPosicion' || tipoAccion === 'TrasladoCambioArea';
}

function destinationEmpresaId(form: AccionPersonalFormState) {
  return form.tipoAccion === 'CambioPosicion' ? form.empresaNuevaId : form.empresaTrasladoNuevaId;
}

function destinationDepartamentoId(form: AccionPersonalFormState) {
  return form.tipoAccion === 'CambioPosicion' ? form.departamentoNuevoId : form.departamentoTrasladoNuevoId;
}

function destinationCargoId(form: AccionPersonalFormState) {
  return form.tipoAccion === 'CambioPosicion' ? form.cargoNuevoId : form.cargoTrasladoNuevoId;
}

function renderAccionDetail(accion: AccionPersonal) {
  switch (accion.tipoAccion) {
    case 'Vacaciones':
      return (
        <>
          <DetailField label="Dias" value={formatNullable(accion.diasVacaciones)} />
          <DetailField label="Inicio" value={formatDate(accion.fechaInicioVacaciones)} />
          <DetailField label="Fin" value={formatDate(accion.fechaFinVacaciones)} />
          <DetailField label="Periodo" value={`${formatDate(accion.periodoVacacionesDesde)} - ${formatDate(accion.periodoVacacionesHasta)}`} />
          <DetailField label="Reemplazo" value={accion.quienReemplaza ?? 'N/D'} />
        </>
      );
    case 'ContratacionIngreso':
      return (
        <>
          <DetailField label="Tipo contrato" value={accion.tipoContratoNuevo ?? 'N/D'} />
          <DetailField label="Inicio contrato" value={formatDate(accion.fechaInicioContrato)} />
          <DetailField label="Fin contrato" value={formatDate(accion.fechaFinContrato)} />
          <DetailField label="Salario" value={formatMoney(accion.salarioNuevo)} />
          <DetailField label="Posicion nueva" value={formatBoolean(accion.esPosicionNueva)} />
          <DetailField label="Reemplazo" value={formatBoolean(accion.esReemplazo)} />
          <DetailField label="Beneficios" value={accion.otrosBeneficios ?? 'N/D'} span />
        </>
      );
    case 'AjusteSalario':
      return (
        <>
          <DetailField label="Salario anterior" value={formatMoney(accion.salarioAnterior ?? accion.salarioActual)} />
          <DetailField label="Nuevo salario" value={formatMoney(accion.salarioNuevoAjuste)} />
          <DetailField label="Ajuste por mes" value={formatMoney(accion.ajustePorMes)} />
          <DetailField label="Motivo" value={accion.motivoAjuste ?? 'N/D'} span />
        </>
      );
    case 'CambioPosicion':
      return (
        <>
          <DetailField label="Empresa nueva" value={accion.empresaNueva ?? 'Sin cambio'} />
          <DetailField label="Departamento nuevo" value={accion.departamentoNuevo ?? 'N/D'} />
          <DetailField label="Cargo nuevo" value={accion.cargoNuevo ?? 'N/D'} />
          <DetailField label="Jefe nuevo" value={accion.jefeNuevo ?? 'Sin cambio'} />
        </>
      );
    case 'TrasladoCambioArea':
      return (
        <>
          <DetailField label="Empresa traslado" value={accion.empresaTrasladoNueva ?? 'Sin cambio'} />
          <DetailField label="Departamento traslado" value={accion.departamentoTrasladoNuevo ?? 'N/D'} />
          <DetailField label="Cargo traslado" value={accion.cargoTrasladoNuevo ?? 'Sin cambio'} />
          <DetailField label="Jefe traslado" value={accion.jefeTrasladoNuevo ?? 'Sin cambio'} />
        </>
      );
    case 'Licencia':
      return (
        <>
          <DetailField label="Tipo licencia" value={accion.tipoLicenciaAccion ?? 'N/D'} />
          <DetailField label="Remunerada" value={formatBoolean(accion.licenciaRemunerada)} />
          <DetailField label="Inicio" value={formatDate(accion.fechaInicioLicencia)} />
          <DetailField label="Fin" value={formatDate(accion.fechaFinLicencia)} />
          <DetailField label="Especificacion" value={accion.especificacionLicencia ?? 'N/D'} span />
        </>
      );
    case 'FinalizacionDesvinculacion':
      return (
        <>
          <DetailField label="Tipo finalizacion" value={accion.tipoFinalizacion ?? 'N/D'} />
          <DetailField label="Fecha salida" value={formatDate(accion.fechaSalida)} />
          <DetailField label="Motivo salida" value={accion.motivoSalida ?? 'N/D'} />
          <DetailField label="Lo recomienda" value={formatBoolean(accion.loRecomienda)} />
          <DetailField label="Evaluacion" value={formatEvaluacionSalida(accion)} span />
        </>
      );
    default:
      return <DetailField label="Detalle" value={accion.observaciones ?? 'N/D'} span />;
  }
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

function emptyAccionFormState(): AccionPersonalFormState {
  return {
    tipoAccion: 'Vacaciones',
    colaboradorId: '',
    empresaId: '',
    departamentoId: '',
    cargoId: '',
    fechaEfectiva: '',
    justificacion: '',
    observaciones: '',
    liderAprobadorUsuarioId: '',
    liderAprobadorColaboradorId: '',
    departamentoResponsableId: '',
    diasVacaciones: '',
    fechaInicioVacaciones: '',
    fechaFinVacaciones: '',
    periodoVacacionesDesde: '',
    periodoVacacionesHasta: '',
    quienReemplaza: '',
    tipoContratoNuevoId: '',
    fechaInicioContrato: '',
    fechaFinContrato: '',
    esReemplazo: '',
    esPosicionNueva: '',
    salarioNuevo: '',
    viaticosNuevo: '',
    gastosRepresentacionNuevo: '',
    otrosBeneficios: '',
    salarioNuevoAjuste: '',
    ajustePorMes: '',
    motivoAjuste: '',
    cargoNuevoId: '',
    departamentoNuevoId: '',
    empresaNuevaId: '',
    jefeNuevoId: '',
    cargoTrasladoNuevoId: '',
    departamentoTrasladoNuevoId: '',
    empresaTrasladoNuevaId: '',
    jefeTrasladoNuevoId: '',
    tipoLicenciaAccion: '',
    licenciaRemunerada: '',
    fechaInicioLicencia: '',
    fechaFinLicencia: '',
    especificacionLicencia: '',
    tipoFinalizacion: '',
    fechaSalida: '',
    motivoSalidaId: '',
    menosDeDosAnios: false,
    terminacionPeriodoPrueba: false,
    causaJustificada: false,
    mutuoAcuerdo: false,
    renovacionExtensionContrato: false,
    continuidadLaboral: false,
    loRecomienda: '',
    puntualidad: '',
    honestidad: '',
    trabajoEquipo: '',
    productividad: '',
    iniciativa: '',
    respetoJefe: '',
    respetoCompaneros: ''
  };
}

function toAccionFormState(detail: SolicitudDetail): AccionPersonalFormState {
  const accion = detail.accionPersonal;
  const leader = detail.aprobaciones.find((item) => item.etapa === 'Lider');
  return {
    ...emptyAccionFormState(),
    tipoAccion: accion?.tipoAccion ?? 'Vacaciones',
    colaboradorId: accion?.colaboradorId ? String(accion.colaboradorId) : '',
    empresaId: detail.empresaId ? String(detail.empresaId) : accion?.empresaActualId ? String(accion.empresaActualId) : '',
    departamentoId: detail.departamentoId ? String(detail.departamentoId) : accion?.departamentoActualId ? String(accion.departamentoActualId) : '',
    cargoId: detail.cargoId ? String(detail.cargoId) : accion?.cargoActualId ? String(accion.cargoActualId) : '',
    fechaEfectiva: toDateInput(detail.fechaEfectiva ?? accion?.fechaEfectiva),
    justificacion: detail.justificacion ?? accion?.justificacion ?? '',
    observaciones: detail.observaciones ?? accion?.observaciones ?? '',
    liderAprobadorUsuarioId: leader?.usuarioAprobadorId ? String(leader.usuarioAprobadorId) : '',
    liderAprobadorColaboradorId: leader?.colaboradorAprobadorId ? String(leader.colaboradorAprobadorId) : '',
    departamentoResponsableId: leader?.departamentoResponsableId ? String(leader.departamentoResponsableId) : '',
    diasVacaciones: accion?.diasVacaciones ? String(accion.diasVacaciones) : '',
    fechaInicioVacaciones: toDateInput(accion?.fechaInicioVacaciones),
    fechaFinVacaciones: toDateInput(accion?.fechaFinVacaciones),
    periodoVacacionesDesde: toDateInput(accion?.periodoVacacionesDesde),
    periodoVacacionesHasta: toDateInput(accion?.periodoVacacionesHasta),
    quienReemplaza: accion?.quienReemplaza ?? '',
    tipoContratoNuevoId: accion?.tipoContratoNuevoId ? String(accion.tipoContratoNuevoId) : '',
    fechaInicioContrato: toDateInput(accion?.fechaInicioContrato),
    fechaFinContrato: toDateInput(accion?.fechaFinContrato),
    esReemplazo: boolToInput(accion?.esReemplazo),
    esPosicionNueva: boolToInput(accion?.esPosicionNueva),
    salarioNuevo: valueToInput(accion?.salarioNuevo),
    viaticosNuevo: valueToInput(accion?.viaticosNuevo),
    gastosRepresentacionNuevo: valueToInput(accion?.gastosRepresentacionNuevo),
    otrosBeneficios: accion?.otrosBeneficios ?? '',
    salarioNuevoAjuste: valueToInput(accion?.salarioNuevoAjuste),
    ajustePorMes: valueToInput(accion?.ajustePorMes),
    motivoAjuste: accion?.motivoAjuste ?? '',
    cargoNuevoId: accion?.cargoNuevoId ? String(accion.cargoNuevoId) : '',
    departamentoNuevoId: accion?.departamentoNuevoId ? String(accion.departamentoNuevoId) : '',
    empresaNuevaId: accion?.empresaNuevaId ? String(accion.empresaNuevaId) : '',
    jefeNuevoId: accion?.jefeNuevoId ? String(accion.jefeNuevoId) : '',
    cargoTrasladoNuevoId: accion?.cargoTrasladoNuevoId ? String(accion.cargoTrasladoNuevoId) : '',
    departamentoTrasladoNuevoId: accion?.departamentoTrasladoNuevoId ? String(accion.departamentoTrasladoNuevoId) : '',
    empresaTrasladoNuevaId: accion?.empresaTrasladoNuevaId ? String(accion.empresaTrasladoNuevaId) : '',
    jefeTrasladoNuevoId: accion?.jefeTrasladoNuevoId ? String(accion.jefeTrasladoNuevoId) : '',
    tipoLicenciaAccion: accion?.tipoLicenciaAccion ?? '',
    licenciaRemunerada: boolToInput(accion?.licenciaRemunerada),
    fechaInicioLicencia: toDateInput(accion?.fechaInicioLicencia),
    fechaFinLicencia: toDateInput(accion?.fechaFinLicencia),
    especificacionLicencia: accion?.especificacionLicencia ?? '',
    tipoFinalizacion: accion?.tipoFinalizacion ?? '',
    fechaSalida: toDateInput(accion?.fechaSalida),
    motivoSalidaId: accion?.motivoSalidaId ? String(accion.motivoSalidaId) : '',
    menosDeDosAnios: accion?.menosDeDosAnios ?? false,
    terminacionPeriodoPrueba: accion?.terminacionPeriodoPrueba ?? false,
    causaJustificada: accion?.causaJustificada ?? false,
    mutuoAcuerdo: accion?.mutuoAcuerdo ?? false,
    renovacionExtensionContrato: accion?.renovacionExtensionContrato ?? false,
    continuidadLaboral: accion?.continuidadLaboral ?? false,
    loRecomienda: boolToInput(accion?.loRecomienda),
    puntualidad: accion?.puntualidad ?? '',
    honestidad: accion?.honestidad ?? '',
    trabajoEquipo: accion?.trabajoEquipo ?? '',
    productividad: accion?.productividad ?? '',
    iniciativa: accion?.iniciativa ?? '',
    respetoJefe: accion?.respetoJefe ?? '',
    respetoCompaneros: accion?.respetoCompaneros ?? ''
  };
}

function toAccionPayload(form: AccionPersonalFormState, enviar: boolean): AccionPersonalRequest {
  return {
    tipoAccion: form.tipoAccion,
    colaboradorId: optionalNumber(form.colaboradorId),
    empresaId: requiredNumber(form.empresaId, 'Empresa'),
    departamentoId: optionalNumber(form.departamentoId),
    cargoId: optionalNumber(form.cargoId),
    fechaEfectiva: optionalDate(form.fechaEfectiva),
    justificacion: optionalText(form.justificacion),
    observaciones: optionalText(form.observaciones),
    liderAprobadorUsuarioId: optionalNumber(form.liderAprobadorUsuarioId),
    liderAprobadorColaboradorId: optionalNumber(form.liderAprobadorColaboradorId),
    departamentoResponsableId: optionalNumber(form.departamentoResponsableId),
    diasVacaciones: optionalNumber(form.diasVacaciones),
    fechaInicioVacaciones: optionalDate(form.fechaInicioVacaciones),
    fechaFinVacaciones: optionalDate(form.fechaFinVacaciones),
    periodoVacacionesDesde: optionalDate(form.periodoVacacionesDesde),
    periodoVacacionesHasta: optionalDate(form.periodoVacacionesHasta),
    quienReemplaza: optionalText(form.quienReemplaza),
    tipoContratoNuevoId: optionalNumber(form.tipoContratoNuevoId),
    fechaInicioContrato: optionalDate(form.fechaInicioContrato),
    fechaFinContrato: optionalDate(form.fechaFinContrato),
    esReemplazo: optionalBool(form.esReemplazo),
    esPosicionNueva: optionalBool(form.esPosicionNueva),
    salarioNuevo: optionalNumber(form.salarioNuevo),
    viaticosNuevo: optionalNumber(form.viaticosNuevo),
    gastosRepresentacionNuevo: optionalNumber(form.gastosRepresentacionNuevo),
    otrosBeneficios: optionalText(form.otrosBeneficios),
    salarioNuevoAjuste: optionalNumber(form.salarioNuevoAjuste),
    ajustePorMes: optionalNumber(form.ajustePorMes),
    motivoAjuste: optionalText(form.motivoAjuste),
    cargoNuevoId: optionalNumber(form.cargoNuevoId),
    departamentoNuevoId: optionalNumber(form.departamentoNuevoId),
    empresaNuevaId: optionalNumber(form.empresaNuevaId),
    jefeNuevoId: optionalNumber(form.jefeNuevoId),
    cargoTrasladoNuevoId: optionalNumber(form.cargoTrasladoNuevoId),
    departamentoTrasladoNuevoId: optionalNumber(form.departamentoTrasladoNuevoId),
    empresaTrasladoNuevaId: optionalNumber(form.empresaTrasladoNuevaId),
    jefeTrasladoNuevoId: optionalNumber(form.jefeTrasladoNuevoId),
    tipoLicenciaAccion: optionalText(form.tipoLicenciaAccion),
    licenciaRemunerada: optionalBool(form.licenciaRemunerada),
    fechaInicioLicencia: optionalDate(form.fechaInicioLicencia),
    fechaFinLicencia: optionalDate(form.fechaFinLicencia),
    especificacionLicencia: optionalText(form.especificacionLicencia),
    tipoFinalizacion: optionalText(form.tipoFinalizacion),
    fechaSalida: optionalDate(form.fechaSalida),
    motivoSalidaId: optionalNumber(form.motivoSalidaId),
    menosDeDosAnios: form.menosDeDosAnios,
    terminacionPeriodoPrueba: form.terminacionPeriodoPrueba,
    causaJustificada: form.causaJustificada,
    mutuoAcuerdo: form.mutuoAcuerdo,
    renovacionExtensionContrato: form.renovacionExtensionContrato,
    continuidadLaboral: form.continuidadLaboral,
    loRecomienda: optionalBool(form.loRecomienda),
    puntualidad: optionalText(form.puntualidad),
    honestidad: optionalText(form.honestidad),
    trabajoEquipo: optionalText(form.trabajoEquipo),
    productividad: optionalText(form.productividad),
    iniciativa: optionalText(form.iniciativa),
    respetoJefe: optionalText(form.respetoJefe),
    respetoCompaneros: optionalText(form.respetoCompaneros),
    enviar
  };
}

function actionIcon(action: string) {
  if (action === 'aprobar' || action === 'confirmar-rrhh') return <CheckCircle2 size={17} />;
  if (action === 'rechazar') return <XCircle size={17} />;
  if (action === 'devolver') return <RotateCcw size={17} />;
  if (action === 'cancelar') return <Ban size={17} />;
  if (action === 'enviar') return <Send size={17} />;
  if (action === 'ejecutar-accion') return <CheckCircle2 size={17} />;
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
    'ejecutar-accion': 'Ejecutar accion',
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
    'ejecutar-accion': 'Accion ejecutada.',
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
    Cerrada: 'Cerrada',
    Ejecutada: 'Ejecutada'
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

function optionalBool(value: string) {
  if (value === 'true') return true;
  if (value === 'false') return false;
  return null;
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

function boolToInput(value?: boolean | null) {
  if (value === true) return 'true';
  if (value === false) return 'false';
  return '';
}

function valueToInput(value?: number | null) {
  return value || value === 0 ? String(value) : '';
}

function formatNullable(value?: number | string | null) {
  return value || value === 0 ? String(value) : 'N/D';
}

function formatBoolean(value?: boolean | null) {
  if (value === true) return 'Si';
  if (value === false) return 'No';
  return 'N/D';
}

function formatEvaluacionSalida(accion: AccionPersonal) {
  const values = [
    ['Puntualidad', accion.puntualidad],
    ['Honestidad', accion.honestidad],
    ['Trabajo equipo', accion.trabajoEquipo],
    ['Productividad', accion.productividad],
    ['Iniciativa', accion.iniciativa],
    ['Respeto jefe', accion.respetoJefe],
    ['Respeto companeros', accion.respetoCompaneros]
  ].filter(([, value]) => value);

  return values.length === 0
    ? 'N/D'
    : values.map(([label, value]) => `${label}: ${value}`).join(' | ');
}
