import { FormEvent, useEffect, useState } from 'react';
import { RefreshCw, Save, Trash2 } from 'lucide-react';
import { apiGet, apiPost } from '../api/client';
import { useAuth } from '../auth/AuthContext';
import type { Cargo, Departamento, Empresa, QaCleanupRequest, QaCleanupResult, QaInventory, QaInventoryItem } from '../types/api';

export function ConfiguracionPage() {
  const { user } = useAuth();
  const canAdmin = user?.rol === 'Admin';
  const [empresas, setEmpresas] = useState<Empresa[]>([]);
  const [departamentos, setDepartamentos] = useState<Departamento[]>([]);
  const [cargos, setCargos] = useState<Cargo[]>([]);
  const [empresaNombre, setEmpresaNombre] = useState('');
  const [empresaRuc, setEmpresaRuc] = useState('');
  const [departamentoNombre, setDepartamentoNombre] = useState('');
  const [departamentoEmpresaId, setDepartamentoEmpresaId] = useState('');
  const [cargoNombre, setCargoNombre] = useState('');
  const [cargoDepartamentoId, setCargoDepartamentoId] = useState('');
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [qaInventory, setQaInventory] = useState<QaInventory | null>(null);
  const [qaSelected, setQaSelected] = useState<Record<string, boolean>>({});
  const [qaFilter, setQaFilter] = useState('todos');
  const [qaBusy, setQaBusy] = useState(false);

  const load = () => {
    Promise.all([apiGet<Empresa[]>('/empresas'), apiGet<Departamento[]>('/departamentos'), apiGet<Cargo[]>('/cargos')])
      .then(([companies, departments, positions]) => {
        setEmpresas(companies);
        setDepartamentos(departments);
        setCargos(positions);
        setDepartamentoEmpresaId((current) => current || String(companies[0]?.empresaId ?? ''));
        setCargoDepartamentoId((current) => current || String(departments[0]?.departamentoId ?? ''));
      })
      .catch((err) => setError(err instanceof Error ? err.message : 'No se pudo cargar configuracion.'));
  };

  useEffect(load, []);

  async function createEmpresa(event: FormEvent) {
    event.preventDefault();
    await submit(() => apiPost('/empresas', { nombre: empresaNombre, ruc: empresaRuc || null }), () => {
      setEmpresaNombre('');
      setEmpresaRuc('');
    });
  }

  async function createDepartamento(event: FormEvent) {
    event.preventDefault();
    await submit(() => apiPost('/departamentos', { nombre: departamentoNombre, empresaId: Number(departamentoEmpresaId) }), () => setDepartamentoNombre(''));
  }

  async function createCargo(event: FormEvent) {
    event.preventDefault();
    await submit(() => apiPost('/cargos', { nombre: cargoNombre, departamentoId: Number(cargoDepartamentoId) }), () => setCargoNombre(''));
  }

  async function submit(call: () => Promise<unknown>, reset: () => void) {
    setError('');
    setNotice('');
    try {
      await call();
      reset();
      load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'No se pudo guardar.');
    }
  }

  async function loadQaInventory() {
    setQaBusy(true);
    setError('');
    setNotice('');
    try {
      const data = await apiGet<QaInventory>('/admin/qa/inventario');
      setQaInventory(data);
      setQaSelected({});
    } catch (err) {
      setError(err instanceof Error ? err.message : 'No se pudo cargar inventario QA.');
    } finally {
      setQaBusy(false);
    }
  }

  async function cleanQa() {
    if (!qaInventory) return;
    const selected = qaItems(qaInventory).filter((item) => qaSelected[qaKey(item)] && item.puedeBorrarseSeguro);
    if (selected.length === 0) {
      setError('Seleccione al menos un registro QA seguro para borrar.');
      return;
    }

    const confirmed = window.confirm('El borrado de datos QA no se puede deshacer. No se borraran colaboradores ni datos maestros. Desea continuar?');
    if (!confirmed) {
      return;
    }

    const payload: QaCleanupRequest = {
      confirmar: true,
      solicitudIds: selected.filter((item) => item.tipoEntidad === 'Solicitud').map((item) => item.id),
      organigramaIds: selected.filter((item) => item.tipoEntidad === 'Organigrama').map((item) => item.id),
      nodoIds: selected.filter((item) => item.tipoEntidad === 'OrganigramaNodo').map((item) => item.id),
      responsableIds: selected.filter((item) => item.tipoEntidad === 'DepartamentoResponsable').map((item) => item.id)
    };

    setQaBusy(true);
    setError('');
    setNotice('');
    try {
      const result = await apiPost<QaCleanupResult>('/admin/qa/limpiar', payload);
      const warnings = result.advertencias.length > 0 ? ` Advertencias: ${result.advertencias.join(' ')}` : '';
      setNotice(`Limpieza QA ejecutada. Solicitudes: ${result.solicitudesBorradas}, nodos: ${result.nodosBorrados}, responsables: ${result.responsablesBorrados}.${warnings}`);
      await loadQaInventory();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'No se pudo ejecutar limpieza QA.');
    } finally {
      setQaBusy(false);
    }
  }

  return (
    <section className="page">
      <div className="page-heading">
        <div>
          <h1>Configuracion</h1>
          <p>Catalogos V1</p>
        </div>
      </div>
      {error && <div className="error-box">{error}</div>}
      {notice && <div className="success-box">{notice}</div>}
      <div className="config-grid">
        <section className="panel">
          <h2>Empresas</h2>
          <form className="mini-form" onSubmit={createEmpresa}>
            <input value={empresaNombre} onChange={(event) => setEmpresaNombre(event.target.value)} placeholder="Nombre" />
            <input value={empresaRuc} onChange={(event) => setEmpresaRuc(event.target.value)} placeholder="RUC" />
            <button className="primary-button"><Save size={18} />Guardar</button>
          </form>
          <SimpleList items={empresas.map((item) => ({ key: item.empresaId, label: `${item.nombre}${item.ruc ? ` · ${item.ruc}` : ''}` }))} />
        </section>
        <section className="panel">
          <h2>Departamentos</h2>
          <form className="mini-form" onSubmit={createDepartamento}>
            <input value={departamentoNombre} onChange={(event) => setDepartamentoNombre(event.target.value)} placeholder="Nombre" />
            <select value={departamentoEmpresaId} onChange={(event) => setDepartamentoEmpresaId(event.target.value)}>
              {empresas.map((item) => <option key={item.empresaId} value={item.empresaId}>{item.nombre}</option>)}
            </select>
            <button className="primary-button"><Save size={18} />Guardar</button>
          </form>
          <SimpleList items={departamentos.map((item) => ({ key: item.departamentoId, label: `${item.nombre} · ${item.empresa}` }))} />
        </section>
        <section className="panel">
          <h2>Cargos</h2>
          <form className="mini-form" onSubmit={createCargo}>
            <input value={cargoNombre} onChange={(event) => setCargoNombre(event.target.value)} placeholder="Nombre" />
            <select value={cargoDepartamentoId} onChange={(event) => setCargoDepartamentoId(event.target.value)}>
              {departamentos.map((item) => <option key={item.departamentoId} value={item.departamentoId}>{item.nombre}</option>)}
            </select>
            <button className="primary-button"><Save size={18} />Guardar</button>
          </form>
          <SimpleList items={cargos.map((item) => ({ key: item.cargoId, label: `${item.nombre} · ${item.departamento}` }))} />
        </section>
      </div>
      {canAdmin && (
        <section className="panel qa-cleanup-panel">
          <div className="panel-title-row">
            <div>
              <h2>Limpieza QA</h2>
              <p className="muted-text">Inventario seguro de datos de prueba</p>
            </div>
            <div className="table-actions-stack">
              <button className="secondary-button" type="button" onClick={loadQaInventory} disabled={qaBusy}>
                <RefreshCw size={18} />
                Consultar inventario
              </button>
              <button className="primary-button danger-action" type="button" onClick={cleanQa} disabled={qaBusy || !qaInventory}>
                <Trash2 size={18} />
                Limpiar seleccionados
              </button>
            </div>
          </div>
          <div className="info-strip warning">
            Los organigramas activos o posiblemente reales estan protegidos y no pueden eliminarse desde limpieza QA.
          </div>
          {!qaInventory && <div className="info-strip">Ejecute el inventario antes de seleccionar registros QA.</div>}
          {qaInventory && (
            <>
              <div className="alert-context">
                <span><strong>Total detectado</strong>{qaInventory.totalDetectado}</span>
                <span><strong>Seguros para borrar</strong>{qaItems(qaInventory).filter(isQaSelectable).length}</span>
                <span><strong>Protegidos</strong>{qaItems(qaInventory).filter((item) => item.esProtegido).length}</span>
                <span><strong>Generado</strong>{new Date(qaInventory.generadoEn).toLocaleString()}</span>
                <span><strong>Modo</strong>Solo registros seguros seleccionados</span>
              </div>
              <div className="qa-filter-row">
                <label>
                  Vista
                  <select value={qaFilter} onChange={(event) => setQaFilter(event.target.value)}>
                    <option value="todos">Todos</option>
                    <option value="seguros">Seguros para borrar</option>
                    <option value="riesgo">Riesgo medio/alto</option>
                    <option value="protegidos">Protegidos</option>
                  </select>
                </label>
              </div>
              <div className="table-wrap qa-table-wrap">
                <table>
                  <thead>
                    <tr>
                      <th>Seleccionar</th>
                      <th>Entidad</th>
                      <th>Codigo / Nombre</th>
                      <th>Estado</th>
                      <th>Motivo</th>
                      <th>Riesgo</th>
                      <th>Proteccion</th>
                    </tr>
                  </thead>
                  <tbody>
                    {filteredQaItems(qaInventory, qaFilter).length === 0 && <tr><td colSpan={7}><div className="empty-state">Sin datos QA detectados para este filtro</div></td></tr>}
                    {filteredQaItems(qaInventory, qaFilter).map((item) => {
                      const key = qaKey(item);
                      const selectable = isQaSelectable(item);
                      return (
                        <tr key={key} className={item.esProtegido ? 'protected-row' : undefined}>
                          <td>
                            <input
                              type="checkbox"
                              checked={Boolean(qaSelected[key])}
                              disabled={!selectable}
                              onChange={(event) => setQaSelected((current) => ({ ...current, [key]: event.target.checked }))}
                            />
                          </td>
                          <td>{item.tipoEntidad}</td>
                          <td>{item.codigo ?? item.nombre ?? item.id}</td>
                          <td>{item.estado ?? (item.isActive ? 'Activo' : 'Inactivo')}</td>
                          <td>{item.motivoDeteccion}</td>
                          <td><span className={`badge ${riskBadgeClass(item)}`}>{item.riesgo}</span></td>
                          <td className="qa-protection-cell">
                            {item.esProtegido ? <span className="badge danger">Protegido</span> : item.puedeBorrarseSeguro ? <span className="badge success">Seguro</span> : <span className="badge warning">Revisar</span>}
                            {item.motivoProteccion && <span className="qa-protection-reason">{item.motivoProteccion}</span>}
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            </>
          )}
        </section>
      )}
    </section>
  );
}

function SimpleList({ items }: { items: Array<{ key: string | number; label: string }> }) {
  return (
    <div className="simple-list">
      {items.length === 0 && <div className="empty-state">Sin registros</div>}
      {items.map((item) => <span key={item.key}>{item.label}</span>)}
    </div>
  );
}

function qaItems(inventory: QaInventory): QaInventoryItem[] {
  return [
    ...inventory.solicitudes,
    ...inventory.requisiciones,
    ...inventory.accionesPersonal,
    ...inventory.organigramas,
    ...inventory.nodos,
    ...inventory.responsables,
    ...inventory.alertasRelacionadas
  ];
}

function qaKey(item: QaInventoryItem) {
  return `${item.tipoEntidad}-${item.id}`;
}

function isQaSelectable(item: QaInventoryItem) {
  return item.puedeBorrarseSeguro &&
    !item.esProtegido &&
    ['Solicitud', 'Organigrama', 'OrganigramaNodo', 'DepartamentoResponsable'].includes(item.tipoEntidad);
}

function filteredQaItems(inventory: QaInventory, filter: string) {
  const items = qaItems(inventory);
  if (filter === 'seguros') {
    return items.filter(isQaSelectable);
  }
  if (filter === 'riesgo') {
    return items.filter((item) => !item.puedeBorrarseSeguro && !item.esProtegido);
  }
  if (filter === 'protegidos') {
    return items.filter((item) => item.esProtegido);
  }

  return items;
}

function riskBadgeClass(item: QaInventoryItem) {
  if (item.esProtegido) return 'danger';
  if (item.puedeBorrarseSeguro) return 'success';
  return 'warning';
}
