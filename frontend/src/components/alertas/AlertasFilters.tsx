import type { ColaboradorList } from '../../types/colaborador';
import type { AlertaFilterValues, EstadoAlerta, TipoAlerta } from '../../types/alerta';

type AlertasFiltersProps = {
  filters: AlertaFilterValues;
  colaboradores: ColaboradorList[];
  onChange: (filters: AlertaFilterValues) => void;
  onClear: () => void;
};

const estados: EstadoAlerta[] = ['Pendiente', 'Vencida', 'Gestionada', 'Ignorada'];
const tipos: TipoAlerta[] = ['Cedula', 'Licencia', 'Contrato', 'PeriodoProbatorio', 'Documento'];

export function AlertasFilters({
  colaboradores,
  filters,
  onChange,
  onClear,
}: AlertasFiltersProps) {
  const update = (name: keyof AlertaFilterValues, value: string | boolean) => {
    onChange({ ...filters, [name]: value });
  };

  return (
    <section className="filters-panel alertas-filters" aria-label="Filtros de alertas">
      <label>
        EstadoAlerta
        <select
          value={filters.estadoAlerta}
          onChange={(event) => update('estadoAlerta', event.target.value)}
        >
          <option value="">Todos</option>
          {estados.map((estado) => (
            <option key={estado} value={estado}>
              {estado}
            </option>
          ))}
        </select>
      </label>

      <label>
        TipoAlerta
        <select
          value={filters.tipoAlerta}
          onChange={(event) => update('tipoAlerta', event.target.value)}
        >
          <option value="">Todos</option>
          {tipos.map((tipo) => (
            <option key={tipo} value={tipo}>
              {tipo}
            </option>
          ))}
        </select>
      </label>

      <label>
        Colaborador
        <select
          value={filters.colaboradorId}
          onChange={(event) => update('colaboradorId', event.target.value)}
        >
          <option value="">Todos</option>
          {colaboradores.map((colaborador) => (
            <option key={colaborador.colaboradorId} value={colaborador.colaboradorId}>
              {colaborador.nombreCompleto}
            </option>
          ))}
        </select>
      </label>

      <label>
        Desde
        <input
          type="date"
          value={filters.desde}
          onChange={(event) => update('desde', event.target.value)}
        />
      </label>

      <label>
        Hasta
        <input
          type="date"
          value={filters.hasta}
          onChange={(event) => update('hasta', event.target.value)}
        />
      </label>

      <label className="checkbox-field filter-checkbox">
        <input
          checked={filters.incluirInactivas}
          type="checkbox"
          onChange={(event) => update('incluirInactivas', event.target.checked)}
        />
        Incluir inactivas
      </label>

      <div className="filters-actions">
        <button className="secondary-button compact-button" type="button" onClick={onClear}>
          Limpiar
        </button>
      </div>
    </section>
  );
}
