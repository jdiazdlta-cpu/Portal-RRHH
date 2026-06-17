import { useCallback, useEffect, useMemo, useState } from 'react';
import type { Dispatch, FormEvent, SetStateAction } from 'react';
import { Eye, MoreVertical, Pencil, Power, PowerOff, Save, Search, X } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { apiGet, apiPatch, apiPut } from '../api/client';
import { useAuth } from '../auth/AuthContext';
import type { CatalogoItem, ColaboradorDetalle, ColaboradorList, ColaboradorUpsert, PagedResult } from '../types/api';
import { formatDate, statusClass } from '../utils/format';

type ColaboradorFormState = {
  noEmpleado: string;
  cedula: string;
  fechaVencimientoCedula: string;
  seguroSocial: string;
  primerNombre: string;
  segundoNombre: string;
  primerApellido: string;
  segundoApellido: string;
  sexo: string;
  telefono: string;
  email: string;
  fechaNacimiento: string;
  direccion: string;
  empresaId: string;
  departamentoId: string;
  cargoId: string;
  jefeInmediatoId: string;
  fechaIngreso: string;
  tipoContratoId: string;
  fechaVencimientoContrato: string;
  fechaVencimientoPeriodoProbatorio: string;
  tieneLicencia: boolean;
  numeroLicencia: string;
  tipoLicencia: string;
  fechaVencimientoLicencia: string;
  estatusId: string;
  salario: string;
  viaticos: string;
  gastosRepresentacion: string;
  fechaSalida: string;
  motivoSalidaId: string;
  vacante: boolean;
  ultimaVacacion: string;
};

export function ColaboradoresPage() {
  const navigate = useNavigate();
  const { hasRole } = useAuth();
  const canOperate = hasRole(['Admin', 'RRHH']);
  const [items, setItems] = useState<ColaboradorList[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const [empresaId, setEmpresaId] = useState('');
  const [departamentoId, setDepartamentoId] = useState('');
  const [cargoId, setCargoId] = useState('');
  const [estatusId, setEstatusId] = useState('');
  const [empresas, setEmpresas] = useState<CatalogoItem[]>([]);
  const [departamentos, setDepartamentos] = useState<CatalogoItem[]>([]);
  const [cargos, setCargos] = useState<CatalogoItem[]>([]);
  const [estatus, setEstatus] = useState<CatalogoItem[]>([]);
  const [tiposContrato, setTiposContrato] = useState<CatalogoItem[]>([]);
  const [motivosSalida, setMotivosSalida] = useState<CatalogoItem[]>([]);
  const [formDepartamentos, setFormDepartamentos] = useState<CatalogoItem[]>([]);
  const [formCargos, setFormCargos] = useState<CatalogoItem[]>([]);
  const [editing, setEditing] = useState<ColaboradorDetalle | null>(null);
  const [form, setForm] = useState<ColaboradorFormState | null>(null);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [formError, setFormError] = useState('');
  const [saving, setSaving] = useState(false);
  const [loadingEdit, setLoadingEdit] = useState(false);

  useEffect(() => {
    Promise.all([
      apiGet<CatalogoItem[]>('/catalogos/empresas'),
      apiGet<CatalogoItem[]>('/catalogos/estatus-colaborador'),
      apiGet<CatalogoItem[]>('/catalogos/tipos-contrato'),
      apiGet<CatalogoItem[]>('/catalogos/motivos-salida')
    ])
      .then(([companies, statuses, contracts, exitReasons]) => {
        setEmpresas(companies);
        setEstatus(statuses);
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
    const params = new URLSearchParams();
    if (departamentoId) params.set('departamentoId', departamentoId);
    apiGet<CatalogoItem[]>(`/catalogos/cargos${params.toString() ? `?${params}` : ''}`)
      .then(setCargos)
      .catch((err) => setError(err instanceof Error ? err.message : 'No se pudieron cargar cargos.'));
  }, [departamentoId]);

  const query = useMemo(() => {
    const params = new URLSearchParams({ page: String(page), pageSize: '25' });
    if (search.trim()) params.set('search', search.trim());
    if (empresaId) params.set('empresaId', empresaId);
    if (departamentoId) params.set('departamentoId', departamentoId);
    if (cargoId) params.set('cargoId', cargoId);
    if (estatusId) params.set('estatusId', estatusId);
    return params.toString();
  }, [cargoId, departamentoId, empresaId, estatusId, page, search]);

  const loadColaboradores = useCallback(() => {
    apiGet<PagedResult<ColaboradorList>>(`/colaboradores?${query}`)
      .then((data) => {
        setItems(data.items);
        setTotal(data.total);
      })
      .catch((err) => setError(err instanceof Error ? err.message : 'No se pudieron cargar colaboradores.'));
  }, [query]);

  useEffect(loadColaboradores, [loadColaboradores]);

  function applyFilters(event: FormEvent) {
    event.preventDefault();
    setPage(1);
  }

  function changeFilterEmpresa(value: string) {
    setEmpresaId(value);
    setDepartamentoId('');
    setCargoId('');
    setPage(1);
  }

  function changeFilterDepartamento(value: string) {
    setDepartamentoId(value);
    setCargoId('');
    setPage(1);
  }

  async function openEdit(id: number) {
    setLoadingEdit(true);
    setError('');
    setFormError('');
    try {
      const detail = await apiGet<ColaboradorDetalle>(`/colaboradores/${id}`);
      const [departments, positions] = await Promise.all([
        apiGet<CatalogoItem[]>(`/catalogos/departamentos?empresaId=${detail.empresaId}`),
        apiGet<CatalogoItem[]>(`/catalogos/cargos?departamentoId=${detail.departamentoId}`)
      ]);
      setFormDepartamentos(departments);
      setFormCargos(positions);
      setEditing(detail);
      setForm(toFormState(detail));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'No se pudo cargar el colaborador.');
    } finally {
      setLoadingEdit(false);
    }
  }

  async function changeFormEmpresa(value: string) {
    setForm((current) => current ? { ...current, empresaId: value, departamentoId: '', cargoId: '' } : current);
    setFormCargos([]);
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

  async function changeFormDepartamento(value: string) {
    setForm((current) => current ? { ...current, departamentoId: value, cargoId: '' } : current);
    if (!value) {
      setFormCargos([]);
      return;
    }

    try {
      setFormCargos(await apiGet<CatalogoItem[]>(`/catalogos/cargos?departamentoId=${value}`));
    } catch (err) {
      setFormError(err instanceof Error ? err.message : 'No se pudieron cargar cargos.');
    }
  }

  async function saveEdit(event: FormEvent) {
    event.preventDefault();
    if (!editing || !form) return;
    setSaving(true);
    setFormError('');
    setNotice('');

    try {
      const payload = toPayload(form);
      await apiPut(`/colaboradores/${editing.colaboradorId}`, payload);
      setEditing(null);
      setForm(null);
      setNotice('Colaborador actualizado correctamente.');
      loadColaboradores();
    } catch (err) {
      setFormError(err instanceof Error ? err.message : 'No se pudo actualizar el colaborador.');
    } finally {
      setSaving(false);
    }
  }

  async function toggleActive(item: ColaboradorList) {
    setError('');
    setNotice('');
    try {
      await apiPatch(`/colaboradores/${item.colaboradorId}/${item.isActive ? 'desactivar' : 'activar'}`);
      setNotice(item.isActive ? 'Colaborador desactivado.' : 'Colaborador activado.');
      loadColaboradores();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'No se pudo cambiar el estado del colaborador.');
    }
  }

  const totalPages = Math.max(1, Math.ceil(total / 25));

  return (
    <section className="page">
      <div className="page-heading">
        <div>
          <h1>Colaboradores</h1>
          <p>{total} registros</p>
        </div>
      </div>
      {error && <div className="error-box">{error}</div>}
      {notice && <div className="success-box">{notice}</div>}
      {loadingEdit && <div className="info-strip">Cargando colaborador...</div>}
      <form className="filter-row" onSubmit={applyFilters}>
        <label>
          Busqueda
          <div className="input-icon">
            <Search size={17} />
            <input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Nombre, cedula, empleado" />
          </div>
        </label>
        <Select label="Empresa" value={empresaId} onChange={changeFilterEmpresa} options={empresas} />
        <Select label="Departamento" value={departamentoId} onChange={changeFilterDepartamento} options={departamentos} />
        <Select label="Cargo" value={cargoId} onChange={(value) => { setCargoId(value); setPage(1); }} options={cargos} />
        <Select label="Estatus" value={estatusId} onChange={(value) => { setEstatusId(value); setPage(1); }} options={estatus} />
        <button className="secondary-button" type="submit">Filtrar</button>
      </form>

      <div className="table-wrap">
        <table>
          <thead>
            <tr>
              <th>No.</th>
              <th>Nombre</th>
              <th>Cedula</th>
              <th>Empresa</th>
              <th>Departamento</th>
              <th>Cargo</th>
              <th>Estatus</th>
              <th>Ingreso</th>
              <th>Acciones</th>
            </tr>
          </thead>
          <tbody>
            {items.length === 0 && (
              <tr><td colSpan={9}><div className="empty-state">Sin colaboradores</div></td></tr>
            )}
            {items.map((item) => (
              <tr key={item.colaboradorId}>
                <td>{item.noEmpleado}</td>
                <td>{item.nombreCompleto}</td>
                <td>{item.cedula}</td>
                <td>{item.empresa}</td>
                <td>{item.departamento}</td>
                <td>{item.cargo}</td>
                <td><span className={`badge ${statusClass(item.estatus)}`}>{item.estatus}</span></td>
                <td>{formatDate(item.fechaIngreso)}</td>
                <td>
                  <details className="action-menu">
                    <summary aria-label="Abrir acciones"><MoreVertical size={18} /></summary>
                    <div className="action-menu-popover">
                      <button onClick={() => navigate(`/colaboradores/${item.colaboradorId}`)} type="button">
                        <Eye size={16} />
                        Ver perfil
                      </button>
                      {canOperate && (
                        <button onClick={() => openEdit(item.colaboradorId)} type="button">
                          <Pencil size={16} />
                          Editar
                        </button>
                      )}
                      {canOperate && (
                        <button onClick={() => toggleActive(item)} type="button">
                          {item.isActive ? <PowerOff size={16} /> : <Power size={16} />}
                          {item.isActive ? 'Desactivar' : 'Activar'}
                        </button>
                      )}
                    </div>
                  </details>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <div className="pager">
        <button className="secondary-button" disabled={page <= 1} onClick={() => setPage((value) => value - 1)}>Anterior</button>
        <span>{page} / {totalPages}</span>
        <button className="secondary-button" disabled={page >= totalPages} onClick={() => setPage((value) => value + 1)}>Siguiente</button>
      </div>

      {editing && form && (
        <div className="modal-backdrop" role="presentation">
          <section className="modal-panel colaborador-modal" role="dialog" aria-modal="true" aria-labelledby="editar-colaborador-title">
            <div className="modal-header">
              <div>
                <h2 id="editar-colaborador-title">Editar colaborador</h2>
                <p>{editing.nombreCompleto}</p>
              </div>
              <button className="icon-button light" onClick={() => { setEditing(null); setForm(null); }} type="button" title="Cerrar" aria-label="Cerrar">
                <X size={18} />
              </button>
            </div>
            {formError && <div className="error-box">{formError}</div>}
            <form className="edit-modal-form" onSubmit={saveEdit}>
              <div className="form-section">
                <h3>Datos personales</h3>
                <div className="edit-form-grid">
                  <TextField label="No. empleado" value={form.noEmpleado} onChange={(value) => updateForm(setForm, 'noEmpleado', value)} required />
                  <TextField label="Cedula" value={form.cedula} onChange={(value) => updateForm(setForm, 'cedula', value)} required />
                  <TextField label="Vencimiento cedula" type="date" value={form.fechaVencimientoCedula} onChange={(value) => updateForm(setForm, 'fechaVencimientoCedula', value)} />
                  <TextField label="Seguro social" value={form.seguroSocial} onChange={(value) => updateForm(setForm, 'seguroSocial', value)} />
                  <TextField label="Primer nombre" value={form.primerNombre} onChange={(value) => updateForm(setForm, 'primerNombre', value)} required />
                  <TextField label="Segundo nombre" value={form.segundoNombre} onChange={(value) => updateForm(setForm, 'segundoNombre', value)} />
                  <TextField label="Primer apellido" value={form.primerApellido} onChange={(value) => updateForm(setForm, 'primerApellido', value)} required />
                  <TextField label="Segundo apellido" value={form.segundoApellido} onChange={(value) => updateForm(setForm, 'segundoApellido', value)} />
                  <TextField label="Sexo" value={form.sexo} onChange={(value) => updateForm(setForm, 'sexo', value)} />
                  <TextField label="Telefono" value={form.telefono} onChange={(value) => updateForm(setForm, 'telefono', value)} />
                  <TextField label="Email" type="email" value={form.email} onChange={(value) => updateForm(setForm, 'email', value)} />
                  <TextField label="Nacimiento" type="date" value={form.fechaNacimiento} onChange={(value) => updateForm(setForm, 'fechaNacimiento', value)} />
                  <label className="span-2">
                    Direccion
                    <input value={form.direccion} onChange={(event) => updateForm(setForm, 'direccion', event.target.value)} />
                  </label>
                </div>
              </div>

              <div className="form-section">
                <h3>Datos laborales</h3>
                <div className="edit-form-grid">
                  <Select label="Empresa" value={form.empresaId} onChange={changeFormEmpresa} options={empresas} required />
                  <Select label="Departamento" value={form.departamentoId} onChange={changeFormDepartamento} options={formDepartamentos} required />
                  <Select label="Cargo" value={form.cargoId} onChange={(value) => updateForm(setForm, 'cargoId', value)} options={formCargos} required />
                  <TextField label="Fecha ingreso" type="date" value={form.fechaIngreso} onChange={(value) => updateForm(setForm, 'fechaIngreso', value)} required />
                  <Select label="Tipo contrato" value={form.tipoContratoId} onChange={(value) => updateForm(setForm, 'tipoContratoId', value)} options={tiposContrato} required />
                  <Select label="Estatus" value={form.estatusId} onChange={(value) => updateForm(setForm, 'estatusId', value)} options={estatus} required />
                  <Select label="Motivo salida" value={form.motivoSalidaId} onChange={(value) => updateForm(setForm, 'motivoSalidaId', value)} options={motivosSalida} />
                  <TextField label="Fecha salida" type="date" value={form.fechaSalida} onChange={(value) => updateForm(setForm, 'fechaSalida', value)} />
                  {editing.jefeInmediato && <p className="form-note span-2">Jefe inmediato actual: {editing.jefeInmediato}</p>}
                </div>
              </div>

              <div className="form-section">
                <h3>Vencimientos y compensacion</h3>
                <div className="edit-form-grid">
                  <TextField label="Vencimiento contrato" type="date" value={form.fechaVencimientoContrato} onChange={(value) => updateForm(setForm, 'fechaVencimientoContrato', value)} />
                  <TextField label="Periodo probatorio" type="date" value={form.fechaVencimientoPeriodoProbatorio} onChange={(value) => updateForm(setForm, 'fechaVencimientoPeriodoProbatorio', value)} />
                  <label className="check-label compact-check">
                    <input type="checkbox" checked={form.tieneLicencia} onChange={(event) => updateForm(setForm, 'tieneLicencia', event.target.checked)} />
                    Tiene licencia
                  </label>
                  <TextField label="Numero licencia" value={form.numeroLicencia} onChange={(value) => updateForm(setForm, 'numeroLicencia', value)} />
                  <TextField label="Tipo licencia" value={form.tipoLicencia} onChange={(value) => updateForm(setForm, 'tipoLicencia', value)} />
                  <TextField label="Vencimiento licencia" type="date" value={form.fechaVencimientoLicencia} onChange={(value) => updateForm(setForm, 'fechaVencimientoLicencia', value)} />
                  <TextField label="Salario" type="number" value={form.salario} onChange={(value) => updateForm(setForm, 'salario', value)} />
                  <TextField label="Viaticos" type="number" value={form.viaticos} onChange={(value) => updateForm(setForm, 'viaticos', value)} />
                  <TextField label="Gastos representacion" type="number" value={form.gastosRepresentacion} onChange={(value) => updateForm(setForm, 'gastosRepresentacion', value)} />
                  <TextField label="Ultima vacacion" type="date" value={form.ultimaVacacion} onChange={(value) => updateForm(setForm, 'ultimaVacacion', value)} />
                  <label className="check-label compact-check">
                    <input type="checkbox" checked={form.vacante} onChange={(event) => updateForm(setForm, 'vacante', event.target.checked)} />
                    Vacante
                  </label>
                </div>
              </div>

              <div className="modal-actions">
                <button className="secondary-button" type="button" onClick={() => { setEditing(null); setForm(null); }}>Cancelar</button>
                <button className="primary-button" disabled={saving} type="submit">
                  <Save size={18} />
                  {saving ? 'Guardando...' : 'Guardar cambios'}
                </button>
              </div>
            </form>
          </section>
        </div>
      )}
    </section>
  );
}

function Select({
  label,
  value,
  onChange,
  options,
  required = false
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  options: CatalogoItem[];
  required?: boolean;
}) {
  return (
    <label>
      {label}
      <select value={value} onChange={(event) => onChange(event.target.value)} required={required}>
        <option value="">{required ? 'Seleccione' : 'Todos'}</option>
        {options.map((item) => (
          <option key={item.id} value={item.id}>{item.nombre}</option>
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

function updateForm<K extends keyof ColaboradorFormState>(
  setter: Dispatch<SetStateAction<ColaboradorFormState | null>>,
  key: K,
  value: ColaboradorFormState[K]
) {
  setter((current) => current ? { ...current, [key]: value } : current);
}

function toFormState(detail: ColaboradorDetalle): ColaboradorFormState {
  return {
    noEmpleado: detail.noEmpleado,
    cedula: detail.cedula,
    fechaVencimientoCedula: toDateInput(detail.fechaVencimientoCedula),
    seguroSocial: detail.seguroSocial ?? '',
    primerNombre: detail.primerNombre,
    segundoNombre: detail.segundoNombre ?? '',
    primerApellido: detail.primerApellido,
    segundoApellido: detail.segundoApellido ?? '',
    sexo: detail.sexo ?? '',
    telefono: detail.telefono ?? '',
    email: detail.email ?? '',
    fechaNacimiento: toDateInput(detail.fechaNacimiento),
    direccion: detail.direccion ?? '',
    empresaId: String(detail.empresaId),
    departamentoId: String(detail.departamentoId),
    cargoId: String(detail.cargoId),
    jefeInmediatoId: detail.jefeInmediatoId ? String(detail.jefeInmediatoId) : '',
    fechaIngreso: toDateInput(detail.fechaIngreso),
    tipoContratoId: String(detail.tipoContratoId),
    fechaVencimientoContrato: toDateInput(detail.fechaVencimientoContrato),
    fechaVencimientoPeriodoProbatorio: toDateInput(detail.fechaVencimientoPeriodoProbatorio),
    tieneLicencia: detail.tieneLicencia,
    numeroLicencia: detail.numeroLicencia ?? '',
    tipoLicencia: detail.tipoLicencia ?? '',
    fechaVencimientoLicencia: toDateInput(detail.fechaVencimientoLicencia),
    estatusId: String(detail.estatusId),
    salario: String(detail.salario ?? 0),
    viaticos: String(detail.viaticos ?? 0),
    gastosRepresentacion: String(detail.gastosRepresentacion ?? 0),
    fechaSalida: toDateInput(detail.fechaSalida),
    motivoSalidaId: detail.motivoSalidaId ? String(detail.motivoSalidaId) : '',
    vacante: detail.vacante,
    ultimaVacacion: toDateInput(detail.ultimaVacacion)
  };
}

function toPayload(form: ColaboradorFormState): ColaboradorUpsert {
  return {
    noEmpleado: form.noEmpleado.trim(),
    cedula: form.cedula.trim(),
    fechaVencimientoCedula: optionalDate(form.fechaVencimientoCedula),
    seguroSocial: optionalText(form.seguroSocial),
    primerNombre: form.primerNombre.trim(),
    segundoNombre: optionalText(form.segundoNombre),
    primerApellido: form.primerApellido.trim(),
    segundoApellido: optionalText(form.segundoApellido),
    sexo: optionalText(form.sexo),
    telefono: optionalText(form.telefono),
    email: optionalText(form.email),
    fechaNacimiento: optionalDate(form.fechaNacimiento),
    direccion: optionalText(form.direccion),
    empresaId: requiredNumber(form.empresaId, 'Empresa'),
    departamentoId: requiredNumber(form.departamentoId, 'Departamento'),
    cargoId: requiredNumber(form.cargoId, 'Cargo'),
    jefeInmediatoId: optionalNumber(form.jefeInmediatoId),
    fechaIngreso: optionalDate(form.fechaIngreso) ?? '',
    tipoContratoId: requiredNumber(form.tipoContratoId, 'Tipo contrato'),
    fechaVencimientoContrato: optionalDate(form.fechaVencimientoContrato),
    fechaVencimientoPeriodoProbatorio: optionalDate(form.fechaVencimientoPeriodoProbatorio),
    tieneLicencia: form.tieneLicencia,
    numeroLicencia: optionalText(form.numeroLicencia),
    tipoLicencia: optionalText(form.tipoLicencia),
    fechaVencimientoLicencia: optionalDate(form.fechaVencimientoLicencia),
    estatusId: requiredNumber(form.estatusId, 'Estatus'),
    salario: numberOrZero(form.salario),
    viaticos: numberOrZero(form.viaticos),
    gastosRepresentacion: numberOrZero(form.gastosRepresentacion),
    fechaSalida: optionalDate(form.fechaSalida),
    motivoSalidaId: optionalNumber(form.motivoSalidaId),
    vacante: form.vacante,
    ultimaVacacion: optionalDate(form.ultimaVacacion)
  };
}

function toDateInput(value?: string | null) {
  return value ? value.slice(0, 10) : '';
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

function numberOrZero(value: string) {
  return value ? Number(value) : 0;
}
