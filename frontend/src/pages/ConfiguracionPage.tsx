import { FormEvent, useEffect, useState } from 'react';
import { Save } from 'lucide-react';
import { apiGet, apiPost } from '../api/client';
import type { Cargo, Departamento, Empresa } from '../types/api';

export function ConfiguracionPage() {
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
    try {
      await call();
      reset();
      load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'No se pudo guardar.');
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
      <div className="config-grid">
        <section className="panel">
          <h2>Empresas</h2>
          <form className="mini-form" onSubmit={createEmpresa}>
            <input value={empresaNombre} onChange={(event) => setEmpresaNombre(event.target.value)} placeholder="Nombre" />
            <input value={empresaRuc} onChange={(event) => setEmpresaRuc(event.target.value)} placeholder="RUC" />
            <button className="primary-button"><Save size={18} />Guardar</button>
          </form>
          <SimpleList items={empresas.map((item) => `${item.nombre}${item.ruc ? ` · ${item.ruc}` : ''}`)} />
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
          <SimpleList items={departamentos.map((item) => `${item.nombre} · ${item.empresa}`)} />
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
          <SimpleList items={cargos.map((item) => `${item.nombre} · ${item.departamento}`)} />
        </section>
      </div>
    </section>
  );
}

function SimpleList({ items }: { items: string[] }) {
  return (
    <div className="simple-list">
      {items.length === 0 && <div className="empty-state">Sin registros</div>}
      {items.map((item) => <span key={item}>{item}</span>)}
    </div>
  );
}
