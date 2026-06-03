import type { CatalogosColaborador } from '../../types/catalogos';
import type { ColaboradorFilterValues } from '../../types/colaborador';

type ColaboradorFiltersProps = {
  catalogos: CatalogosColaborador;
  filters: ColaboradorFilterValues;
  onChange: (filters: ColaboradorFilterValues) => void;
  onClear: () => void;
};

export function ColaboradorFilters({
  catalogos,
  filters,
  onChange,
  onClear,
}: ColaboradorFiltersProps) {
  const selectedEmpresaId = Number(filters.empresaId);
  const selectedDepartamentoId = Number(filters.departamentoId);
  const departamentos = selectedEmpresaId
    ? catalogos.departamentos.filter((item) => item.empresaId === selectedEmpresaId)
    : catalogos.departamentos;
  const cargos = selectedDepartamentoId
    ? catalogos.cargos.filter((item) => item.departamentoId === selectedDepartamentoId)
    : catalogos.cargos;

  const update = (name: keyof ColaboradorFilterValues, value: string) => {
    const next = { ...filters, [name]: value };

    if (name === 'empresaId') {
      next.departamentoId = '';
      next.cargoId = '';
    }

    if (name === 'departamentoId') {
      next.cargoId = '';
    }

    onChange(next);
  };

  return (
    <section className="filters-panel" aria-label="Filtros de colaboradores">
      <label>
        Buscar
        <input
          placeholder="No. empleado, cedula o nombre"
          type="search"
          value={filters.search}
          onChange={(event) => update('search', event.target.value)}
        />
      </label>

      <label>
        Empresa
        <select value={filters.empresaId} onChange={(event) => update('empresaId', event.target.value)}>
          <option value="">Todas</option>
          {catalogos.empresas.map((empresa) => (
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
          onChange={(event) => update('departamentoId', event.target.value)}
        >
          <option value="">Todos</option>
          {departamentos.map((departamento) => (
            <option key={departamento.departamentoId} value={departamento.departamentoId}>
              {departamento.nombre}
            </option>
          ))}
        </select>
      </label>

      <label>
        Cargo
        <select value={filters.cargoId} onChange={(event) => update('cargoId', event.target.value)}>
          <option value="">Todos</option>
          {cargos.map((cargo) => (
            <option key={cargo.cargoId} value={cargo.cargoId}>
              {cargo.nombre}
            </option>
          ))}
        </select>
      </label>

      <label>
        Estatus
        <select value={filters.estatusId} onChange={(event) => update('estatusId', event.target.value)}>
          <option value="">Todos</option>
          {catalogos.estatus.map((estatus) => (
            <option key={estatus.estatusId} value={estatus.estatusId}>
              {estatus.nombre}
            </option>
          ))}
        </select>
      </label>

      <label>
        Contrato
        <select
          value={filters.tipoContratoId}
          onChange={(event) => update('tipoContratoId', event.target.value)}
        >
          <option value="">Todos</option>
          {catalogos.tiposContrato.map((tipo) => (
            <option key={tipo.tipoContratoId} value={tipo.tipoContratoId}>
              {tipo.nombre}
            </option>
          ))}
        </select>
      </label>

      <label>
        Estado
        <select
          value={filters.isActive}
          onChange={(event) => update('isActive', event.target.value)}
        >
          <option value="all">Todos</option>
          <option value="true">Activos</option>
          <option value="false">Inactivos</option>
        </select>
      </label>

      <div className="filters-actions">
        <button className="secondary-button compact-button" type="button" onClick={onClear}>
          Limpiar
        </button>
      </div>
    </section>
  );
}
