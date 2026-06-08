export type ColaboradorFilters = {
  empresaId?: number;
  departamentoId?: number;
  cargoId?: number;
  estatusId?: number;
  tipoContratoId?: number;
  isActive?: boolean;
  search?: string;
};

export type ColaboradorFilterValues = {
  empresaId: string;
  departamentoId: string;
  cargoId: string;
  estatusId: string;
  tipoContratoId: string;
  isActive: 'all' | 'true' | 'false';
  search: string;
};

export type ColaboradorList = {
  colaboradorId: number;
  noEmpleado: string;
  cedula: string;
  nombreCompleto: string;
  email: string | null;
  empresaId: number;
  empresaNombre: string;
  departamentoId: number;
  departamentoNombre: string;
  cargoId: number;
  cargoNombre: string;
  estatusId: number;
  estatusNombre: string;
  tipoContratoId: number;
  tipoContratoNombre: string;
  fechaIngreso: string;
  isActive: boolean;
};

export type ColaboradorDetail = {
  colaboradorId: number;
  noEmpleado: string;
  cedula: string;
  fechaVencimientoCedula: string | null;
  seguroSocial: string | null;
  primerNombre: string;
  segundoNombre: string | null;
  primerApellido: string;
  segundoApellido: string | null;
  nombreCompleto: string;
  sexo: string | null;
  telefono: string | null;
  email: string | null;
  fechaNacimiento: string | null;
  direccion: string | null;
  empresaId: number;
  empresaNombre: string;
  departamentoId: number;
  departamentoNombre: string;
  cargoId: number;
  cargoNombre: string;
  jefeInmediatoId: number | null;
  jefeInmediatoNombre: string | null;
  fechaIngreso: string;
  tipoContratoId: number;
  tipoContratoNombre: string;
  fechaVencimientoContrato: string | null;
  fechaVencimientoPeriodoProbatorio: string | null;
  tieneLicencia: boolean;
  numeroLicencia: string | null;
  tipoLicencia: string | null;
  fechaVencimientoLicencia: string | null;
  estatusId: number;
  estatusNombre: string;
  salario: number | null;
  viaticos: number | null;
  gastosRepresentacion: number | null;
  fechaSalida: string | null;
  motivoSalidaId: number | null;
  motivoSalidaNombre: string | null;
  vacante: boolean;
  ultimaVacacion: string | null;
  createdAt: string;
  updatedAt: string | null;
  createdBy: string | null;
  updatedBy: string | null;
  isActive: boolean;
};

export type ColaboradorPerfil = {
  datosPersonales: ColaboradorPerfilDatosPersonales;
  datosLaborales: ColaboradorPerfilDatosLaborales;
  contrato: ColaboradorPerfilContrato;
  vencimientos: ColaboradorPerfilVencimientos;
  compensacion: ColaboradorPerfilCompensacion;
};

export type ColaboradorPerfilDatosPersonales = {
  colaboradorId: number;
  noEmpleado: string;
  cedula: string;
  nombreCompleto: string;
  sexo: string | null;
  telefono: string | null;
  email: string | null;
  fechaNacimiento: string | null;
  direccion: string | null;
};

export type ColaboradorPerfilDatosLaborales = {
  empresaId: number;
  empresaNombre: string;
  departamentoId: number;
  departamentoNombre: string;
  cargoId: number;
  cargoNombre: string;
  jefeInmediatoId: number | null;
  jefeInmediatoNombre: string | null;
  estatusId: number;
  estatusNombre: string;
  fechaIngreso: string;
  fechaSalida: string | null;
  motivoSalidaId: number | null;
  motivoSalidaNombre: string | null;
  vacante: boolean;
  isActive: boolean;
};

export type ColaboradorPerfilContrato = {
  tipoContratoId: number;
  tipoContratoNombre: string;
  fechaVencimientoContrato: string | null;
  fechaVencimientoPeriodoProbatorio: string | null;
};

export type ColaboradorPerfilVencimientos = {
  fechaVencimientoCedula: string | null;
  tieneLicencia: boolean;
  numeroLicencia: string | null;
  tipoLicencia: string | null;
  fechaVencimientoLicencia: string | null;
  fechaVencimientoContrato: string | null;
  fechaVencimientoPeriodoProbatorio: string | null;
};

export type ColaboradorPerfilCompensacion = {
  salario: number | null;
  viaticos: number | null;
  gastosRepresentacion: number | null;
};

export type HistorialColaborador = {
  historialColaboradorId: number;
  colaboradorId: number;
  usuarioId: number;
  usuarioNombre: string;
  accion: string;
  campo: string | null;
  valorAnterior: string | null;
  valorNuevo: string | null;
  fecha: string;
  observacion: string | null;
};

export type ColaboradorRequest = {
  noEmpleado: string;
  cedula: string;
  fechaVencimientoCedula: string | null;
  seguroSocial: string | null;
  primerNombre: string;
  segundoNombre: string | null;
  primerApellido: string;
  segundoApellido: string | null;
  sexo: string | null;
  telefono: string | null;
  email: string | null;
  fechaNacimiento: string | null;
  direccion: string | null;
  empresaId: number;
  departamentoId: number;
  cargoId: number;
  jefeInmediatoId: number | null;
  fechaIngreso: string | null;
  tipoContratoId: number;
  fechaVencimientoContrato: string | null;
  fechaVencimientoPeriodoProbatorio: string | null;
  tieneLicencia: boolean;
  numeroLicencia: string | null;
  tipoLicencia: string | null;
  fechaVencimientoLicencia: string | null;
  estatusId: number;
  salario: number | null;
  viaticos: number | null;
  gastosRepresentacion: number | null;
  fechaSalida: string | null;
  motivoSalidaId: number | null;
  vacante: boolean;
  ultimaVacacion: string | null;
};

export type ColaboradorFormValues = {
  noEmpleado: string;
  cedula: string;
  fechaVencimientoCedula: string;
  seguroSocial: string;
  primerNombre: string;
  segundoNombre: string;
  primerApellido: string;
  segundoApellido: string;
  sexo: string;
  telefono: string;
  email: string;
  fechaNacimiento: string;
  direccion: string;
  empresaId: string;
  departamentoId: string;
  cargoId: string;
  jefeInmediatoId: string;
  fechaIngreso: string;
  tipoContratoId: string;
  fechaVencimientoContrato: string;
  fechaVencimientoPeriodoProbatorio: string;
  tieneLicencia: boolean;
  numeroLicencia: string;
  tipoLicencia: string;
  fechaVencimientoLicencia: string;
  estatusId: string;
  salario: string;
  viaticos: string;
  gastosRepresentacion: string;
  fechaSalida: string;
  motivoSalidaId: string;
  vacante: boolean;
  ultimaVacacion: string;
};
