export type Role = 'Admin' | 'RRHH' | 'Supervisor' | 'Consulta';

export type ApiResponse<T> = {
  success: boolean;
  message: string;
  data: T;
};

export type CurrentUser = {
  usuarioId: number;
  nombreUsuario: string;
  email: string;
  rol: Role;
};

export type AuthResult = {
  token: string;
  expiresAt: string;
  usuario: CurrentUser;
};

export type CatalogoItem = {
  id: number;
  nombre: string;
  codigo?: string | null;
  requiereFechaVencimiento?: boolean | null;
  tieneVencimientoSugerido?: boolean | null;
};

export type RolDto = {
  rolId: number;
  nombre: Role;
  descripcion?: string | null;
};

export type PagedResult<T> = {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
};

export type DashboardResumen = {
  totalColaboradores: number;
  activos: number;
  cesantes: number;
  vacaciones: number;
  servicio: number;
  alertasActivas: number;
  vencimientos: number;
};

export type ChartItem = {
  label: string;
  value: number;
};

export type AltasBajas = {
  periodo: string;
  altas: number;
  bajas: number;
};

export type Movimiento = {
  historialColaboradorId: number;
  colaborador: string;
  usuario: string;
  accion: string;
  fecha: string;
  observacion?: string | null;
};

export type Alerta = {
  alertaId: number;
  tipoAlerta: string;
  estadoAlerta: string;
  colaboradorId: number;
  colaborador: string;
  documentoColaboradorId?: number | null;
  fechaVencimiento: string;
  mensaje: string;
  fechaGeneracion: string;
  fechaGestion?: string | null;
  observacionGestion?: string | null;
  empresa: string;
  diasRestantes: number;
  diasVencidos: number;
};

export type RecordatorioDocumento = {
  alertaId: number;
  colaboradorId: number;
  colaborador: string;
  empresa: string;
  tipoVencimiento: string;
  fechaVencimiento: string;
  diasRestantes: number;
};

export type ColaboradorList = {
  colaboradorId: number;
  noEmpleado: string;
  cedula: string;
  nombreCompleto: string;
  empresa: string;
  departamento: string;
  cargo: string;
  estatus: string;
  fechaIngreso: string;
  fechaSalida?: string | null;
  isActive: boolean;
};

export type Documento = {
  documentoColaboradorId: number;
  tipoDocumentoId: number;
  tipoDocumento: string;
  colaboradorId: number;
  nombreArchivo: string;
  rutaArchivo: string;
  fechaCarga: string;
  tieneVencimiento: boolean;
  fechaVencimiento?: string | null;
  observacion?: string | null;
  isActive: boolean;
};

export type ColaboradorDetalle = {
  colaboradorId: number;
  noEmpleado: string;
  cedula: string;
  fechaVencimientoCedula?: string | null;
  seguroSocial?: string | null;
  primerNombre: string;
  segundoNombre?: string | null;
  primerApellido: string;
  segundoApellido?: string | null;
  nombreCompleto: string;
  sexo?: string | null;
  telefono?: string | null;
  email?: string | null;
  fechaNacimiento?: string | null;
  direccion?: string | null;
  empresaId: number;
  empresa: string;
  departamentoId: number;
  departamento: string;
  cargoId: number;
  cargo: string;
  jefeInmediatoId?: number | null;
  jefeInmediato?: string | null;
  fechaIngreso: string;
  tipoContratoId: number;
  tipoContrato: string;
  fechaVencimientoContrato?: string | null;
  fechaVencimientoPeriodoProbatorio?: string | null;
  tieneLicencia: boolean;
  numeroLicencia?: string | null;
  tipoLicencia?: string | null;
  fechaVencimientoLicencia?: string | null;
  estatusId: number;
  estatus: string;
  salario: number;
  viaticos: number;
  gastosRepresentacion: number;
  fechaSalida?: string | null;
  motivoSalidaId?: number | null;
  motivoSalida?: string | null;
  vacante: boolean;
  ultimaVacacion?: string | null;
  isActive: boolean;
  documentos: Documento[];
  alertas: Alerta[];
};

export type ColaboradorUpsert = {
  noEmpleado: string;
  cedula: string;
  fechaVencimientoCedula?: string | null;
  seguroSocial?: string | null;
  primerNombre: string;
  segundoNombre?: string | null;
  primerApellido: string;
  segundoApellido?: string | null;
  sexo?: string | null;
  telefono?: string | null;
  email?: string | null;
  fechaNacimiento?: string | null;
  direccion?: string | null;
  empresaId: number;
  departamentoId: number;
  cargoId: number;
  jefeInmediatoId?: number | null;
  fechaIngreso: string;
  tipoContratoId: number;
  fechaVencimientoContrato?: string | null;
  fechaVencimientoPeriodoProbatorio?: string | null;
  tieneLicencia: boolean;
  numeroLicencia?: string | null;
  tipoLicencia?: string | null;
  fechaVencimientoLicencia?: string | null;
  estatusId: number;
  salario: number;
  viaticos: number;
  gastosRepresentacion: number;
  fechaSalida?: string | null;
  motivoSalidaId?: number | null;
  vacante: boolean;
  ultimaVacacion?: string | null;
};

export type AlertaGestionCorreccion = {
  observacionGestion: string;
  gestionarSinCambio?: boolean;
  resultadoGestionContrato?: string | null;
  fechaVencimientoCedula?: string | null;
  tieneLicencia?: boolean | null;
  numeroLicencia?: string | null;
  tipoLicencia?: string | null;
  fechaVencimientoLicencia?: string | null;
  tipoContratoId?: number | null;
  fechaVencimientoContrato?: string | null;
  nuevaFechaVencimientoContrato?: string | null;
  estatusId?: number | null;
  motivoSalidaId?: number | null;
  fechaSalida?: string | null;
  fechaVencimientoPeriodoProbatorio?: string | null;
  fechaVencimientoDocumento?: string | null;
  observacionDocumento?: string | null;
};

export type Usuario = {
  usuarioId: number;
  nombreUsuario: string;
  email: string;
  rolId: number;
  rol: Role;
  ultimoAcceso?: string | null;
  isActive: boolean;
};

export type Empresa = {
  empresaId: number;
  nombre: string;
  ruc?: string | null;
  isActive: boolean;
};

export type Departamento = {
  departamentoId: number;
  empresaId: number;
  empresa: string;
  nombre: string;
  isActive: boolean;
};

export type Cargo = {
  cargoId: number;
  departamentoId: number;
  departamento: string;
  empresaId: number;
  empresa: string;
  nombre: string;
  isActive: boolean;
};
