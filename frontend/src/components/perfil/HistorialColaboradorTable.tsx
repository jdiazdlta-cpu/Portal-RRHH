import type { HistorialColaborador } from '../../types/colaborador';

type HistorialColaboradorTableProps = {
  historial: HistorialColaborador[];
};

function formatDate(value: string) {
  return new Intl.DateTimeFormat('es-PA', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  }).format(new Date(value));
}

export function HistorialColaboradorTable({ historial }: HistorialColaboradorTableProps) {
  return (
    <section className="panel wide-panel">
      <header className="panel-header">
        <h3>Historial</h3>
        <span className="count-pill">{historial.length}</span>
      </header>
      <div className="table-scroll">
        <table className="data-table compact-table">
          <thead>
            <tr>
              <th>Fecha</th>
              <th>Usuario</th>
              <th>Accion</th>
              <th>Campo</th>
              <th>Anterior</th>
              <th>Nuevo</th>
              <th>Observacion</th>
            </tr>
          </thead>
          <tbody>
            {historial.length === 0 && (
              <tr>
                <td colSpan={7}>Sin historial registrado.</td>
              </tr>
            )}
            {historial.map((item) => (
              <tr key={item.historialColaboradorId}>
                <td>{formatDate(item.fecha)}</td>
                <td>{item.usuarioNombre}</td>
                <td>{item.accion}</td>
                <td>{item.campo ?? '-'}</td>
                <td>{item.valorAnterior ?? '-'}</td>
                <td>{item.valorNuevo ?? '-'}</td>
                <td>{item.observacion ?? '-'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  );
}
