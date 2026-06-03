import type { ColaboradorList } from '../../types/colaborador';

type ColaboradoresTableProps = {
  colaboradores: ColaboradorList[];
  isLoading: boolean;
  onView: (id: number) => void;
  onEdit: (id: number) => void;
  onToggleActive: (colaborador: ColaboradorList) => void;
};

export function ColaboradoresTable({
  colaboradores,
  isLoading,
  onEdit,
  onToggleActive,
  onView,
}: ColaboradoresTableProps) {
  return (
    <div className="table-panel">
      <div className="table-scroll">
        <table className="data-table">
          <thead>
            <tr>
              <th>NoEmpleado</th>
              <th>NombreCompleto</th>
              <th>Cedula</th>
              <th>Empresa</th>
              <th>Departamento</th>
              <th>Cargo</th>
              <th>Contrato</th>
              <th>Estatus</th>
              <th>Activo</th>
              <th>Acciones</th>
            </tr>
          </thead>
          <tbody>
            {isLoading && (
              <tr>
                <td colSpan={10}>Cargando colaboradores...</td>
              </tr>
            )}

            {!isLoading && colaboradores.length === 0 && (
              <tr>
                <td colSpan={10}>No hay colaboradores para los filtros seleccionados.</td>
              </tr>
            )}

            {!isLoading &&
              colaboradores.map((colaborador) => (
                <tr key={colaborador.colaboradorId}>
                  <td>{colaborador.noEmpleado}</td>
                  <td>{colaborador.nombreCompleto}</td>
                  <td>{colaborador.cedula}</td>
                  <td>{colaborador.empresaNombre}</td>
                  <td>{colaborador.departamentoNombre}</td>
                  <td>{colaborador.cargoNombre}</td>
                  <td>{colaborador.tipoContratoNombre}</td>
                  <td>{colaborador.estatusNombre}</td>
                  <td>
                    <span className={colaborador.isActive ? 'status active' : 'status inactive'}>
                      {colaborador.isActive ? 'Activo' : 'Inactivo'}
                    </span>
                  </td>
                  <td>
                    <div className="row-actions">
                      <button type="button" onClick={() => onView(colaborador.colaboradorId)}>
                        Ver
                      </button>
                      <button type="button" onClick={() => onEdit(colaborador.colaboradorId)}>
                        Editar
                      </button>
                      <button type="button" onClick={() => onToggleActive(colaborador)}>
                        {colaborador.isActive ? 'Desactivar' : 'Activar'}
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
