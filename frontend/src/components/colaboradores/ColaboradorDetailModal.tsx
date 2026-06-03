import type { ColaboradorDetail } from '../../types/colaborador';

type ColaboradorDetailModalProps = {
  colaborador: ColaboradorDetail;
  onClose: () => void;
};

function valueOrDash(value: string | number | boolean | null | undefined) {
  if (value === null || value === undefined || value === '') {
    return '-';
  }

  if (typeof value === 'boolean') {
    return value ? 'Si' : 'No';
  }

  return value;
}

function dateOrDash(value: string | null) {
  if (!value) {
    return '-';
  }

  return new Intl.DateTimeFormat('es-PA').format(new Date(value));
}

function moneyOrDash(value: number | null) {
  if (value === null) {
    return '-';
  }

  return new Intl.NumberFormat('es-PA', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(value);
}

function detailRow(label: string, value: string | number | boolean | null | undefined) {
  return (
    <div className="detail-row">
      <span>{label}</span>
      <strong>{valueOrDash(value)}</strong>
    </div>
  );
}

export function ColaboradorDetailModal({ colaborador, onClose }: ColaboradorDetailModalProps) {
  return (
    <div className="modal-backdrop" role="presentation">
      <section className="modal large-modal" role="dialog" aria-modal="true" aria-label="Detalle de colaborador">
        <header className="modal-header">
          <div>
            <span className="eyebrow">Detalle basico</span>
            <h3>{colaborador.nombreCompleto}</h3>
          </div>
          <button className="icon-button" type="button" aria-label="Cerrar" onClick={onClose}>
            X
          </button>
        </header>

        <div className="detail-grid">
          <section>
            <h4>Datos personales</h4>
            {detailRow('NoEmpleado', colaborador.noEmpleado)}
            {detailRow('Cedula', colaborador.cedula)}
            {detailRow('Seguro social', colaborador.seguroSocial)}
            {detailRow('Sexo', colaborador.sexo)}
            {detailRow('Telefono', colaborador.telefono)}
            {detailRow('Email', colaborador.email)}
            {detailRow('Nacimiento', dateOrDash(colaborador.fechaNacimiento))}
            {detailRow('Direccion', colaborador.direccion)}
          </section>

          <section>
            <h4>Datos laborales</h4>
            {detailRow('Empresa', colaborador.empresaNombre)}
            {detailRow('Departamento', colaborador.departamentoNombre)}
            {detailRow('Cargo', colaborador.cargoNombre)}
            {detailRow('Jefe inmediato', colaborador.jefeInmediatoNombre)}
            {detailRow('Fecha ingreso', dateOrDash(colaborador.fechaIngreso))}
            {detailRow('Fecha salida', dateOrDash(colaborador.fechaSalida))}
            {detailRow('Motivo salida', colaborador.motivoSalidaNombre)}
            {detailRow('Vacante', colaborador.vacante)}
          </section>

          <section>
            <h4>Contrato</h4>
            {detailRow('Tipo contrato', colaborador.tipoContratoNombre)}
            {detailRow('Vence contrato', dateOrDash(colaborador.fechaVencimientoContrato))}
            {detailRow('Vence probatorio', dateOrDash(colaborador.fechaVencimientoPeriodoProbatorio))}
          </section>

          <section>
            <h4>Vencimientos</h4>
            {detailRow('Vence cedula', dateOrDash(colaborador.fechaVencimientoCedula))}
            {detailRow('Tiene licencia', colaborador.tieneLicencia)}
            {detailRow('Numero licencia', colaborador.numeroLicencia)}
            {detailRow('Tipo licencia', colaborador.tipoLicencia)}
            {detailRow('Vence licencia', dateOrDash(colaborador.fechaVencimientoLicencia))}
            {detailRow('Ultima vacacion', dateOrDash(colaborador.ultimaVacacion))}
          </section>

          <section>
            <h4>Compensacion</h4>
            {detailRow('Salario', moneyOrDash(colaborador.salario))}
            {detailRow('Viaticos', moneyOrDash(colaborador.viaticos))}
            {detailRow('Gastos representacion', moneyOrDash(colaborador.gastosRepresentacion))}
          </section>

          <section>
            <h4>Estado</h4>
            {detailRow('Estatus', colaborador.estatusNombre)}
            {detailRow('Activo', colaborador.isActive)}
            {detailRow('Creado por', colaborador.createdBy)}
            {detailRow('Actualizado por', colaborador.updatedBy)}
          </section>
        </div>
      </section>
    </div>
  );
}
