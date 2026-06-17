import { useEffect, useMemo, useState } from 'react';
import type { ReactNode } from 'react';
import { AlertTriangle, BriefcaseBusiness, Building2, CalendarClock, FileClock, UserCheck, Users } from 'lucide-react';
import { Link } from 'react-router-dom';
import { apiGet } from '../api/client';
import type {
  Alerta,
  AltasBajas,
  CatalogoItem,
  ChartItem,
  ColaboradorList,
  DashboardResumen,
  Movimiento,
  RecordatorioDocumento
} from '../types/api';
import { formatDate } from '../utils/format';

const chartColors = ['#6D28D9', '#8B5CF6', '#A78BFA', '#10B981', '#F59E0B', '#0EA5E9', '#EF4444'];
const tipoVencimientoOptions = [
  { value: '', label: 'Todos' },
  { value: 'Cedula', label: 'Cedula' },
  { value: 'Licencia', label: 'Licencia' },
  { value: 'Contrato', label: 'Contrato' },
  { value: 'PeriodoProbatorio', label: 'Periodo probatorio' },
  { value: 'Documento', label: 'Documento' }
];
const months = [
  { value: '', label: 'Todo el año' },
  { value: '1', label: 'Enero' },
  { value: '2', label: 'Febrero' },
  { value: '3', label: 'Marzo' },
  { value: '4', label: 'Abril' },
  { value: '5', label: 'Mayo' },
  { value: '6', label: 'Junio' },
  { value: '7', label: 'Julio' },
  { value: '8', label: 'Agosto' },
  { value: '9', label: 'Septiembre' },
  { value: '10', label: 'Octubre' },
  { value: '11', label: 'Noviembre' },
  { value: '12', label: 'Diciembre' }
];

export function DashboardPage() {
  const currentYear = new Date().getFullYear();
  const [resumen, setResumen] = useState<DashboardResumen | null>(null);
  const [empresasCatalogo, setEmpresasCatalogo] = useState<CatalogoItem[]>([]);
  const [estatusCatalogo, setEstatusCatalogo] = useState<CatalogoItem[]>([]);
  const [empresaId, setEmpresaId] = useState('');
  const [estatusId, setEstatusId] = useState('');
  const [year, setYear] = useState(String(currentYear));
  const [month, setMonth] = useState('');
  const [tipoVencimiento, setTipoVencimiento] = useState('');
  const [empresas, setEmpresas] = useState<ChartItem[]>([]);
  const [tiposContrato, setTiposContrato] = useState<ChartItem[]>([]);
  const [departamentos, setDepartamentos] = useState<ChartItem[]>([]);
  const [altasBajas, setAltasBajas] = useState<AltasBajas[]>([]);
  const [altasDetalle, setAltasDetalle] = useState<ColaboradorList[]>([]);
  const [bajasDetalle, setBajasDetalle] = useState<ColaboradorList[]>([]);
  const [vencimientos, setVencimientos] = useState<Alerta[]>([]);
  const [recordatorios, setRecordatorios] = useState<RecordatorioDocumento[]>([]);
  const [movimientos, setMovimientos] = useState<Movimiento[]>([]);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    Promise.all([
      apiGet<CatalogoItem[]>('/catalogos/empresas'),
      apiGet<CatalogoItem[]>('/catalogos/estatus-colaborador')
    ])
      .then(([companies, statuses]) => {
        setEmpresasCatalogo(companies);
        setEstatusCatalogo(statuses);
      })
      .catch((err) => setError(err instanceof Error ? err.message : 'No se pudieron cargar filtros.'));
  }, []);

  useEffect(() => {
    const baseParams = new URLSearchParams();
    if (empresaId) baseParams.set('empresaId', empresaId);
    if (estatusId) baseParams.set('estatusId', estatusId);

    const periodParams = new URLSearchParams(baseParams);
    periodParams.set('year', year);
    if (month) periodParams.set('month', month);

    const vencimientoParams = new URLSearchParams(baseParams);
    if (tipoVencimiento) vencimientoParams.set('tipoAlerta', tipoVencimiento);

    setLoading(true);
    setError('');
    Promise.all([
      apiGet<DashboardResumen>(withQuery('/dashboard/resumen', baseParams)),
      apiGet<ChartItem[]>(withQuery('/dashboard/colaboradores-por-empresa', baseParams)),
      apiGet<ChartItem[]>(withQuery('/dashboard/colaboradores-por-tipo-contrato', baseParams)),
      apiGet<ChartItem[]>(withQuery('/dashboard/colaboradores-por-departamento', baseParams)),
      apiGet<AltasBajas[]>(withQuery('/dashboard/altas-bajas', periodParams)),
      apiGet<Alerta[]>(withQuery('/dashboard/vencimientos', vencimientoParams)),
      apiGet<Movimiento[]>(withQuery('/dashboard/ultimos-movimientos', baseParams)),
      apiGet<RecordatorioDocumento[]>(withQuery('/dashboard/recordatorios-documentos', vencimientoParams)),
      month ? apiGet<ColaboradorList[]>(withQuery('/dashboard/altas-detalle', periodParams)) : Promise.resolve([]),
      month ? apiGet<ColaboradorList[]>(withQuery('/dashboard/bajas-detalle', periodParams)) : Promise.resolve([])
    ])
      .then(([summary, byCompany, byContract, byDepartment, hires, expirations, moves, reminders, hiresDetail, exitsDetail]) => {
        setResumen(summary);
        setEmpresas(byCompany);
        setTiposContrato(byContract);
        setDepartamentos(byDepartment);
        setAltasBajas(hires);
        setVencimientos(expirations);
        setMovimientos(moves);
        setRecordatorios(reminders);
        setAltasDetalle(hiresDetail);
        setBajasDetalle(exitsDetail);
      })
      .catch((err) => setError(err instanceof Error ? err.message : 'No se pudo cargar el dashboard.'))
      .finally(() => setLoading(false));
  }, [empresaId, estatusId, month, tipoVencimiento, year]);

  const maxDepartamento = useMemo(() => Math.max(1, ...departamentos.map((item) => item.value)), [departamentos]);
  const maxAltasBajas = useMemo(() => Math.max(1, ...altasBajas.map((item) => Math.max(item.altas, item.bajas))), [altasBajas]);
  const totalAltas = useMemo(() => altasBajas.reduce((sum, item) => sum + item.altas, 0), [altasBajas]);
  const totalBajas = useMemo(() => altasBajas.reduce((sum, item) => sum + item.bajas, 0), [altasBajas]);
  const years = useMemo(() => Array.from({ length: 6 }, (_, index) => String(currentYear - index)), [currentYear]);

  return (
    <section className="page">
      <div className="page-heading">
        <div>
          <h1>Dashboard</h1>
          <p>Panel ejecutivo operativo</p>
        </div>
      </div>
      {error && <div className="error-box">{error}</div>}

      <div className="metric-grid">
        <Metric to="/colaboradores" icon={<Users size={22} />} label="Total" value={resumen?.totalColaboradores ?? 0} />
        <Metric to="/colaboradores" icon={<UserCheck size={22} />} label="Activos" value={resumen?.activos ?? 0} />
        <Metric to="/colaboradores" icon={<BriefcaseBusiness size={22} />} label="Cesantes" value={resumen?.cesantes ?? 0} />
        <Metric to="/alertas" icon={<AlertTriangle size={22} />} label="Alertas" value={resumen?.alertasActivas ?? 0} />
        <Metric to="/alertas" icon={<CalendarClock size={22} />} label="Vencimientos" value={resumen?.vencimientos ?? 0} />
      </div>

      <form className="dashboard-filters">
        <label>
          Empresa
          <select value={empresaId} onChange={(event) => setEmpresaId(event.target.value)}>
            <option value="">Todas</option>
            {empresasCatalogo.map((item) => <option key={item.id} value={item.id}>{item.nombre}</option>)}
          </select>
        </label>
        <label>
          Estatus
          <select value={estatusId} onChange={(event) => setEstatusId(event.target.value)}>
            <option value="">Todos los estatus</option>
            {estatusCatalogo.map((item) => (
              <option key={item.id} value={item.id}>{item.nombre}{item.codigo ? ` (${item.codigo})` : ''}</option>
            ))}
          </select>
        </label>
        <label>
          Año
          <select value={year} onChange={(event) => setYear(event.target.value)}>
            {years.map((item) => <option key={item} value={item}>{item}</option>)}
          </select>
        </label>
        <label>
          Mes
          <select value={month} onChange={(event) => setMonth(event.target.value)}>
            {months.map((item) => <option key={item.value || 'all'} value={item.value}>{item.label}</option>)}
          </select>
        </label>
      </form>

      {loading && <div className="info-strip">Actualizando dashboard...</div>}

      <div className="dashboard-grid executive-grid">
        <section className="panel altas-bajas-panel">
          <div className="panel-title-row">
            <h2>Colaboradores por empresa</h2>
            <Building2 size={18} aria-hidden />
          </div>
          <DonutChart items={empresas} />
        </section>

        <section className="panel">
          <div className="panel-title-row">
            <h2>Tipo de contrato</h2>
            <BriefcaseBusiness size={18} aria-hidden />
          </div>
          <DonutChart items={tiposContrato} />
        </section>

        <section className="panel fixed-panel">
          <h2>Colaboradores por departamento</h2>
          <div className="bar-list scroll-list">
            {departamentos.length === 0 && <EmptyState />}
            {departamentos.map((item) => (
              <div key={item.label} className="bar-row">
                <span title={item.label}>{item.label}</span>
                <div className="bar-track"><i style={{ width: `${(item.value / maxDepartamento) * 100}%` }} /></div>
                <strong>{item.value}</strong>
              </div>
            ))}
          </div>
        </section>

        <section className="panel">
          <div className="panel-title-row">
            <h2>Altas y bajas</h2>
            <div className="tiny-totals">
              <span>Altas <strong>{totalAltas}</strong></span>
              <span>Bajas <strong>{totalBajas}</strong></span>
            </div>
          </div>
          <div className="mini-chart">
            {altasBajas.length === 0 && <EmptyState />}
            {altasBajas.map((item) => (
              <div key={item.periodo} className="month-pair">
                <span>{item.periodo.slice(5)}</span>
                <i className="alta" title={`Altas ${item.altas}`} style={{ height: `${Math.max(4, (item.altas / maxAltasBajas) * 90)}px` }} />
                <i className="baja" title={`Bajas ${item.bajas}`} style={{ height: `${Math.max(4, (item.bajas / maxAltasBajas) * 90)}px` }} />
              </div>
            ))}
          </div>
          {month && (
            <div className="month-detail-grid month-detail-scroll">
              <DetalleMes title="Altas del mes" items={altasDetalle} tipo="Alta" dateField="fechaIngreso" />
              <DetalleMes title="Bajas del mes" items={bajasDetalle} tipo="Baja" dateField="fechaSalida" />
            </div>
          )}
        </section>

        <section className="panel fixed-panel with-filter">
          <h2>Vencimientos</h2>
          <label className="panel-filter">
            Tipo de vencimiento
            <select value={tipoVencimiento} onChange={(event) => setTipoVencimiento(event.target.value)}>
              {tipoVencimientoOptions.map((item) => <option key={item.value || 'all'} value={item.value}>{item.label}</option>)}
            </select>
          </label>
          <div className="compact-list scroll-list">
            {vencimientos.length === 0 && <EmptyState />}
            {vencimientos.map((item) => (
              <Link to={`/colaboradores/${item.colaboradorId}`} key={item.alertaId} className={`compact-item alert-item ${alertToneClass(item)}`}>
                <span>
                  <strong>{item.colaborador}</strong>
                  <small>{item.empresa || 'N/D'} - {item.tipoAlerta} - {item.estadoAlerta}</small>
                </span>
                <span className="alert-meta">
                  <em>{formatDate(item.fechaVencimiento)}</em>
                  <small>{alertTimingText(item)}</small>
                </span>
              </Link>
            ))}
          </div>
        </section>

        <section className="panel fixed-panel">
          <div className="panel-title-row">
            <h2>Recordatorios por vencer</h2>
            <FileClock size={18} aria-hidden />
          </div>
          <div className="compact-list scroll-list">
            {recordatorios.length === 0 && <EmptyState />}
            {recordatorios.map((item) => (
              <Link to={`/colaboradores/${item.colaboradorId}`} key={item.alertaId} className="compact-item alert-item">
                <span>
                  <strong>{item.colaborador}</strong>
                  <small>{item.empresa} - {item.tipoVencimiento}</small>
                </span>
                <em>{item.diasRestantes} dias</em>
              </Link>
            ))}
          </div>
        </section>

        <section className="panel wide-panel">
          <h2>Ultimos movimientos</h2>
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Fecha</th>
                  <th>Colaborador</th>
                  <th>Accion</th>
                  <th>Usuario</th>
                </tr>
              </thead>
              <tbody>
                {movimientos.length === 0 && (
                  <tr><td colSpan={4}><EmptyState /></td></tr>
                )}
                {movimientos.map((item) => (
                  <tr key={item.historialColaboradorId}>
                    <td>{formatDate(item.fecha)}</td>
                    <td>{item.colaborador}</td>
                    <td>{item.accion}</td>
                    <td>{item.usuario}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>
      </div>
    </section>
  );
}

function Metric({ to, icon, label, value }: { to: string; icon: ReactNode; label: string; value: number }) {
  return (
    <Link to={to} className="metric-card">
      <span>{icon}</span>
      <small>{label}</small>
      <strong>{value}</strong>
    </Link>
  );
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

function DonutChart({ items }: { items: ChartItem[] }) {
  const total = items.reduce((sum, item) => sum + item.value, 0);
  const background = total > 0 ? buildDonutGradient(items, total) : '#ede9fe';

  return (
    <div className="donut-layout">
      <div className="donut-chart" style={{ background }}>
        <span>{total}</span>
        <small>Total</small>
      </div>
      <div className="donut-legend">
        {items.length === 0 && <EmptyState />}
        {items.map((item, index) => (
          <div key={item.label}>
            <i style={{ background: chartColors[index % chartColors.length] }} />
            <span>{item.label}</span>
            <strong>{item.value}</strong>
          </div>
        ))}
      </div>
    </div>
  );
}

function DetalleMes({ title, items, tipo, dateField }: { title: string; items: ColaboradorList[]; tipo: 'Alta' | 'Baja'; dateField: 'fechaIngreso' | 'fechaSalida' }) {
  return (
    <div className="mini-detail-list">
      <strong>{title}</strong>
      {items.length === 0 && <span>Sin registros</span>}
      {items.map((item) => (
        <Link to={`/colaboradores/${item.colaboradorId}`} key={item.colaboradorId}>
          <span>
            <strong>{item.nombreCompleto}</strong>
            <small>{item.empresa} - {item.departamento || item.cargo}</small>
            <em>{tipo} - {formatDate(item[dateField] ?? item.fechaIngreso)}</em>
          </span>
        </Link>
      ))}
    </div>
  );
}

function EmptyState() {
  return <div className="empty-state">Sin datos</div>;
}

function withQuery(path: string, params: URLSearchParams) {
  const query = params.toString();
  return query ? `${path}?${query}` : path;
}

function buildDonutGradient(items: ChartItem[], total: number) {
  let cursor = 0;
  const stops = items.map((item, index) => {
    const start = (cursor / total) * 100;
    cursor += item.value;
    const end = (cursor / total) * 100;
    const color = chartColors[index % chartColors.length];
    return `${color} ${start}% ${end}%`;
  });

  return `conic-gradient(${stops.join(', ')})`;
}
