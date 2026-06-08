import { useCallback, useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { getAlertas } from '../../api/alertasApi';
import { getDepartamentosCatalogo, getEmpresasCatalogo } from '../../api/catalogosApi';
import { getColaboradores } from '../../api/colaboradoresApi';
import {
  getAltasBajas,
  getAltasDetalle,
  getBajasDetalle,
  getDashboardResumen,
  getDashboardVencimientos,
  getUltimosMovimientos,
} from '../../api/dashboardApi';
import type { AlertaList, TipoAlerta } from '../../types/alerta';
import type { DepartamentoCatalogo, EmpresaCatalogo } from '../../types/catalogos';
import type { ColaboradorList } from '../../types/colaborador';
import type {
  AltasBajas,
  AltaDetalle,
  BajaDetalle,
  DashboardResumen,
  DashboardVencimientos,
  UltimosMovimientos,
} from '../../types/dashboard';

type DashboardState = {
  resumen: DashboardResumen;
  vencimientos: DashboardVencimientos;
  altasBajas: AltasBajas[];
  movimientos: UltimosMovimientos;
  colaboradores: ColaboradorList[];
  alertas: AlertaList[];
  empresas: EmpresaCatalogo[];
  departamentos: DepartamentoCatalogo[];
};

type DashboardFilters = {
  empresaId: string;
  departamentoId: string;
  anio: string;
  mes: string;
};

type ChartRow = {
  key: string;
  label: string;
  detail?: string;
  total: number;
};

type DepartmentChartRow = ChartRow & {
  empresaId: number;
  empresaNombre: string;
  departamentoId: number;
  departamentoNombre: string;
};

type EnrichedAlerta = AlertaList & {
  empresaNombre: string;
  departamentoNombre: string;
  cargoNombre: string;
};

type MovementModalState = {
  kind: 'altas' | 'bajas';
  title: string;
  items: Array<AltaDetalle | BajaDetalle>;
  isLoading: boolean;
  error: string | null;
};

type VencimientoModalState = {
  title: string;
  alertas: EnrichedAlerta[];
};

const monthNames = [
  'Ene',
  'Feb',
  'Mar',
  'Abr',
  'May',
  'Jun',
  'Jul',
  'Ago',
  'Sep',
  'Oct',
  'Nov',
  'Dic',
];

const vencimientoLabels: Array<{ tipo: TipoAlerta; label: string }> = [
  { tipo: 'Cedula', label: 'Cedulas' },
  { tipo: 'Licencia', label: 'Licencias' },
  { tipo: 'Contrato', label: 'Contratos' },
  { tipo: 'PeriodoProbatorio', label: 'Periodos probatorios' },
  { tipo: 'Documento', label: 'Documentos' },
];

const statusOrder = ['Activo', 'Cesante', 'Vacaciones', 'Servicio', 'Suspendido'];

function toNumber(value: string) {
  const parsed = Number(value);
  return Number.isFinite(parsed) && parsed > 0 ? parsed : undefined;
}

function formatDate(value: string | null) {
  if (!value) {
    return 'Sin fecha';
  }

  return new Intl.DateTimeFormat('es-PA', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
  }).format(new Date(value));
}

function getDaysFromToday(value: string) {
  const today = new Date();
  today.setHours(0, 0, 0, 0);

  const date = new Date(value);
  date.setHours(0, 0, 0, 0);

  return Math.ceil((date.getTime() - today.getTime()) / 86_400_000);
}

function formatDaysLabel(value: string) {
  const days = getDaysFromToday(value);

  if (days === 0) {
    return 'Vence hoy';
  }

  if (days > 0) {
    return `${days} dias restantes`;
  }

  return `${Math.abs(days)} dias vencido`;
}

function isVencida(alerta: AlertaList) {
  return alerta.estadoAlerta === 'Vencida'
    || (alerta.estadoAlerta === 'Pendiente' && getDaysFromToday(alerta.fechaVencimiento) < 0);
}

function isPendienteAVencer(alerta: AlertaList) {
  return alerta.estadoAlerta === 'Pendiente' && getDaysFromToday(alerta.fechaVencimiento) >= 0;
}

function groupByKey<T>(
  items: T[],
  getKey: (item: T) => string,
  getRow: (item: T) => Omit<ChartRow, 'total'>,
) {
  const grouped = new Map<string, ChartRow>();

  for (const item of items) {
    const key = getKey(item);
    const current = grouped.get(key);

    if (current) {
      current.total += 1;
    } else {
      grouped.set(key, { ...getRow(item), total: 1 });
    }
  }

  return Array.from(grouped.values()).sort((a, b) => b.total - a.total || a.label.localeCompare(b.label));
}

function buildDepartmentRows(colaboradores: ColaboradorList[]): DepartmentChartRow[] {
  const grouped = new Map<string, DepartmentChartRow>();

  for (const colaborador of colaboradores) {
    const key = String(colaborador.departamentoId);
    const current = grouped.get(key);

    if (current) {
      current.total += 1;
    } else {
      grouped.set(key, {
        key,
        empresaId: colaborador.empresaId,
        empresaNombre: colaborador.empresaNombre,
        departamentoId: colaborador.departamentoId,
        departamentoNombre: colaborador.departamentoNombre,
        label: colaborador.departamentoNombre,
        detail: colaborador.empresaNombre,
        total: 1,
      });
    }
  }

  return Array.from(grouped.values()).sort((a, b) => b.total - a.total || a.label.localeCompare(b.label));
}

function HorizontalBars({ rows, emptyText }: { rows: ChartRow[]; emptyText: string }) {
  const max = Math.max(...rows.map((row) => row.total), 0);

  if (rows.length === 0) {
    return <span className="muted">{emptyText}</span>;
  }

  return (
    <div className="dashboard-bars">
      {rows.map((row) => {
        const width = max === 0 ? 0 : Math.max((row.total / max) * 100, 8);

        return (
          <div className="dashboard-bar-row" key={row.key}>
            <div className="dashboard-bar-label">
              <strong>{row.label}</strong>
              {row.detail && <span>{row.detail}</span>}
            </div>
            <div className="dashboard-bar-track">
              <i style={{ width: `${width}%` }} />
            </div>
            <strong className="dashboard-bar-total">{row.total}</strong>
          </div>
        );
      })}
    </div>
  );
}

function DonutChart({ rows }: { rows: ChartRow[] }) {
  const colors = ['#6d28d9', '#8b5cf6', '#a78bfa', '#c4b5fd', '#ddd6fe'];
  const total = rows.reduce((sum, row) => sum + row.total, 0);
  let start = 0;

  const gradient = rows
    .filter((row) => row.total > 0)
    .map((row, index) => {
      const percent = total === 0 ? 0 : (row.total / total) * 100;
      const end = start + percent;
      const segment = `${colors[index % colors.length]} ${start}% ${end}%`;
      start = end;
      return segment;
    })
    .join(', ');

  return (
    <div className="donut-wrap">
      <div
        className="donut-chart"
        style={{ background: total === 0 ? '#ede9fe' : `conic-gradient(${gradient})` }}
      >
        <div>
          <strong>{total}</strong>
          <span>Activos</span>
        </div>
      </div>
      <div className="donut-legend">
        {rows.map((row, index) => {
          const percent = total === 0 ? 0 : Math.round((row.total / total) * 100);

          return (
            <div className="donut-legend-row" key={row.key}>
              <i style={{ background: colors[index % colors.length] }} />
              <span>{row.label}</span>
              <strong>
                {row.total} ({percent}%)
              </strong>
            </div>
          );
        })}
      </div>
    </div>
  );
}

export function DashboardPage() {
  const navigate = useNavigate();
  const currentDate = new Date();
  const [filters, setFilters] = useState<DashboardFilters>({
    empresaId: '',
    departamentoId: '',
    anio: String(currentDate.getFullYear()),
    mes: String(currentDate.getMonth() + 1),
  });
  const [dashboard, setDashboard] = useState<DashboardState | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [departmentModalOpen, setDepartmentModalOpen] = useState(false);
  const [movementModal, setMovementModal] = useState<MovementModalState | null>(null);
  const [vencimientoModal, setVencimientoModal] = useState<VencimientoModalState | null>(null);

  const selectedEmpresaId = toNumber(filters.empresaId);
  const selectedDepartamentoId = toNumber(filters.departamentoId);
  const selectedAnio = Number(filters.anio) || currentDate.getFullYear();
  const selectedMes = Number(filters.mes) || currentDate.getMonth() + 1;

  const loadDashboard = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    try {
      const [
        resumen,
        vencimientos,
        altasBajas,
        movimientos,
        colaboradores,
        alertas,
        empresas,
        departamentos,
      ] = await Promise.all([
        getDashboardResumen(),
        getDashboardVencimientos(),
        getAltasBajas({
          anio: selectedAnio,
          empresaId: selectedEmpresaId,
          departamentoId: selectedDepartamentoId,
        }),
        getUltimosMovimientos(),
        getColaboradores({
          empresaId: selectedEmpresaId,
          departamentoId: selectedDepartamentoId,
        }),
        getAlertas({ incluirInactivas: true }),
        getEmpresasCatalogo(),
        getDepartamentosCatalogo(selectedEmpresaId),
      ]);

      if (
        !resumen.data ||
        !vencimientos.data ||
        !altasBajas.data ||
        !movimientos.data ||
        !colaboradores.data ||
        !alertas.data ||
        !empresas.data ||
        !departamentos.data
      ) {
        throw new Error('La API no devolvio toda la informacion del dashboard.');
      }

      setDashboard({
        resumen: resumen.data,
        vencimientos: vencimientos.data,
        altasBajas: altasBajas.data,
        movimientos: movimientos.data,
        colaboradores: colaboradores.data,
        alertas: alertas.data,
        empresas: empresas.data,
        departamentos: departamentos.data,
      });
    } catch (loadError) {
      setError(
        loadError instanceof Error
          ? loadError.message
          : 'No fue posible cargar el dashboard.',
      );
    } finally {
      setIsLoading(false);
    }
  }, [selectedAnio, selectedDepartamentoId, selectedEmpresaId]);

  useEffect(() => {
    document.title = 'Dashboard | Portal RRHH FZ';
  }, []);

  useEffect(() => {
    void loadDashboard();
  }, [loadDashboard]);

  const computed = useMemo(() => {
    if (!dashboard) {
      return null;
    }

    const activeColaboradores = dashboard.colaboradores.filter((colaborador) => colaborador.isActive);
    const colaboradorMap = new Map(
      dashboard.colaboradores.map((colaborador) => [colaborador.colaboradorId, colaborador]),
    );
    const alertasEnriquecidas = dashboard.alertas
      .map((alerta): EnrichedAlerta | null => {
        const colaborador = colaboradorMap.get(alerta.colaboradorId);

        if (!colaborador) {
          return null;
        }

        return {
          ...alerta,
          empresaNombre: colaborador.empresaNombre,
          departamentoNombre: colaborador.departamentoNombre,
          cargoNombre: colaborador.cargoNombre,
        };
      })
      .filter((alerta): alerta is EnrichedAlerta => alerta !== null && alerta.isActive);

    const companyRows = groupByKey(
      activeColaboradores,
      (colaborador) => String(colaborador.empresaId),
      (colaborador) => ({
        key: String(colaborador.empresaId),
        label: colaborador.empresaNombre,
      }),
    );
    const departmentRows = buildDepartmentRows(activeColaboradores);
    const statusGrouped = groupByKey(
      dashboard.colaboradores,
      (colaborador) => colaborador.estatusNombre,
      (colaborador) => ({
        key: colaborador.estatusNombre,
        label: colaborador.estatusNombre,
      }),
    );
    const statusRows = [
      ...statusOrder
        .map((status) => statusGrouped.find((row) => row.label.toLowerCase() === status.toLowerCase()) ?? {
          key: status,
          label: status,
          total: 0,
        }),
      ...statusGrouped.filter(
        (row) => !statusOrder.some((status) => status.toLowerCase() === row.label.toLowerCase()),
      ),
    ];
    const contractRows = groupByKey(
      activeColaboradores,
      (colaborador) => colaborador.tipoContratoNombre,
      (colaborador) => ({
        key: colaborador.tipoContratoNombre,
        label: colaborador.tipoContratoNombre,
      }),
    );
    const vencimientoCards = vencimientoLabels.map((item) => {
      const alertas = alertasEnriquecidas.filter((alerta) =>
        alerta.tipoAlerta === item.tipo
        && (alerta.estadoAlerta === 'Pendiente' || alerta.estadoAlerta === 'Vencida')
      );
      const proximos = alertas.filter(isPendienteAVencer).length;
      const vencidos = alertas.filter(isVencida).length;

      return {
        ...item,
        alertas,
        proximos,
        vencidos,
        total: proximos + vencidos,
      };
    });
    const alertasCriticas = alertasEnriquecidas
      .filter(isVencida)
      .sort((a, b) => new Date(a.fechaVencimiento).getTime() - new Date(b.fechaVencimiento).getTime());

    return {
      activeColaboradores,
      alertasCriticas,
      companyRows,
      contractRows,
      departmentRows,
      statusRows,
      vencimientoCards,
    };
  }, [dashboard]);

  const monthRecord = useMemo(() => {
    return dashboard?.altasBajas.find((item) => item.mes === selectedMes) ?? {
      mes: selectedMes,
      altas: 0,
      bajas: 0,
    };
  }, [dashboard, selectedMes]);

  const yearOptions = useMemo(() => {
    const year = currentDate.getFullYear();
    return Array.from({ length: 7 }, (_, index) => year - 3 + index);
  }, [currentDate]);

  const updateFilter = (name: keyof DashboardFilters, value: string) => {
    setFilters((current) => ({
      ...current,
      [name]: value,
      ...(name === 'empresaId' ? { departamentoId: '' } : {}),
    }));
  };

  const openMovementModal = async (kind: 'altas' | 'bajas') => {
    const title = `${kind === 'altas' ? 'Altas' : 'Bajas'} de ${monthNames[selectedMes - 1]} ${selectedAnio}`;

    setMovementModal({
      kind,
      title,
      items: [],
      isLoading: true,
      error: null,
    });

    try {
      const response = kind === 'altas'
        ? await getAltasDetalle({
          anio: selectedAnio,
          mes: selectedMes,
          empresaId: selectedEmpresaId,
          departamentoId: selectedDepartamentoId,
        })
        : await getBajasDetalle({
          anio: selectedAnio,
          mes: selectedMes,
          empresaId: selectedEmpresaId,
          departamentoId: selectedDepartamentoId,
        });

      setMovementModal({
        kind,
        title,
        items: response.data ?? [],
        isLoading: false,
        error: null,
      });
    } catch (modalError) {
      setMovementModal({
        kind,
        title,
        items: [],
        isLoading: false,
        error: modalError instanceof Error ? modalError.message : 'No fue posible cargar el detalle.',
      });
    }
  };

  const goToProfile = (colaboradorId: number) => {
    setMovementModal(null);
    setVencimientoModal(null);
    navigate(`/colaboradores/${colaboradorId}/perfil`);
  };

  if (isLoading) {
    return (
      <section className="state-panel">
        <div className="loader" />
        <h2>Cargando dashboard</h2>
      </section>
    );
  }

  if (error || !dashboard || !computed) {
    return (
      <section className="state-panel">
        <h2>No se pudo cargar el dashboard</h2>
        <p>{error}</p>
        <button className="primary-button compact" type="button" onClick={loadDashboard}>
          Reintentar
        </button>
      </section>
    );
  }

  const totalAlertasCriticas = computed.alertasCriticas.length;

  return (
    <div className="dashboard-page">
      <section className="page-heading dashboard-heading">
        <div>
          <span className="eyebrow">Vista operativa</span>
          <h2>Dashboard RRHH</h2>
          <p>Indicadores filtrados por alcance operativo, sin exponer datos salariales.</p>
        </div>
        <button className="secondary-button" type="button" onClick={loadDashboard}>
          Actualizar
        </button>
      </section>

      <section className="dashboard-filter-panel" aria-label="Filtros globales">
        <label>
          Empresa
          <select
            value={filters.empresaId}
            onChange={(event) => updateFilter('empresaId', event.target.value)}
          >
            <option value="">Todas</option>
            {dashboard.empresas.map((empresa) => (
              <option key={empresa.empresaId} value={empresa.empresaId}>
                {empresa.nombre}
              </option>
            ))}
          </select>
        </label>
        <label>
          Departamento
          <select
            value={filters.departamentoId}
            onChange={(event) => updateFilter('departamentoId', event.target.value)}
          >
            <option value="">Todos</option>
            {dashboard.departamentos.map((departamento) => (
              <option key={departamento.departamentoId} value={departamento.departamentoId}>
                {departamento.nombre}
              </option>
            ))}
          </select>
        </label>
        <label>
          Ano
          <select value={filters.anio} onChange={(event) => updateFilter('anio', event.target.value)}>
            {yearOptions.map((year) => (
              <option key={year} value={year}>
                {year}
              </option>
            ))}
          </select>
        </label>
        <label>
          Mes
          <select value={filters.mes} onChange={(event) => updateFilter('mes', event.target.value)}>
            {monthNames.map((month, index) => (
              <option key={month} value={index + 1}>
                {month}
              </option>
            ))}
          </select>
        </label>
      </section>

      {dashboard.vencimientos.requiereRecalculo && (
        <section className="dashboard-warning">
          Hay alertas pendientes con fecha vencida. Conviene recalcular alertas desde el modulo Alertas.
        </section>
      )}

      <section className="metric-grid dashboard-summary-grid" aria-label="Resumen principal">
        <article className="metric-card">
          <span>Colaboradores en vista</span>
          <strong>{dashboard.colaboradores.length}</strong>
        </article>
        <article className="metric-card">
          <span>Activos en vista</span>
          <strong>{computed.activeColaboradores.length}</strong>
        </article>
        <article className="metric-card">
          <span>Empresas activas</span>
          <strong>{dashboard.resumen.totalEmpresasActivas}</strong>
        </article>
        <article className="metric-card">
          <span>Documentos activos</span>
          <strong>{dashboard.resumen.totalDocumentosActivos}</strong>
        </article>
        <article className="metric-card critical">
          <span>Alertas criticas</span>
          <strong>{totalAlertasCriticas}</strong>
        </article>
      </section>

      <section className="dashboard-panel-grid">
        <article className="panel dashboard-chart-panel">
          <header className="panel-header">
            <div>
              <h3>Activos por empresa</h3>
              <span className="muted">Colaboradores activos dentro del alcance seleccionado.</span>
            </div>
          </header>
          <HorizontalBars rows={computed.companyRows} emptyText="Sin colaboradores activos." />
        </article>

        <article className="panel dashboard-chart-panel">
          <header className="panel-header">
            <div>
              <h3>Top departamentos</h3>
              <span className="muted">Los 10 departamentos con mas colaboradores activos.</span>
            </div>
            <button
              className="secondary-button compact"
              disabled={computed.departmentRows.length === 0}
              type="button"
              onClick={() => setDepartmentModalOpen(true)}
            >
              Ver todos
            </button>
          </header>
          <HorizontalBars
            rows={computed.departmentRows.slice(0, 10)}
            emptyText="Sin departamentos con colaboradores activos."
          />
        </article>

        <article className="panel dashboard-chart-panel">
          <header className="panel-header">
            <h3>Colaboradores por estatus</h3>
          </header>
          <div className="status-card-grid">
            {computed.statusRows.map((row) => (
              <div className="status-mini-card" key={row.key}>
                <span>{row.label}</span>
                <strong>{row.total}</strong>
              </div>
            ))}
          </div>
        </article>

        <article className="panel dashboard-chart-panel">
          <header className="panel-header">
            <div>
              <h3>Permanentes vs Eventuales</h3>
              <span className="muted">Distribucion por tipo de contrato de activos.</span>
            </div>
          </header>
          <DonutChart rows={computed.contractRows} />
        </article>
      </section>

      <section className="panel dashboard-movements-panel">
        <header className="panel-header">
          <div>
            <h3>Altas y bajas</h3>
            <span className="muted">Detalle mensual segun los filtros de ano, mes, empresa y departamento.</span>
          </div>
        </header>
        <div className="movement-action-grid">
          <button className="dashboard-action-card positive" type="button" onClick={() => openMovementModal('altas')}>
            <span>Altas del mes</span>
            <strong>{monthRecord.altas}</strong>
            <small>{monthNames[selectedMes - 1]} {selectedAnio}</small>
          </button>
          <button className="dashboard-action-card negative" type="button" onClick={() => openMovementModal('bajas')}>
            <span>Bajas del mes</span>
            <strong>{monthRecord.bajas}</strong>
            <small>{monthNames[selectedMes - 1]} {selectedAnio}</small>
          </button>
          <div className="months-grid dashboard-months-grid">
            {dashboard.altasBajas.slice(0, 12).map((item) => (
              <div className={item.mes === selectedMes ? 'month-cell active' : 'month-cell'} key={item.mes}>
                <span>{monthNames[item.mes - 1] ?? item.mes}</span>
                <strong>{item.altas}</strong>
                <em>{item.bajas}</em>
              </div>
            ))}
          </div>
        </div>
      </section>

      <section className="panel dashboard-vencimientos-panel">
        <header className="panel-header">
          <div>
            <h3>Vencimientos</h3>
            <span className="muted">Tarjetas clickeables con proximos a vencer y vencidos.</span>
          </div>
        </header>
        <div className="vencimiento-card-grid">
          {computed.vencimientoCards.map((card) => (
            <button
              className="vencimiento-card"
              key={card.tipo}
              type="button"
              onClick={() => setVencimientoModal({ title: card.label, alertas: card.alertas })}
            >
              <span>{card.label}</span>
              <strong>{card.total}</strong>
              <div>
                <small>{card.proximos} proximos</small>
                <small>{card.vencidos} vencidos</small>
              </div>
            </button>
          ))}
        </div>
      </section>

      <section className="panel critical-alerts-panel">
        <header className="panel-header">
          <div>
            <h3>Alertas criticas</h3>
            <span className="muted">Vencidas priorizadas por fecha de vencimiento.</span>
          </div>
          <span className="count-pill">{totalAlertasCriticas}</span>
        </header>
        <div className="critical-alert-list">
          {computed.alertasCriticas.length === 0 && (
            <span className="muted">No hay alertas criticas en el alcance seleccionado.</span>
          )}
          {computed.alertasCriticas.slice(0, 5).map((alerta) => (
            <div className="critical-alert-row" key={alerta.alertaId}>
              <div>
                <strong>{alerta.nombreCompletoColaborador}</strong>
                <span>{alerta.tipoAlerta} | {formatDate(alerta.fechaVencimiento)} | {formatDaysLabel(alerta.fechaVencimiento)}</span>
              </div>
              <button className="secondary-button compact" type="button" onClick={() => goToProfile(alerta.colaboradorId)}>
                Ir al perfil
              </button>
            </div>
          ))}
        </div>
      </section>

      <section className="panel wide-panel">
        <header className="panel-header">
          <h3>Ultimos movimientos</h3>
        </header>
        <div className="movement-grid">
          <div>
            <h4>Ingresos</h4>
            {dashboard.movimientos.ultimosIngresos.length === 0 && (
              <span className="muted">Sin ingresos recientes.</span>
            )}
            {dashboard.movimientos.ultimosIngresos.slice(0, 5).map((item) => (
              <div className="movement-row" key={`ingreso-${item.colaboradorId}`}>
                <strong>{item.nombreCompleto}</strong>
                <span>{formatDate(item.fechaIngreso)}</span>
              </div>
            ))}
          </div>
          <div>
            <h4>Salidas</h4>
            {dashboard.movimientos.ultimasSalidas.length === 0 && (
              <span className="muted">Sin salidas recientes.</span>
            )}
            {dashboard.movimientos.ultimasSalidas.slice(0, 5).map((item) => (
              <div className="movement-row" key={`salida-${item.colaboradorId}`}>
                <strong>{item.nombreCompleto}</strong>
                <span>{formatDate(item.fechaSalida)}</span>
              </div>
            ))}
          </div>
        </div>
      </section>

      {departmentModalOpen && (
        <div className="modal-backdrop">
          <section className="modal large-modal">
            <header className="modal-header">
              <h3>Todos los departamentos</h3>
              <button className="icon-button" type="button" onClick={() => setDepartmentModalOpen(false)}>
                X
              </button>
            </header>
            <div className="table-scroll modal-table-wrap">
              <table className="data-table compact-table">
                <thead>
                  <tr>
                    <th>Empresa</th>
                    <th>Departamento</th>
                    <th>Activos</th>
                  </tr>
                </thead>
                <tbody>
                  {computed.departmentRows.length === 0 && (
                    <tr>
                      <td colSpan={3}>Sin datos registrados.</td>
                    </tr>
                  )}
                  {computed.departmentRows.map((row) => (
                    <tr key={row.departamentoId}>
                      <td>{row.empresaNombre}</td>
                      <td>{row.departamentoNombre}</td>
                      <td>{row.total}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </section>
        </div>
      )}

      {movementModal && (
        <div className="modal-backdrop">
          <section className="modal xl-modal">
            <header className="modal-header">
              <h3>{movementModal.title}</h3>
              <button className="icon-button" type="button" onClick={() => setMovementModal(null)}>
                X
              </button>
            </header>
            {movementModal.isLoading && (
              <div className="modal-state">
                <div className="loader" />
                <span>Cargando detalle...</span>
              </div>
            )}
            {movementModal.error && <div className="form-error-list">{movementModal.error}</div>}
            {!movementModal.isLoading && !movementModal.error && (
              <div className="table-scroll modal-table-wrap">
                <table className="data-table">
                  <thead>
                    <tr>
                      <th>Nombre</th>
                      <th>Cedula</th>
                      <th>Empresa</th>
                      <th>Departamento</th>
                      <th>Cargo</th>
                      <th>{movementModal.kind === 'altas' ? 'Fecha ingreso' : 'Fecha salida'}</th>
                      <th>{movementModal.kind === 'altas' ? 'Estatus' : 'Motivo'}</th>
                      <th>Contrato</th>
                      <th>Accion</th>
                    </tr>
                  </thead>
                  <tbody>
                    {movementModal.items.length === 0 && (
                      <tr>
                        <td colSpan={9}>Sin registros para el periodo seleccionado.</td>
                      </tr>
                    )}
                    {movementModal.items.map((item) => (
                      <tr key={item.colaboradorId}>
                        <td>{item.nombreCompleto}</td>
                        <td>{item.cedula}</td>
                        <td>{item.empresaNombre}</td>
                        <td>{item.departamentoNombre}</td>
                        <td>{item.cargoNombre}</td>
                        <td>
                          {movementModal.kind === 'altas'
                            ? formatDate((item as AltaDetalle).fechaIngreso)
                            : formatDate((item as BajaDetalle).fechaSalida)}
                        </td>
                        <td>
                          {movementModal.kind === 'altas'
                            ? (item as AltaDetalle).estatusNombre
                            : (item as BajaDetalle).motivoSalidaNombre}
                        </td>
                        <td>{item.tipoContratoNombre}</td>
                        <td>
                          <button className="secondary-button compact" type="button" onClick={() => goToProfile(item.colaboradorId)}>
                            Ir al perfil
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </section>
        </div>
      )}

      {vencimientoModal && (
        <div className="modal-backdrop">
          <section className="modal xl-modal">
            <header className="modal-header">
              <h3>Vencimientos | {vencimientoModal.title}</h3>
              <button className="icon-button" type="button" onClick={() => setVencimientoModal(null)}>
                X
              </button>
            </header>
            <div className="table-scroll modal-table-wrap">
              <table className="data-table alertas-table">
                <thead>
                  <tr>
                    <th>Tipo</th>
                    <th>Estado</th>
                    <th>Colaborador</th>
                    <th>Empresa</th>
                    <th>Departamento</th>
                    <th>Cargo</th>
                    <th>Documento</th>
                    <th>Vencimiento</th>
                    <th>Dias</th>
                    <th>Mensaje</th>
                    <th>Accion</th>
                  </tr>
                </thead>
                <tbody>
                  {vencimientoModal.alertas.length === 0 && (
                    <tr>
                      <td colSpan={11}>Sin alertas para este tipo.</td>
                    </tr>
                  )}
                  {vencimientoModal.alertas.map((alerta) => (
                    <tr key={alerta.alertaId}>
                      <td>{alerta.tipoAlerta}</td>
                      <td>
                        <span className={`alert-state ${alerta.estadoAlerta.toLowerCase()}`}>
                          {alerta.estadoAlerta}
                        </span>
                      </td>
                      <td>{alerta.nombreCompletoColaborador}</td>
                      <td>{alerta.empresaNombre}</td>
                      <td>{alerta.departamentoNombre}</td>
                      <td>{alerta.cargoNombre}</td>
                      <td>{alerta.tipoDocumentoNombre ?? '-'}</td>
                      <td>{formatDate(alerta.fechaVencimiento)}</td>
                      <td>{formatDaysLabel(alerta.fechaVencimiento)}</td>
                      <td>{alerta.mensaje}</td>
                      <td>
                        <button className="secondary-button compact" type="button" onClick={() => goToProfile(alerta.colaboradorId)}>
                          Ir al perfil
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </section>
        </div>
      )}
    </div>
  );
}
