import type { ColaboradorPerfil } from '../../types/colaborador';

type PerfilCardsProps = {
  perfil: ColaboradorPerfil;
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

function dateOrDash(value: string | null | undefined) {
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

function row(label: string, value: string | number | boolean | null | undefined) {
  return (
    <div className="detail-row">
      <span>{label}</span>
      <strong>{valueOrDash(value)}</strong>
    </div>
  );
}

export function PerfilResumenCard({ perfil }: PerfilCardsProps) {
  return (
    <section className="perfil-hero">
      <div>
        <span className="eyebrow">Perfil del colaborador</span>
        <h2>{perfil.datosPersonales.nombreCompleto}</h2>
        <p>
          {perfil.datosPersonales.noEmpleado} · {perfil.datosPersonales.cedula}
        </p>
      </div>
      <span className={perfil.datosLaborales.isActive ? 'status active' : 'status inactive'}>
        {perfil.datosLaborales.isActive ? 'Activo' : 'Inactivo'}
      </span>
    </section>
  );
}

export function PerfilDatosPersonales({ perfil }: PerfilCardsProps) {
  return (
    <section className="profile-card">
      <h3>Datos personales</h3>
      {row('Telefono', perfil.datosPersonales.telefono)}
      {row('Email', perfil.datosPersonales.email)}
      {row('Sexo', perfil.datosPersonales.sexo)}
      {row('Nacimiento', dateOrDash(perfil.datosPersonales.fechaNacimiento))}
      {row('Direccion', perfil.datosPersonales.direccion)}
    </section>
  );
}

export function PerfilDatosLaborales({ perfil }: PerfilCardsProps) {
  return (
    <section className="profile-card">
      <h3>Datos laborales</h3>
      {row('Empresa', perfil.datosLaborales.empresaNombre)}
      {row('Departamento', perfil.datosLaborales.departamentoNombre)}
      {row('Cargo', perfil.datosLaborales.cargoNombre)}
      {row('Jefe inmediato', perfil.datosLaborales.jefeInmediatoNombre)}
      {row('Fecha ingreso', dateOrDash(perfil.datosLaborales.fechaIngreso))}
      {row('Fecha salida', dateOrDash(perfil.datosLaborales.fechaSalida))}
      {row('Motivo salida', perfil.datosLaborales.motivoSalidaNombre)}
      {row('Vacante', perfil.datosLaborales.vacante)}
    </section>
  );
}

export function PerfilContrato({ perfil }: PerfilCardsProps) {
  return (
    <section className="profile-card">
      <h3>Contrato</h3>
      {row('Tipo contrato', perfil.contrato.tipoContratoNombre)}
      {row('Vence contrato', dateOrDash(perfil.contrato.fechaVencimientoContrato))}
      {row('Vence probatorio', dateOrDash(perfil.contrato.fechaVencimientoPeriodoProbatorio))}
    </section>
  );
}

export function PerfilVencimientos({ perfil }: PerfilCardsProps) {
  return (
    <section className="profile-card">
      <h3>Vencimientos</h3>
      {row('Vence cedula', dateOrDash(perfil.vencimientos.fechaVencimientoCedula))}
      {row('Tiene licencia', perfil.vencimientos.tieneLicencia)}
      {row('Numero licencia', perfil.vencimientos.numeroLicencia)}
      {row('Tipo licencia', perfil.vencimientos.tipoLicencia)}
      {row('Vence licencia', dateOrDash(perfil.vencimientos.fechaVencimientoLicencia))}
    </section>
  );
}

export function PerfilCompensacion({ perfil }: PerfilCardsProps) {
  return (
    <section className="profile-card">
      <h3>Compensacion</h3>
      {row('Salario', moneyOrDash(perfil.compensacion.salario))}
      {row('Viaticos', moneyOrDash(perfil.compensacion.viaticos))}
      {row('Gastos representacion', moneyOrDash(perfil.compensacion.gastosRepresentacion))}
    </section>
  );
}
