import { ActionsMenu } from '../common/ActionsMenu';
import type { AlertaList } from '../../types/alerta';

type AlertasTableProps = {
  alertas: AlertaList[];
  isLoading: boolean;
  onGestionar: (alerta: AlertaList) => void;
  onIgnorar: (alerta: AlertaList) => void;
};

function formatDate(value: string | null) {
  if (!value) {
    return '-';
  }

  return new Intl.DateTimeFormat('es-PA', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
  }).format(new Date(value));
}

function formatDateTime(value: string | null) {
  if (!value) {
    return '-';
  }

  return new Intl.DateTimeFormat('es-PA', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  }).format(new Date(value));
}

function canAct(alerta: AlertaList) {
  return alerta.estadoAlerta === 'Pendiente' || alerta.estadoAlerta === 'Vencida';
}

function stateClass(estado: string) {
  return `alert-state ${estado.toLowerCase()}`;
}

export function AlertasTable({ alertas, isLoading, onGestionar, onIgnorar }: AlertasTableProps) {
  return (
    <div className="table-panel">
      <div className="table-scroll">
        <table className="data-table alertas-table">
          <thead>
            <tr>
              <th>AlertaId</th>
              <th>TipoAlerta</th>
              <th>EstadoAlerta</th>
              <th>Colaborador</th>
              <th>Documento</th>
              <th>FechaVencimiento</th>
              <th>Mensaje</th>
              <th>FechaGeneracion</th>
              <th>FechaGestion</th>
              <th>GestionadaPor</th>
              <th>ObservacionGestion</th>
              <th>IsActive</th>
              <th>Acciones</th>
            </tr>
          </thead>
          <tbody>
            {isLoading && (
              <tr>
                <td colSpan={13}>Cargando alertas...</td>
              </tr>
            )}
            {!isLoading && alertas.length === 0 && (
              <tr>
                <td colSpan={13}>No hay alertas para los filtros seleccionados.</td>
              </tr>
            )}
            {!isLoading &&
              alertas.map((alerta) => (
                <tr key={alerta.alertaId}>
                  <td>{alerta.alertaId}</td>
                  <td>{alerta.tipoAlerta}</td>
                  <td>
                    <span className={stateClass(alerta.estadoAlerta)}>{alerta.estadoAlerta}</span>
                  </td>
                  <td>{alerta.nombreCompletoColaborador}</td>
                  <td>{alerta.tipoDocumentoNombre ?? '-'}</td>
                  <td>{formatDate(alerta.fechaVencimiento)}</td>
                  <td>{alerta.mensaje}</td>
                  <td>{formatDateTime(alerta.fechaGeneracion)}</td>
                  <td>{formatDateTime(alerta.fechaGestion)}</td>
                  <td>{alerta.gestionadaPorNombre ?? '-'}</td>
                  <td>{alerta.observacionGestion ?? '-'}</td>
                  <td>
                    <span className={alerta.isActive ? 'status active' : 'status inactive'}>
                      {alerta.isActive ? 'Activo' : 'Inactivo'}
                    </span>
                  </td>
                  <td>
                    {canAct(alerta) ? (
                      <ActionsMenu
                        items={[
                          {
                            label: 'Gestionar',
                            onClick: () => onGestionar(alerta),
                          },
                          {
                            label: 'Ignorar',
                            onClick: () => onIgnorar(alerta),
                          },
                        ]}
                      />
                    ) : (
                      <span className="muted">Sin acciones</span>
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
