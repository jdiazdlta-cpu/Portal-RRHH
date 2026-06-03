import { useEffect, useMemo, useState } from 'react';
import {
  getAltasBajas,
  getColaboradoresPorDepartamento,
  getColaboradoresPorEstatus,
  getDashboardResumen,
  getDashboardVencimientos,
  getUltimosMovimientos,
} from '../../api/dashboardApi';
import type {
  AltasBajas,
  ColaboradoresPorDepartamento,
  ColaboradoresPorEstatus,
  DashboardResumen,
  DashboardVencimientos,
  UltimosMovimientos,
} from '../../types/dashboard';

type DashboardState = {
  resumen: DashboardResumen;
  vencimientos: DashboardVencimientos;
  estatus: ColaboradoresPorEstatus[];
  departamentos: ColaboradoresPorDepartamento[];
  altasBajas: AltasBajas[];
  movimientos: UltimosMovimientos;
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

function metric(label: string, value: number) {
  return (
    <article className="metric-card" key={label}>
      <span>{label}</span>
      <strong>{value}</strong>
    </article>
  );
}

export function DashboardPage() {
  const [dashboard, setDashboard] = useState<DashboardState | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  const loadDashboard = async () => {
    setIsLoading(true);
    setError(null);

    try {
      const [
        resumen,
        vencimientos,
        estatus,
        departamentos,
        altasBajas,
        movimientos,
      ] = await Promise.all([
        getDashboardResumen(),
        getDashboardVencimientos(),
        getColaboradoresPorEstatus(),
        getColaboradoresPorDepartamento(),
        getAltasBajas(),
        getUltimosMovimientos(),
      ]);

      if (
        !resumen.data ||
        !vencimientos.data ||
        !estatus.data ||
        !departamentos.data ||
        !altasBajas.data ||
        !movimientos.data
      ) {
        throw new Error('La API no devolvio toda la informacion del dashboard.');
      }

      setDashboard({
        resumen: resumen.data,
        vencimientos: vencimientos.data,
        estatus: estatus.data,
        departamentos: departamentos.data,
        altasBajas: altasBajas.data,
        movimientos: movimientos.data,
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
  };

  useEffect(() => {
    document.title = 'Dashboard | Portal RRHH FZ';
    void loadDashboard();
  }, []);

  const vencimientosTotales = useMemo(() => {
    if (!dashboard) {
      return 0;
    }

    const vencimientos = dashboard.vencimientos;

    return (
      vencimientos.cedulasPorVencer +
      vencimientos.licenciasPorVencer +
      vencimientos.contratosPorVencer +
      vencimientos.periodosProbatoriosPorVencer +
      vencimientos.documentosPorVencer +
      vencimientos.cedulasVencidas +
      vencimientos.licenciasVencidas +
      vencimientos.contratosVencidos +
      vencimientos.periodosProbatoriosVencidos +
      vencimientos.documentosVencidos
    );
  }, [dashboard]);

  if (isLoading) {
    return (
      <section className="state-panel">
        <div className="loader" />
        <h2>Cargando dashboard</h2>
      </section>
    );
  }

  if (error || !dashboard) {
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

  return (
    <div className="dashboard-page">
      <section className="page-heading">
        <div>
          <span className="eyebrow">Vista operativa</span>
          <h2>Dashboard RRHH</h2>
        </div>
        <button className="secondary-button" type="button" onClick={loadDashboard}>
          Actualizar
        </button>
      </section>

      <section className="metric-grid" aria-label="Resumen principal">
        {metric('Colaboradores', dashboard.resumen.totalColaboradores)}
        {metric('Activos', dashboard.resumen.totalActivos)}
        {metric('Cesantes', dashboard.resumen.totalCesantes)}
        {metric('Documentos', dashboard.resumen.totalDocumentosActivos)}
        {metric('Alertas pendientes', dashboard.resumen.totalAlertasPendientes)}
        {metric('Vencidos', dashboard.resumen.totalAlertasVencidas)}
      </section>

      <section className="dashboard-grid">
        <article className="panel">
          <header className="panel-header">
            <h3>Vencimientos</h3>
            <span className="count-pill">{vencimientosTotales}</span>
          </header>
          <div className="split-list">
            <span>Cedulas</span>
            <strong>
              {dashboard.vencimientos.cedulasPorVencer} / {dashboard.vencimientos.cedulasVencidas}
            </strong>
            <span>Licencias</span>
            <strong>
              {dashboard.vencimientos.licenciasPorVencer} /{' '}
              {dashboard.vencimientos.licenciasVencidas}
            </strong>
            <span>Contratos</span>
            <strong>
              {dashboard.vencimientos.contratosPorVencer} /{' '}
              {dashboard.vencimientos.contratosVencidos}
            </strong>
            <span>Probatorio</span>
            <strong>
              {dashboard.vencimientos.periodosProbatoriosPorVencer} /{' '}
              {dashboard.vencimientos.periodosProbatoriosVencidos}
            </strong>
            <span>Documentos</span>
            <strong>
              {dashboard.vencimientos.documentosPorVencer} /{' '}
              {dashboard.vencimientos.documentosVencidos}
            </strong>
          </div>
        </article>

        <article className="panel">
          <header className="panel-header">
            <h3>Por estatus</h3>
          </header>
          <div className="bar-list">
            {dashboard.estatus.map((item) => (
              <div className="bar-row" key={item.estatusId}>
                <span>{item.nombre}</span>
                <div>
                  <i style={{ width: `${Math.max(item.total * 18, 8)}px` }} />
                  <strong>{item.total}</strong>
                </div>
              </div>
            ))}
          </div>
        </article>

        <article className="panel">
          <header className="panel-header">
            <h3>Departamentos</h3>
          </header>
          <div className="compact-list">
            {dashboard.departamentos.length === 0 && <span>Sin datos registrados.</span>}
            {dashboard.departamentos.slice(0, 6).map((item) => (
              <div className="list-row" key={item.departamentoId}>
                <span>{item.departamentoNombre}</span>
                <strong>{item.total}</strong>
              </div>
            ))}
          </div>
        </article>

        <article className="panel">
          <header className="panel-header">
            <h3>Altas y bajas</h3>
          </header>
          <div className="months-grid">
            {dashboard.altasBajas.slice(0, 12).map((item) => (
              <div className="month-cell" key={item.mes}>
                <span>{monthNames[item.mes - 1] ?? item.mes}</span>
                <strong>{item.altas}</strong>
                <em>{item.bajas}</em>
              </div>
            ))}
          </div>
        </article>

        <article className="panel wide-panel">
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
        </article>
      </section>
    </div>
  );
}
