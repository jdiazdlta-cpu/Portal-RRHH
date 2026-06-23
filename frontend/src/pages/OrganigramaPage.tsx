import { useCallback, useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { Edit3, GitBranch, Network, Plus, Save, Search, ShieldCheck, Users, X } from 'lucide-react';
import { apiGet, apiPost, apiPut } from '../api/client';
import { useAuth } from '../auth/AuthContext';
import type {
  AprobadorSolicitud,
  CatalogoItem,
  ColaboradorLookup,
  DepartamentoResponsable,
  DepartamentoResponsableRequest,
  OrganigramaDetail,
  OrganigramaList,
  OrganigramaNodo,
  OrganigramaNodoRequest,
  OrganigramaRequest,
  Usuario
} from '../types/api';
import { formatDate } from '../utils/format';

type TabKey = 'organigramas' | 'nodos' | 'responsables' | 'aprobadores';

type OrganigramaForm = {
  organigramaId?: number;
  nombre: string;
  empresaId: string;
  descripcion: string;
  fechaInicio: string;
  fechaFin: string;
  isActive: boolean;
};

type NodoForm = {
  organigramaNodoId?: number;
  organigramaId: string;
  nombreNodo: string;
  empresaId: string;
  departamentoId: string;
  cargoId: string;
  nodoPadreId: string;
  descripcion: string;
  nivel: string;
  orden: string;
  esRolOperativo: boolean;
  isActive: boolean;
};

type ResponsableForm = {
  departamentoResponsableId?: number;
  empresaId: string;
  departamentoId: string;
  colaboradorResponsableId: string;
  usuarioResponsableId: string;
  tipoResponsable: string;
  esPrincipal: boolean;
  puedeAprobarSolicitudes: boolean;
  fechaInicio: string;
  fechaFin: string;
  observacion: string;
  isActive: boolean;
};

const tabs: Array<{ key: TabKey; label: string; icon: typeof Network }> = [
  { key: 'organigramas', label: 'Organigramas', icon: Network },
  { key: 'nodos', label: 'Nodos', icon: GitBranch },
  { key: 'responsables', label: 'Responsables', icon: Users },
  { key: 'aprobadores', label: 'Aprobadores', icon: ShieldCheck }
];

const responsibleTypes = ['LiderPrincipal', 'LiderAlterno', 'Supervisor', 'Coordinador', 'Gerente', 'RRHHApoyo', 'Otro'];

export function OrganigramaPage() {
  const { user } = useAuth();
  const canAdmin = user?.rol === 'Admin';
  const [activeTab, setActiveTab] = useState<TabKey>('organigramas');
  const [empresas, setEmpresas] = useState<CatalogoItem[]>([]);
  const [departamentos, setDepartamentos] = useState<CatalogoItem[]>([]);
  const [cargos, setCargos] = useState<CatalogoItem[]>([]);
  const [usuarios, setUsuarios] = useState<Usuario[]>([]);
  const [organigramas, setOrganigramas] = useState<OrganigramaList[]>([]);
  const [selectedOrgId, setSelectedOrgId] = useState('');
  const [orgDetail, setOrgDetail] = useState<OrganigramaDetail | null>(null);
  const [responsables, setResponsables] = useState<DepartamentoResponsable[]>([]);
  const [aprobadores, setAprobadores] = useState<AprobadorSolicitud[]>([]);
  const [colaboradores, setColaboradores] = useState<ColaboradorLookup[]>([]);
  const [filterEmpresaId, setFilterEmpresaId] = useState('');
  const [filterDepartamentoId, setFilterDepartamentoId] = useState('');
  const [colaboradorSearch, setColaboradorSearch] = useState('');
  const [organigramaForm, setOrganigramaForm] = useState<OrganigramaForm | null>(null);
  const [nodoForm, setNodoForm] = useState<NodoForm | null>(null);
  const [responsableForm, setResponsableForm] = useState<ResponsableForm | null>(null);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');

  const selectedOrgNodes = orgDetail?.nodos ?? [];

  useEffect(() => {
    Promise.all([
      apiGet<CatalogoItem[]>('/catalogos/empresas'),
      apiGet<CatalogoItem[]>('/catalogos/departamentos'),
      apiGet<CatalogoItem[]>('/catalogos/cargos')
    ])
      .then(([companies, departments, positions]) => {
        setEmpresas(companies);
        setDepartamentos(departments);
        setCargos(positions);
      })
      .catch((err) => setError(err instanceof Error ? err.message : 'No se pudieron cargar catalogos.'));
  }, []);

  useEffect(() => {
    if (!canAdmin) return;
    apiGet<Usuario[]>('/usuarios')
      .then(setUsuarios)
      .catch(() => setUsuarios([]));
  }, [canAdmin]);

  const loadOrganigramas = useCallback(() => {
    const params = new URLSearchParams();
    if (filterEmpresaId) params.set('empresaId', filterEmpresaId);
    setLoading(true);
    apiGet<OrganigramaList[]>(`/organigrama${params.toString() ? `?${params}` : ''}`)
      .then((data) => {
        setOrganigramas(data);
        setSelectedOrgId((current) => current || (data[0]?.organigramaId ? String(data[0].organigramaId) : ''));
      })
      .catch((err) => setError(err instanceof Error ? err.message : 'No se pudieron cargar organigramas.'))
      .finally(() => setLoading(false));
  }, [filterEmpresaId]);

  const loadResponsables = useCallback(() => {
    const params = new URLSearchParams();
    if (filterEmpresaId) params.set('empresaId', filterEmpresaId);
    if (filterDepartamentoId) params.set('departamentoId', filterDepartamentoId);
    apiGet<DepartamentoResponsable[]>(`/organigrama/responsables${params.toString() ? `?${params}` : ''}`)
      .then(setResponsables)
      .catch((err) => setError(err instanceof Error ? err.message : 'No se pudieron cargar responsables.'));
  }, [filterDepartamentoId, filterEmpresaId]);

  const loadAprobadores = useCallback(() => {
    const params = new URLSearchParams();
    if (filterEmpresaId) params.set('empresaId', filterEmpresaId);
    if (filterDepartamentoId) params.set('departamentoId', filterDepartamentoId);
    apiGet<AprobadorSolicitud[]>(`/organigrama/aprobadores${params.toString() ? `?${params}` : ''}`)
      .then(setAprobadores)
      .catch((err) => setError(err instanceof Error ? err.message : 'No se pudieron cargar aprobadores.'));
  }, [filterDepartamentoId, filterEmpresaId]);

  useEffect(loadOrganigramas, [loadOrganigramas]);
  useEffect(loadResponsables, [loadResponsables]);
  useEffect(loadAprobadores, [loadAprobadores]);

  useEffect(() => {
    if (!selectedOrgId) {
      setOrgDetail(null);
      return;
    }

    apiGet<OrganigramaDetail>(`/organigrama/${selectedOrgId}`)
      .then(setOrgDetail)
      .catch((err) => setError(err instanceof Error ? err.message : 'No se pudo cargar el detalle del organigrama.'));
  }, [selectedOrgId]);

  useEffect(() => {
    if (!responsableForm) return;
    const params = new URLSearchParams();
    if (responsableForm.empresaId) params.set('empresaId', responsableForm.empresaId);
    if (responsableForm.departamentoId) params.set('departamentoId', responsableForm.departamentoId);
    if (colaboradorSearch) params.set('search', colaboradorSearch);

    apiGet<ColaboradorLookup[]>(`/organigrama/colaboradores-activos${params.toString() ? `?${params}` : ''}`)
      .then(setColaboradores)
      .catch((err) => setError(err instanceof Error ? err.message : 'No se pudieron cargar colaboradores activos.'));
  }, [colaboradorSearch, responsableForm?.departamentoId, responsableForm?.empresaId]);

  function changeEmpresaFilter(value: string) {
    setFilterEmpresaId(value);
    setFilterDepartamentoId('');
    loadDepartamentos(value);
    setCargos([]);
  }

  function loadDepartamentos(empresaId: string) {
    const params = new URLSearchParams();
    if (empresaId) params.set('empresaId', empresaId);
    apiGet<CatalogoItem[]>(`/catalogos/departamentos${params.toString() ? `?${params}` : ''}`)
      .then(setDepartamentos)
      .catch((err) => setError(err instanceof Error ? err.message : 'No se pudieron cargar departamentos.'));
  }

  function loadCargos(departamentoId: string) {
    const params = new URLSearchParams();
    if (departamentoId) params.set('departamentoId', departamentoId);
    apiGet<CatalogoItem[]>(`/catalogos/cargos${params.toString() ? `?${params}` : ''}`)
      .then(setCargos)
      .catch((err) => setError(err instanceof Error ? err.message : 'No se pudieron cargar cargos.'));
  }

  function openOrganigramaForm(item?: OrganigramaList) {
    if (!canAdmin) return;
    setError('');
    setNotice('');
    setOrganigramaForm(item ? {
      organigramaId: item.organigramaId,
      nombre: item.nombre,
      empresaId: item.empresaId ? String(item.empresaId) : '',
      descripcion: item.descripcion ?? '',
      fechaInicio: toDateInput(item.fechaInicio),
      fechaFin: toDateInput(item.fechaFin),
      isActive: item.isActive
    } : emptyOrganigramaForm(filterEmpresaId));
  }

  function openNodoForm(item?: OrganigramaNodo) {
    if (!canAdmin || !selectedOrgId) return;
    setError('');
    setNotice('');
    setNodoForm(item ? {
      organigramaNodoId: item.organigramaNodoId,
      organigramaId: String(item.organigramaId),
      nombreNodo: item.nombreNodo,
      empresaId: item.empresaId ? String(item.empresaId) : '',
      departamentoId: item.departamentoId ? String(item.departamentoId) : '',
      cargoId: item.cargoId ? String(item.cargoId) : '',
      nodoPadreId: item.nodoPadreId ? String(item.nodoPadreId) : '',
      descripcion: item.descripcion ?? '',
      nivel: String(item.nivel),
      orden: String(item.orden),
      esRolOperativo: item.esRolOperativo,
      isActive: item.isActive
    } : emptyNodoForm(selectedOrgId, filterEmpresaId));
    loadDepartamentos(item?.empresaId ? String(item.empresaId) : filterEmpresaId);
    loadCargos(item?.departamentoId ? String(item.departamentoId) : '');
  }

  function openResponsableForm(item?: DepartamentoResponsable) {
    if (!canAdmin) return;
    setError('');
    setNotice('');
    setColaboradorSearch('');
    setResponsableForm(item ? {
      departamentoResponsableId: item.departamentoResponsableId,
      empresaId: String(item.empresaId),
      departamentoId: String(item.departamentoId),
      colaboradorResponsableId: String(item.colaboradorResponsableId),
      usuarioResponsableId: item.usuarioResponsableId ? String(item.usuarioResponsableId) : '',
      tipoResponsable: item.tipoResponsable,
      esPrincipal: item.esPrincipal,
      puedeAprobarSolicitudes: item.puedeAprobarSolicitudes,
      fechaInicio: toDateInput(item.fechaInicio),
      fechaFin: toDateInput(item.fechaFin),
      observacion: item.observacion ?? '',
      isActive: item.isActive
    } : emptyResponsableForm(filterEmpresaId, filterDepartamentoId));
    loadDepartamentos(item?.empresaId ? String(item.empresaId) : filterEmpresaId);
  }

  function closeModals() {
    setOrganigramaForm(null);
    setNodoForm(null);
    setResponsableForm(null);
    setColaboradorSearch('');
  }

  async function saveOrganigrama(event: FormEvent) {
    event.preventDefault();
    if (!organigramaForm) return;
    setSaving(true);
    setError('');
    try {
      const payload: OrganigramaRequest = {
        nombre: organigramaForm.nombre.trim(),
        empresaId: optionalNumber(organigramaForm.empresaId),
        descripcion: optionalText(organigramaForm.descripcion),
        fechaInicio: organigramaForm.fechaInicio,
        fechaFin: optionalDate(organigramaForm.fechaFin),
        isActive: organigramaForm.isActive
      };

      if (organigramaForm.organigramaId) {
        await apiPut(`/organigrama/${organigramaForm.organigramaId}`, payload);
      } else {
        await apiPost('/organigrama', payload);
      }

      setNotice('Organigrama guardado.');
      closeModals();
      loadOrganigramas();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'No se pudo guardar el organigrama.');
    } finally {
      setSaving(false);
    }
  }

  async function saveNodo(event: FormEvent) {
    event.preventDefault();
    if (!nodoForm) return;
    setSaving(true);
    setError('');
    try {
      const payload: OrganigramaNodoRequest = {
        empresaId: optionalNumber(nodoForm.empresaId),
        departamentoId: optionalNumber(nodoForm.departamentoId),
        cargoId: optionalNumber(nodoForm.cargoId),
        nodoPadreId: optionalNumber(nodoForm.nodoPadreId),
        nombreNodo: nodoForm.nombreNodo.trim(),
        descripcion: optionalText(nodoForm.descripcion),
        nivel: Number(nodoForm.nivel || 0),
        orden: Number(nodoForm.orden || 0),
        esRolOperativo: nodoForm.esRolOperativo,
        isActive: nodoForm.isActive
      };

      if (nodoForm.organigramaNodoId) {
        await apiPut(`/organigrama/nodos/${nodoForm.organigramaNodoId}`, payload);
      } else {
        await apiPost(`/organigrama/${nodoForm.organigramaId}/nodos`, payload);
      }

      setNotice('Nodo guardado.');
      closeModals();
      setOrgDetail(await apiGet<OrganigramaDetail>(`/organigrama/${nodoForm.organigramaId}`));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'No se pudo guardar el nodo.');
    } finally {
      setSaving(false);
    }
  }

  async function saveResponsable(event: FormEvent) {
    event.preventDefault();
    if (!responsableForm) return;
    setSaving(true);
    setError('');
    try {
      const payload: DepartamentoResponsableRequest = {
        empresaId: requiredNumber(responsableForm.empresaId, 'Empresa'),
        departamentoId: requiredNumber(responsableForm.departamentoId, 'Departamento'),
        colaboradorResponsableId: requiredNumber(responsableForm.colaboradorResponsableId, 'Colaborador responsable'),
        usuarioResponsableId: optionalNumber(responsableForm.usuarioResponsableId),
        tipoResponsable: responsableForm.tipoResponsable,
        esPrincipal: responsableForm.esPrincipal,
        puedeAprobarSolicitudes: responsableForm.puedeAprobarSolicitudes,
        fechaInicio: responsableForm.fechaInicio,
        fechaFin: optionalDate(responsableForm.fechaFin),
        observacion: optionalText(responsableForm.observacion),
        isActive: responsableForm.isActive
      };

      if (responsableForm.departamentoResponsableId) {
        await apiPut(`/organigrama/responsables/${responsableForm.departamentoResponsableId}`, payload);
      } else {
        await apiPost('/organigrama/responsables', payload);
      }

      setNotice('Responsable guardado.');
      closeModals();
      loadResponsables();
      loadAprobadores();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'No se pudo guardar el responsable.');
    } finally {
      setSaving(false);
    }
  }

  return (
    <section className="page">
      <div className="page-heading">
        <div>
          <h1>Organigrama</h1>
          <p>{canAdmin ? 'Configuracion funcional' : 'Consulta funcional'}</p>
        </div>
        {canAdmin && activeTab === 'organigramas' && (
          <button className="primary-button" onClick={() => openOrganigramaForm()} type="button">
            <Plus size={18} />
            Nuevo
          </button>
        )}
      </div>

      {error && <div className="error-box">{error}</div>}
      {notice && <div className="success-box">{notice}</div>}
      {!canAdmin && <div className="info-strip">Modo lectura. Solo Admin puede crear o modificar organigramas.</div>}

      <div className="tab-strip" role="tablist" aria-label="Organigrama">
        {tabs.map((item) => {
          const Icon = item.icon;
          return (
            <button key={item.key} className={activeTab === item.key ? 'active' : ''} onClick={() => setActiveTab(item.key)} type="button">
              <Icon size={17} />
              {item.label}
            </button>
          );
        })}
      </div>

      <form className="filter-row organigrama-filter-row" onSubmit={(event) => event.preventDefault()}>
        <Select label="Empresa" value={filterEmpresaId} onChange={changeEmpresaFilter} options={empresas} emptyText="Todas" />
        <Select label="Departamento" value={filterDepartamentoId} onChange={(value) => { setFilterDepartamentoId(value); loadCargos(value); }} options={departamentos} emptyText="Todos" />
        <button className="secondary-button" type="button" onClick={() => { loadOrganigramas(); loadResponsables(); loadAprobadores(); }}>
          <Search size={17} />
          Filtrar
        </button>
      </form>

      {loading && <div className="info-strip">Cargando organigrama...</div>}

      {activeTab === 'organigramas' && (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Nombre</th>
                <th>Empresa</th>
                <th>Inicio</th>
                <th>Fin</th>
                <th>Nodos</th>
                <th>Estado</th>
                <th>Acciones</th>
              </tr>
            </thead>
            <tbody>
              {organigramas.length === 0 && <tr><td colSpan={7}><div className="empty-state">Sin organigramas</div></td></tr>}
              {organigramas.map((item) => (
                <tr key={item.organigramaId}>
                  <td>{item.nombre}</td>
                  <td>{item.empresa ?? 'General'}</td>
                  <td>{formatDate(item.fechaInicio)}</td>
                  <td>{formatDate(item.fechaFin)}</td>
                  <td>{item.nodos}</td>
                  <td><span className={`badge ${item.isActive ? 'success' : 'muted'}`}>{item.isActive ? 'Activo' : 'Inactivo'}</span></td>
                  <td>
                    {canAdmin && (
                      <button className="icon-text-button" type="button" onClick={() => openOrganigramaForm(item)}>
                        <Edit3 size={16} />
                        Editar
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {activeTab === 'nodos' && (
        <div className="organigrama-section">
          <div className="toolbar-row">
            <label>
              Organigrama
              <select value={selectedOrgId} onChange={(event) => setSelectedOrgId(event.target.value)}>
                <option value="">Seleccione</option>
                {organigramas.map((item) => <option key={item.organigramaId} value={item.organigramaId}>{item.nombre}</option>)}
              </select>
            </label>
            {canAdmin && selectedOrgId && (
              <button className="primary-button" type="button" onClick={() => openNodoForm()}>
                <Plus size={18} />
                Nodo
              </button>
            )}
          </div>
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Nodo</th>
                  <th>Padre</th>
                  <th>Empresa</th>
                  <th>Departamento</th>
                  <th>Cargo</th>
                  <th>Activos</th>
                  <th>Estado</th>
                  <th>Acciones</th>
                </tr>
              </thead>
              <tbody>
                {selectedOrgNodes.length === 0 && <tr><td colSpan={8}><div className="empty-state">Sin nodos</div></td></tr>}
                {selectedOrgNodes.map((item) => (
                  <tr key={item.organigramaNodoId}>
                    <td style={{ paddingLeft: `${12 + item.nivel * 18}px` }}>{item.nombreNodo}</td>
                    <td>{item.nodoPadre ?? 'Raiz'}</td>
                    <td>{item.empresa ?? 'Todas'}</td>
                    <td>{item.departamento ?? 'Todos'}</td>
                    <td>{item.cargo ?? 'Todos'}</td>
                    <td>{item.colaboradoresActivos}</td>
                    <td><span className={`badge ${item.isActive ? 'success' : 'muted'}`}>{item.isActive ? 'Activo' : 'Inactivo'}</span></td>
                    <td>
                      {canAdmin && (
                        <button className="icon-text-button" type="button" onClick={() => openNodoForm(item)}>
                          <Edit3 size={16} />
                          Editar
                        </button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {activeTab === 'responsables' && (
        <ResponsablesTable responsables={responsables} canAdmin={canAdmin} onCreate={() => openResponsableForm()} onEdit={openResponsableForm} />
      )}

      {activeTab === 'aprobadores' && (
        <AprobadoresTable aprobadores={aprobadores} />
      )}

      {organigramaForm && (
        <div className="modal-backdrop" role="presentation">
          <section className="modal-panel user-modal" role="dialog" aria-modal="true" aria-labelledby="org-form-title">
            <ModalHeader title={organigramaForm.organigramaId ? 'Editar organigrama' : 'Nuevo organigrama'} onClose={closeModals} />
            <form className="edit-modal-form" onSubmit={saveOrganigrama}>
              <div className="edit-form-grid">
                <TextField label="Nombre" value={organigramaForm.nombre} onChange={(value) => setOrganigramaForm((current) => current ? { ...current, nombre: value } : current)} required />
                <Select label="Empresa" value={organigramaForm.empresaId} onChange={(value) => setOrganigramaForm((current) => current ? { ...current, empresaId: value } : current)} options={empresas} emptyText="General" />
                <TextField label="Inicio" type="date" value={organigramaForm.fechaInicio} onChange={(value) => setOrganigramaForm((current) => current ? { ...current, fechaInicio: value } : current)} required />
                <TextField label="Fin" type="date" value={organigramaForm.fechaFin} onChange={(value) => setOrganigramaForm((current) => current ? { ...current, fechaFin: value } : current)} />
                <Textarea label="Descripcion" value={organigramaForm.descripcion} onChange={(value) => setOrganigramaForm((current) => current ? { ...current, descripcion: value } : current)} />
                <Check label="Activo" checked={organigramaForm.isActive} onChange={(value) => setOrganigramaForm((current) => current ? { ...current, isActive: value } : current)} />
              </div>
              <ModalActions saving={saving} onCancel={closeModals} />
            </form>
          </section>
        </div>
      )}

      {nodoForm && (
        <div className="modal-backdrop" role="presentation">
          <section className="modal-panel user-modal" role="dialog" aria-modal="true" aria-labelledby="node-form-title">
            <ModalHeader title={nodoForm.organigramaNodoId ? 'Editar nodo' : 'Nuevo nodo'} onClose={closeModals} />
            <form className="edit-modal-form" onSubmit={saveNodo}>
              <div className="edit-form-grid">
                <TextField label="Nombre nodo" value={nodoForm.nombreNodo} onChange={(value) => setNodoForm((current) => current ? { ...current, nombreNodo: value } : current)} required />
                <Select label="Empresa" value={nodoForm.empresaId} onChange={(value) => { setNodoForm((current) => current ? { ...current, empresaId: value, departamentoId: '', cargoId: '' } : current); loadDepartamentos(value); setCargos([]); }} options={empresas} emptyText="Todas" />
                <Select label="Departamento" value={nodoForm.departamentoId} onChange={(value) => { setNodoForm((current) => current ? { ...current, departamentoId: value, cargoId: '' } : current); loadCargos(value); }} options={departamentos} emptyText="Todos" />
                <SelectCargo label="Cargo" value={nodoForm.cargoId} onChange={(value) => setNodoForm((current) => current ? { ...current, cargoId: value } : current)} options={cargos} emptyText="Todos" />
                <SelectNode label="Nodo padre" value={nodoForm.nodoPadreId} onChange={(value) => setNodoForm((current) => current ? { ...current, nodoPadreId: value } : current)} nodes={selectedOrgNodes.filter((item) => item.organigramaNodoId !== nodoForm.organigramaNodoId)} />
                <TextField label="Orden" type="number" value={nodoForm.orden} onChange={(value) => setNodoForm((current) => current ? { ...current, orden: value } : current)} />
                <Textarea label="Descripcion" value={nodoForm.descripcion} onChange={(value) => setNodoForm((current) => current ? { ...current, descripcion: value } : current)} />
                <Check label="Rol operativo" checked={nodoForm.esRolOperativo} onChange={(value) => setNodoForm((current) => current ? { ...current, esRolOperativo: value } : current)} />
                <Check label="Activo" checked={nodoForm.isActive} onChange={(value) => setNodoForm((current) => current ? { ...current, isActive: value } : current)} />
              </div>
              <ModalActions saving={saving} onCancel={closeModals} />
            </form>
          </section>
        </div>
      )}

      {responsableForm && (
        <div className="modal-backdrop" role="presentation">
          <section className="modal-panel solicitud-modal" role="dialog" aria-modal="true" aria-labelledby="responsable-form-title">
            <ModalHeader title={responsableForm.departamentoResponsableId ? 'Editar responsable' : 'Nuevo responsable'} onClose={closeModals} />
            <form className="edit-modal-form" onSubmit={saveResponsable}>
              <div className="edit-form-grid">
                <Select label="Empresa" value={responsableForm.empresaId} onChange={(value) => { setResponsableForm((current) => current ? { ...current, empresaId: value, departamentoId: '', colaboradorResponsableId: '' } : current); loadDepartamentos(value); }} options={empresas} emptyText="Seleccione" required />
                <Select label="Departamento" value={responsableForm.departamentoId} onChange={(value) => setResponsableForm((current) => current ? { ...current, departamentoId: value, colaboradorResponsableId: '' } : current)} options={departamentos} emptyText="Seleccione" required />
                <TypeSelect value={responsableForm.tipoResponsable} onChange={(value) => setResponsableForm((current) => current ? { ...current, tipoResponsable: value } : current)} />
                <label>
                  Buscar colaborador
                  <input value={colaboradorSearch} onChange={(event) => setColaboradorSearch(event.target.value)} placeholder="Nombre o no. empleado" />
                </label>
                <ColaboradorSelect value={responsableForm.colaboradorResponsableId} options={colaboradores} onChange={(value) => setResponsableForm((current) => current ? { ...current, colaboradorResponsableId: value } : current)} />
                <UserSelect value={responsableForm.usuarioResponsableId} options={usuarios.filter((item) => item.isActive && item.rol !== 'Consulta')} onChange={(value) => setResponsableForm((current) => current ? { ...current, usuarioResponsableId: value } : current)} />
                <TextField label="Inicio" type="date" value={responsableForm.fechaInicio} onChange={(value) => setResponsableForm((current) => current ? { ...current, fechaInicio: value } : current)} required />
                <TextField label="Fin" type="date" value={responsableForm.fechaFin} onChange={(value) => setResponsableForm((current) => current ? { ...current, fechaFin: value } : current)} />
                <Textarea label="Observacion" value={responsableForm.observacion} onChange={(value) => setResponsableForm((current) => current ? { ...current, observacion: value } : current)} />
                <Check label="Principal" checked={responsableForm.esPrincipal} onChange={(value) => setResponsableForm((current) => current ? { ...current, esPrincipal: value } : current)} />
                <Check label="Puede aprobar solicitudes" checked={responsableForm.puedeAprobarSolicitudes} onChange={(value) => setResponsableForm((current) => current ? { ...current, puedeAprobarSolicitudes: value } : current)} />
                <Check label="Activo" checked={responsableForm.isActive} onChange={(value) => setResponsableForm((current) => current ? { ...current, isActive: value } : current)} />
              </div>
              <ModalActions saving={saving} onCancel={closeModals} />
            </form>
          </section>
        </div>
      )}
    </section>
  );
}

function ResponsablesTable({
  responsables,
  canAdmin,
  onCreate,
  onEdit
}: {
  responsables: DepartamentoResponsable[];
  canAdmin: boolean;
  onCreate: () => void;
  onEdit: (item: DepartamentoResponsable) => void;
}) {
  return (
    <div className="organigrama-section">
      <div className="toolbar-row">
        <strong>{responsables.length} responsables</strong>
        {canAdmin && (
          <button className="primary-button" type="button" onClick={onCreate}>
            <Plus size={18} />
            Responsable
          </button>
        )}
      </div>
      <div className="table-wrap">
        <table>
          <thead>
            <tr>
              <th>Departamento</th>
              <th>Responsable</th>
              <th>Usuario</th>
              <th>Tipo</th>
              <th>Aprobador</th>
              <th>Estado</th>
              <th>Advertencias</th>
              <th>Acciones</th>
            </tr>
          </thead>
          <tbody>
            {responsables.length === 0 && <tr><td colSpan={8}><div className="empty-state">Sin responsables</div></td></tr>}
            {responsables.map((item) => (
              <tr key={item.departamentoResponsableId}>
                <td>{item.empresa} / {item.departamento}</td>
                <td>{item.colaboradorResponsable}<br /><small>{item.noEmpleado}</small></td>
                <td>{item.usuarioResponsable ?? 'Sin usuario'}</td>
                <td>{item.tipoResponsable}{item.esPrincipal ? ' / Principal' : ''}</td>
                <td>{item.puedeAprobarSolicitudes ? 'Si' : 'No'}</td>
                <td><span className={`badge ${item.isActive ? 'success' : 'muted'}`}>{item.isActive ? 'Activo' : 'Inactivo'}</span></td>
                <td>{item.advertencias.length ? item.advertencias.join(' | ') : 'N/D'}</td>
                <td>
                  {canAdmin && (
                    <button className="icon-text-button" type="button" onClick={() => onEdit(item)}>
                      <Edit3 size={16} />
                      Editar
                    </button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

function AprobadoresTable({ aprobadores }: { aprobadores: AprobadorSolicitud[] }) {
  return (
    <div className="table-wrap">
      <table>
        <thead>
          <tr>
            <th>Departamento</th>
            <th>Aprobador</th>
            <th>Cargo</th>
            <th>Tipo</th>
            <th>Usuario</th>
            <th>Principal</th>
            <th>Advertencias</th>
          </tr>
        </thead>
        <tbody>
          {aprobadores.length === 0 && <tr><td colSpan={7}><div className="empty-state">Sin aprobadores configurados</div></td></tr>}
          {aprobadores.map((item) => (
            <tr key={item.departamentoResponsableId}>
              <td>{item.empresa} / {item.departamento}</td>
              <td>{item.nombreCompleto}<br /><small>{item.noEmpleado}</small></td>
              <td>{item.cargo}</td>
              <td>{item.tipoResponsable}</td>
              <td>{item.usuarioResponsable ?? 'Sin usuario'}</td>
              <td>{item.esPrincipal ? 'Si' : 'No'}</td>
              <td>{item.advertencias.length ? item.advertencias.join(' | ') : 'N/D'}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function ModalHeader({ title, onClose }: { title: string; onClose: () => void }) {
  return (
    <div className="modal-header">
      <div>
        <h2>{title}</h2>
        <p>Organigrama funcional</p>
      </div>
      <button className="icon-button light" onClick={onClose} type="button" title="Cerrar" aria-label="Cerrar">
        <X size={18} />
      </button>
    </div>
  );
}

function ModalActions({ saving, onCancel }: { saving: boolean; onCancel: () => void }) {
  return (
    <div className="modal-actions">
      <button className="secondary-button" type="button" onClick={onCancel}>Cancelar</button>
      <button className="primary-button" disabled={saving} type="submit">
        <Save size={18} />
        {saving ? 'Guardando...' : 'Guardar'}
      </button>
    </div>
  );
}

function Select({
  label,
  value,
  onChange,
  options,
  emptyText,
  required
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  options: CatalogoItem[];
  emptyText: string;
  required?: boolean;
}) {
  return (
    <label>
      {label}
      <select value={value} onChange={(event) => onChange(event.target.value)} required={required}>
        <option value="">{emptyText}</option>
        {options.map((item) => <option key={item.id} value={item.id}>{item.nombre}</option>)}
      </select>
    </label>
  );
}

function SelectCargo({ label, value, onChange, options, emptyText }: { label: string; value: string; onChange: (value: string) => void; options: CatalogoItem[]; emptyText: string }) {
  return (
    <label>
      {label}
      <select value={value} onChange={(event) => onChange(event.target.value)}>
        <option value="">{emptyText}</option>
        {options.map((item) => <option key={item.id} value={item.id}>{item.nombre}</option>)}
      </select>
    </label>
  );
}

function SelectNode({ label, value, onChange, nodes }: { label: string; value: string; onChange: (value: string) => void; nodes: OrganigramaNodo[] }) {
  return (
    <label>
      {label}
      <select value={value} onChange={(event) => onChange(event.target.value)}>
        <option value="">Raiz</option>
        {nodes.map((item) => <option key={item.organigramaNodoId} value={item.organigramaNodoId}>{item.nombreNodo}</option>)}
      </select>
    </label>
  );
}

function TypeSelect({ value, onChange }: { value: string; onChange: (value: string) => void }) {
  return (
    <label>
      Tipo responsable
      <select value={value} onChange={(event) => onChange(event.target.value)}>
        {responsibleTypes.map((item) => <option key={item} value={item}>{item}</option>)}
      </select>
    </label>
  );
}

function ColaboradorSelect({ value, onChange, options }: { value: string; onChange: (value: string) => void; options: ColaboradorLookup[] }) {
  return (
    <label>
      Colaborador responsable
      <select value={value} onChange={(event) => onChange(event.target.value)} required>
        <option value="">Seleccione</option>
        {options.map((item) => (
          <option key={item.colaboradorId} value={item.colaboradorId}>
            {item.nombreCompleto} - {item.cargo} - {item.departamento}
          </option>
        ))}
      </select>
    </label>
  );
}

function UserSelect({ value, onChange, options }: { value: string; onChange: (value: string) => void; options: Usuario[] }) {
  return (
    <label>
      Usuario asociado
      <select value={value} onChange={(event) => onChange(event.target.value)}>
        <option value="">Sin usuario</option>
        {options.map((item) => <option key={item.usuarioId} value={item.usuarioId}>{item.nombreUsuario} - {item.rol}</option>)}
      </select>
    </label>
  );
}

function TextField({ label, value, onChange, type = 'text', required = false }: { label: string; value: string; onChange: (value: string) => void; type?: string; required?: boolean }) {
  return (
    <label>
      {label}
      <input type={type} value={value} onChange={(event) => onChange(event.target.value)} required={required} />
    </label>
  );
}

function Textarea({ label, value, onChange }: { label: string; value: string; onChange: (value: string) => void }) {
  return (
    <label className="span-2">
      {label}
      <textarea value={value} onChange={(event) => onChange(event.target.value)} />
    </label>
  );
}

function Check({ label, checked, onChange }: { label: string; checked: boolean; onChange: (value: boolean) => void }) {
  return (
    <label className="check-label compact-check">
      <input type="checkbox" checked={checked} onChange={(event) => onChange(event.target.checked)} />
      {label}
    </label>
  );
}

function emptyOrganigramaForm(empresaId: string): OrganigramaForm {
  return {
    nombre: '',
    empresaId,
    descripcion: '',
    fechaInicio: new Date().toISOString().slice(0, 10),
    fechaFin: '',
    isActive: true
  };
}

function emptyNodoForm(organigramaId: string, empresaId: string): NodoForm {
  return {
    organigramaId,
    nombreNodo: '',
    empresaId,
    departamentoId: '',
    cargoId: '',
    nodoPadreId: '',
    descripcion: '',
    nivel: '0',
    orden: '0',
    esRolOperativo: true,
    isActive: true
  };
}

function emptyResponsableForm(empresaId: string, departamentoId: string): ResponsableForm {
  return {
    empresaId,
    departamentoId,
    colaboradorResponsableId: '',
    usuarioResponsableId: '',
    tipoResponsable: 'LiderPrincipal',
    esPrincipal: true,
    puedeAprobarSolicitudes: true,
    fechaInicio: new Date().toISOString().slice(0, 10),
    fechaFin: '',
    observacion: '',
    isActive: true
  };
}

function optionalText(value: string) {
  const trimmed = value.trim();
  return trimmed ? trimmed : null;
}

function optionalDate(value: string) {
  return value || null;
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
