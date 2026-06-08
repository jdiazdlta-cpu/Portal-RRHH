import type { AlertaResumen } from '../../types/alerta';

type AlertasResumenCardsProps = {
  resumen: AlertaResumen | null;
};

function metric(label: string, value: number) {
  return (
    <article className="metric-card" key={label}>
      <span>{label}</span>
      <strong>{value}</strong>
    </article>
  );
}

export function AlertasResumenCards({ resumen }: AlertasResumenCardsProps) {
  if (!resumen) {
    return null;
  }

  return (
    <>
      <section className="metric-grid alertas-metrics" aria-label="Resumen de alertas">
        {metric('TotalAlertas', resumen.totalAlertas)}
        {metric('Pendientes', resumen.pendientes)}
        {metric('Vencidas', resumen.vencidas)}
        {metric('Gestionadas', resumen.gestionadas)}
        {metric('Ignoradas', resumen.ignoradas)}
        {metric('ProximasAVencer', resumen.proximasAVencer)}
        {metric('VencidasPendientes', resumen.vencidasPendientes)}
      </section>

      <section className="panel alertas-tipos-panel">
        <header className="panel-header">
          <h3>PorTipoAlerta</h3>
        </header>
        <div className="bar-list">
          {resumen.porTipoAlerta.length === 0 && <span className="muted">Sin datos por tipo.</span>}
          {resumen.porTipoAlerta.map((item) => (
            <div className="bar-row" key={item.tipoAlerta}>
              <span>{item.tipoAlerta}</span>
              <div>
                <i style={{ width: `${Math.max(item.total * 18, 8)}px` }} />
                <strong>{item.total}</strong>
              </div>
            </div>
          ))}
        </div>
      </section>
    </>
  );
}
