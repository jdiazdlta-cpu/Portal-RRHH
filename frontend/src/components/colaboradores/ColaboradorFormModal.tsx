import { useEffect, useMemo, useState } from 'react';
import type { CatalogosColaborador } from '../../types/catalogos';
import type {
  ColaboradorDetail,
  ColaboradorFormValues,
  ColaboradorList,
  ColaboradorRequest,
} from '../../types/colaborador';

type ColaboradorFormModalProps = {
  mode: 'create' | 'edit';
  initialValues: ColaboradorFormValues;
  catalogos: CatalogosColaborador;
  colaboradoresActivos: ColaboradorList[];
  editingId: number | null;
  isSubmitting: boolean;
  apiErrors: string[];
  onClose: () => void;
  onSubmit: (request: ColaboradorRequest) => Promise<void>;
};

type InputProps = {
  label: string;
  name: keyof ColaboradorFormValues;
  value: string;
  type?: string;
  required?: boolean;
  disabled?: boolean;
  onChange: (name: keyof ColaboradorFormValues, value: string) => void;
};

export const emptyColaboradorFormValues: ColaboradorFormValues = {
  noEmpleado: '',
  cedula: '',
  fechaVencimientoCedula: '',
  seguroSocial: '',
  primerNombre: '',
  segundoNombre: '',
  primerApellido: '',
  segundoApellido: '',
  sexo: '',
  telefono: '',
  email: '',
  fechaNacimiento: '',
  direccion: '',
  empresaId: '',
  departamentoId: '',
  cargoId: '',
  jefeInmediatoId: '',
  fechaIngreso: '',
  tipoContratoId: '',
  fechaVencimientoContrato: '',
  fechaVencimientoPeriodoProbatorio: '',
  tieneLicencia: false,
  numeroLicencia: '',
  tipoLicencia: '',
  fechaVencimientoLicencia: '',
  estatusId: '',
  salario: '',
  viaticos: '',
  gastosRepresentacion: '',
  fechaSalida: '',
  motivoSalidaId: '',
  vacante: false,
  ultimaVacacion: '',
};

function dateInput(value: string | null | undefined) {
  return value ? value.slice(0, 10) : '';
}

function decimalInput(value: number | null | undefined) {
  return value === null || value === undefined ? '' : String(value);
}

export function detailToFormValues(detail: ColaboradorDetail): ColaboradorFormValues {
  return {
    noEmpleado: detail.noEmpleado,
    cedula: detail.cedula,
    fechaVencimientoCedula: dateInput(detail.fechaVencimientoCedula),
    seguroSocial: detail.seguroSocial ?? '',
    primerNombre: detail.primerNombre,
    segundoNombre: detail.segundoNombre ?? '',
    primerApellido: detail.primerApellido,
    segundoApellido: detail.segundoApellido ?? '',
    sexo: detail.sexo ?? '',
    telefono: detail.telefono ?? '',
    email: detail.email ?? '',
    fechaNacimiento: dateInput(detail.fechaNacimiento),
    direccion: detail.direccion ?? '',
    empresaId: String(detail.empresaId),
    departamentoId: String(detail.departamentoId),
    cargoId: String(detail.cargoId),
    jefeInmediatoId: detail.jefeInmediatoId ? String(detail.jefeInmediatoId) : '',
    fechaIngreso: dateInput(detail.fechaIngreso),
    tipoContratoId: String(detail.tipoContratoId),
    fechaVencimientoContrato: dateInput(detail.fechaVencimientoContrato),
    fechaVencimientoPeriodoProbatorio: dateInput(detail.fechaVencimientoPeriodoProbatorio),
    tieneLicencia: detail.tieneLicencia,
    numeroLicencia: detail.numeroLicencia ?? '',
    tipoLicencia: detail.tipoLicencia ?? '',
    fechaVencimientoLicencia: dateInput(detail.fechaVencimientoLicencia),
    estatusId: String(detail.estatusId),
    salario: decimalInput(detail.salario),
    viaticos: decimalInput(detail.viaticos),
    gastosRepresentacion: decimalInput(detail.gastosRepresentacion),
    fechaSalida: dateInput(detail.fechaSalida),
    motivoSalidaId: detail.motivoSalidaId ? String(detail.motivoSalidaId) : '',
    vacante: detail.vacante,
    ultimaVacacion: dateInput(detail.ultimaVacacion),
  };
}

function requiredText(value: string) {
  return value.trim().length > 0;
}

function nullableText(value: string) {
  const trimmed = value.trim();
  return trimmed ? trimmed : null;
}

function nullableDate(value: string) {
  return value || null;
}

function nullableNumber(value: string) {
  return value ? Number(value) : null;
}

function requiredNumber(value: string) {
  return Number(value);
}

function validateDecimal(label: string, value: string, errors: string[]) {
  if (value && Number.isNaN(Number(value))) {
    errors.push(`${label} debe ser un numero valido.`);
  }
}

export function formValuesToRequest(values: ColaboradorFormValues): ColaboradorRequest {
  return {
    noEmpleado: values.noEmpleado.trim(),
    cedula: values.cedula.trim(),
    fechaVencimientoCedula: nullableDate(values.fechaVencimientoCedula),
    seguroSocial: nullableText(values.seguroSocial),
    primerNombre: values.primerNombre.trim(),
    segundoNombre: nullableText(values.segundoNombre),
    primerApellido: values.primerApellido.trim(),
    segundoApellido: nullableText(values.segundoApellido),
    sexo: nullableText(values.sexo),
    telefono: nullableText(values.telefono),
    email: nullableText(values.email),
    fechaNacimiento: nullableDate(values.fechaNacimiento),
    direccion: nullableText(values.direccion),
    empresaId: requiredNumber(values.empresaId),
    departamentoId: requiredNumber(values.departamentoId),
    cargoId: requiredNumber(values.cargoId),
    jefeInmediatoId: nullableNumber(values.jefeInmediatoId),
    fechaIngreso: nullableDate(values.fechaIngreso),
    tipoContratoId: requiredNumber(values.tipoContratoId),
    fechaVencimientoContrato: nullableDate(values.fechaVencimientoContrato),
    fechaVencimientoPeriodoProbatorio: nullableDate(values.fechaVencimientoPeriodoProbatorio),
    tieneLicencia: values.tieneLicencia,
    numeroLicencia: values.tieneLicencia ? nullableText(values.numeroLicencia) : null,
    tipoLicencia: values.tieneLicencia ? nullableText(values.tipoLicencia) : null,
    fechaVencimientoLicencia: values.tieneLicencia
      ? nullableDate(values.fechaVencimientoLicencia)
      : null,
    estatusId: requiredNumber(values.estatusId),
    salario: nullableNumber(values.salario),
    viaticos: nullableNumber(values.viaticos),
    gastosRepresentacion: nullableNumber(values.gastosRepresentacion),
    fechaSalida: nullableDate(values.fechaSalida),
    motivoSalidaId: nullableNumber(values.motivoSalidaId),
    vacante: values.vacante,
    ultimaVacacion: nullableDate(values.ultimaVacacion),
  };
}

function Field({
  disabled,
  label,
  name,
  onChange,
  required,
  type = 'text',
  value,
}: InputProps) {
  return (
    <label>
      {label}
      {required && <span className="required-mark">*</span>}
      <input
        disabled={disabled}
        min={type === 'number' ? '0' : undefined}
        step={type === 'number' ? '0.01' : undefined}
        type={type}
        value={value}
        onChange={(event) => onChange(name, event.target.value)}
      />
    </label>
  );
}

export function ColaboradorFormModal({
  apiErrors,
  catalogos,
  colaboradoresActivos,
  editingId,
  initialValues,
  isSubmitting,
  mode,
  onClose,
  onSubmit,
}: ColaboradorFormModalProps) {
  const [values, setValues] = useState<ColaboradorFormValues>(initialValues);
  const [localErrors, setLocalErrors] = useState<string[]>([]);

  useEffect(() => {
    setValues(initialValues);
    setLocalErrors([]);
  }, [initialValues]);

  const selectedEmpresaId = Number(values.empresaId);
  const selectedDepartamentoId = Number(values.departamentoId);
  const selectedTipoContratoId = Number(values.tipoContratoId);
  const departamentos = selectedEmpresaId
    ? catalogos.departamentos.filter((item) => item.empresaId === selectedEmpresaId)
    : [];
  const cargos = selectedDepartamentoId
    ? catalogos.cargos.filter((item) => item.departamentoId === selectedDepartamentoId)
    : [];
  const tipoContrato = catalogos.tiposContrato.find(
    (item) => item.tipoContratoId === selectedTipoContratoId,
  );
  const contratoRequiereVencimiento = Boolean(tipoContrato?.requiereFechaVencimiento);
  const jefesDisponibles = useMemo(
    () => colaboradoresActivos.filter((item) => item.colaboradorId !== editingId),
    [colaboradoresActivos, editingId],
  );

  const update = (name: keyof ColaboradorFormValues, value: string | boolean) => {
    setValues((current) => {
      const next = { ...current, [name]: value };

      if (name === 'empresaId') {
        next.departamentoId = '';
        next.cargoId = '';
      }

      if (name === 'departamentoId') {
        next.cargoId = '';
      }

      if (name === 'tieneLicencia' && value === false) {
        next.numeroLicencia = '';
        next.tipoLicencia = '';
        next.fechaVencimientoLicencia = '';
      }

      return next;
    });
  };

  const validate = () => {
    const errors: string[] = [];

    if (!requiredText(values.noEmpleado)) errors.push('NoEmpleado es obligatorio.');
    if (!requiredText(values.cedula)) errors.push('Cedula es obligatoria.');
    if (!requiredText(values.primerNombre)) errors.push('PrimerNombre es obligatorio.');
    if (!requiredText(values.primerApellido)) errors.push('PrimerApellido es obligatorio.');
    if (!values.empresaId) errors.push('Empresa es obligatoria.');
    if (!values.departamentoId) errors.push('Departamento es obligatorio.');
    if (!values.cargoId) errors.push('Cargo es obligatorio.');
    if (!values.fechaIngreso) errors.push('FechaIngreso es obligatoria.');
    if (!values.tipoContratoId) errors.push('TipoContrato es obligatorio.');
    if (!values.estatusId) errors.push('Estatus es obligatorio.');

    if (values.tieneLicencia) {
      if (!requiredText(values.numeroLicencia)) errors.push('NumeroLicencia es obligatorio.');
      if (!requiredText(values.tipoLicencia)) errors.push('TipoLicencia es obligatorio.');
      if (!values.fechaVencimientoLicencia) {
        errors.push('FechaVencimientoLicencia es obligatoria.');
      }
    }

    if (contratoRequiereVencimiento && !values.fechaVencimientoContrato) {
      errors.push('FechaVencimientoContrato es obligatoria para este tipo de contrato.');
    }

    validateDecimal('Salario', values.salario, errors);
    validateDecimal('Viaticos', values.viaticos, errors);
    validateDecimal('GastosRepresentacion', values.gastosRepresentacion, errors);

    return errors;
  };

  const handleSubmit = async () => {
    const errors = validate();
    setLocalErrors(errors);

    if (errors.length > 0) {
      return;
    }

    await onSubmit(formValuesToRequest(values));
  };

  const allErrors = [...localErrors, ...apiErrors];

  return (
    <div className="modal-backdrop" role="presentation">
      <section className="modal xl-modal" role="dialog" aria-modal="true" aria-label="Formulario de colaborador">
        <header className="modal-header">
          <div>
            <span className="eyebrow">{mode === 'create' ? 'Nuevo registro' : 'Editar registro'}</span>
            <h3>{mode === 'create' ? 'Crear colaborador' : 'Editar colaborador'}</h3>
          </div>
          <button className="icon-button" type="button" aria-label="Cerrar" onClick={onClose}>
            X
          </button>
        </header>

        {allErrors.length > 0 && (
          <div className="form-error-list">
            {allErrors.map((error) => (
              <span key={error}>{error}</span>
            ))}
          </div>
        )}

        <div className="form-sections">
          <section>
            <h4>Datos personales</h4>
            <div className="form-grid">
              <Field label="NoEmpleado" name="noEmpleado" required value={values.noEmpleado} onChange={update} />
              <Field label="Cedula" name="cedula" required value={values.cedula} onChange={update} />
              <Field
                label="Vence cedula"
                name="fechaVencimientoCedula"
                type="date"
                value={values.fechaVencimientoCedula}
                onChange={update}
              />
              <Field label="Seguro social" name="seguroSocial" value={values.seguroSocial} onChange={update} />
              <Field
                label="PrimerNombre"
                name="primerNombre"
                required
                value={values.primerNombre}
                onChange={update}
              />
              <Field label="SegundoNombre" name="segundoNombre" value={values.segundoNombre} onChange={update} />
              <Field
                label="PrimerApellido"
                name="primerApellido"
                required
                value={values.primerApellido}
                onChange={update}
              />
              <Field
                label="SegundoApellido"
                name="segundoApellido"
                value={values.segundoApellido}
                onChange={update}
              />
              <label>
                Sexo
                <select value={values.sexo} onChange={(event) => update('sexo', event.target.value)}>
                  <option value="">Sin especificar</option>
                  <option value="F">F</option>
                  <option value="M">M</option>
                </select>
              </label>
              <Field label="Telefono" name="telefono" value={values.telefono} onChange={update} />
              <Field label="Email" name="email" type="email" value={values.email} onChange={update} />
              <Field
                label="Fecha nacimiento"
                name="fechaNacimiento"
                type="date"
                value={values.fechaNacimiento}
                onChange={update}
              />
            </div>
            <label className="wide-field">
              Direccion
              <textarea value={values.direccion} onChange={(event) => update('direccion', event.target.value)} />
            </label>
          </section>

          <section>
            <h4>Datos laborales</h4>
            <div className="form-grid">
              <label>
                Empresa<span className="required-mark">*</span>
                <select value={values.empresaId} onChange={(event) => update('empresaId', event.target.value)}>
                  <option value="">Seleccione</option>
                  {catalogos.empresas.map((empresa) => (
                    <option key={empresa.empresaId} value={empresa.empresaId}>
                      {empresa.nombre}
                    </option>
                  ))}
                </select>
              </label>
              <label>
                Departamento<span className="required-mark">*</span>
                <select
                  disabled={!values.empresaId}
                  value={values.departamentoId}
                  onChange={(event) => update('departamentoId', event.target.value)}
                >
                  <option value="">Seleccione</option>
                  {departamentos.map((departamento) => (
                    <option key={departamento.departamentoId} value={departamento.departamentoId}>
                      {departamento.nombre}
                    </option>
                  ))}
                </select>
              </label>
              <label>
                Cargo<span className="required-mark">*</span>
                <select
                  disabled={!values.departamentoId}
                  value={values.cargoId}
                  onChange={(event) => update('cargoId', event.target.value)}
                >
                  <option value="">Seleccione</option>
                  {cargos.map((cargo) => (
                    <option key={cargo.cargoId} value={cargo.cargoId}>
                      {cargo.nombre}
                    </option>
                  ))}
                </select>
              </label>
              <label>
                Jefe inmediato
                <select
                  value={values.jefeInmediatoId}
                  onChange={(event) => update('jefeInmediatoId', event.target.value)}
                >
                  <option value="">Sin jefe</option>
                  {jefesDisponibles.map((jefe) => (
                    <option key={jefe.colaboradorId} value={jefe.colaboradorId}>
                      {jefe.nombreCompleto}
                    </option>
                  ))}
                </select>
              </label>
              <Field
                label="FechaIngreso"
                name="fechaIngreso"
                required
                type="date"
                value={values.fechaIngreso}
                onChange={update}
              />
              <label>
                Estatus<span className="required-mark">*</span>
                <select value={values.estatusId} onChange={(event) => update('estatusId', event.target.value)}>
                  <option value="">Seleccione</option>
                  {catalogos.estatus.map((estatus) => (
                    <option key={estatus.estatusId} value={estatus.estatusId}>
                      {estatus.nombre}
                    </option>
                  ))}
                </select>
              </label>
              <Field label="Fecha salida" name="fechaSalida" type="date" value={values.fechaSalida} onChange={update} />
              <label>
                Motivo salida
                <select
                  value={values.motivoSalidaId}
                  onChange={(event) => update('motivoSalidaId', event.target.value)}
                >
                  <option value="">Sin motivo</option>
                  {catalogos.motivosSalida.map((motivo) => (
                    <option key={motivo.motivoSalidaId} value={motivo.motivoSalidaId}>
                      {motivo.nombre}
                    </option>
                  ))}
                </select>
              </label>
            </div>
            <label className="checkbox-field">
              <input
                checked={values.vacante}
                type="checkbox"
                onChange={(event) => update('vacante', event.target.checked)}
              />
              Vacante
            </label>
          </section>

          <section>
            <h4>Contrato y vencimientos</h4>
            <div className="form-grid">
              <label>
                TipoContrato<span className="required-mark">*</span>
                <select
                  value={values.tipoContratoId}
                  onChange={(event) => update('tipoContratoId', event.target.value)}
                >
                  <option value="">Seleccione</option>
                  {catalogos.tiposContrato.map((tipo) => (
                    <option key={tipo.tipoContratoId} value={tipo.tipoContratoId}>
                      {tipo.nombre}
                    </option>
                  ))}
                </select>
              </label>
              <Field
                label={`Vence contrato${contratoRequiereVencimiento ? ' requerido' : ''}`}
                name="fechaVencimientoContrato"
                required={contratoRequiereVencimiento}
                type="date"
                value={values.fechaVencimientoContrato}
                onChange={update}
              />
              <Field
                label="Vence probatorio"
                name="fechaVencimientoPeriodoProbatorio"
                type="date"
                value={values.fechaVencimientoPeriodoProbatorio}
                onChange={update}
              />
              <Field
                label="Ultima vacacion"
                name="ultimaVacacion"
                type="date"
                value={values.ultimaVacacion}
                onChange={update}
              />
            </div>
            <label className="checkbox-field">
              <input
                checked={values.tieneLicencia}
                type="checkbox"
                onChange={(event) => update('tieneLicencia', event.target.checked)}
              />
              Tiene licencia
            </label>
            {values.tieneLicencia && (
              <div className="form-grid">
                <Field
                  label="NumeroLicencia"
                  name="numeroLicencia"
                  required
                  value={values.numeroLicencia}
                  onChange={update}
                />
                <Field
                  label="TipoLicencia"
                  name="tipoLicencia"
                  required
                  value={values.tipoLicencia}
                  onChange={update}
                />
                <Field
                  label="Vence licencia"
                  name="fechaVencimientoLicencia"
                  required
                  type="date"
                  value={values.fechaVencimientoLicencia}
                  onChange={update}
                />
              </div>
            )}
          </section>

          <section>
            <h4>Compensacion</h4>
            <div className="form-grid">
              <Field label="Salario" name="salario" type="number" value={values.salario} onChange={update} />
              <Field label="Viaticos" name="viaticos" type="number" value={values.viaticos} onChange={update} />
              <Field
                label="GastosRepresentacion"
                name="gastosRepresentacion"
                type="number"
                value={values.gastosRepresentacion}
                onChange={update}
              />
            </div>
          </section>
        </div>

        <footer className="modal-actions sticky-actions">
          <button className="secondary-button" disabled={isSubmitting} type="button" onClick={onClose}>
            Cancelar
          </button>
          <button className="primary-button" disabled={isSubmitting} type="button" onClick={handleSubmit}>
            {isSubmitting ? 'Guardando...' : 'Guardar'}
          </button>
        </footer>
      </section>
    </div>
  );
}
