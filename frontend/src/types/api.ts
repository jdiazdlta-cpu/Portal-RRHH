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

export type PosibleJefe = {
  colaboradorId: number;
  noEmpleado: string;
  nombreCompleto: string;
  empresa: string;
  departamento: string;
  cargo: string;
};

export type ColaboradorLookup = {
  colaboradorId: number;
  noEmpleado: string;
  nombreCompleto: string;
  empresaId: number;
  empresa: string;
  departamentoId: number;
  departamento: string;
  cargoId: number;
  cargo: string;
  estatus: string;
};

export type ColaboradorSelect = {
  colaboradorId: number;
  noEmpleado: string;
  nombreCompleto: string;
  cedula: string;
  empresaId: number;
  empresa: string;
  departamentoId: number;
  departamento: string;
  cargoId: number;
  cargo: string;
  jefeInmediatoId?: number | null;
  jefeInmediato?: string | null;
  tipoContratoId: number;
  tipoContrato: string;
  estatusId: number;
  estatus: string;
  estatusCodigo: string;
  salarioActual?: number | null;
  viaticosActual?: number | null;
  gastosRepresentacionActual?: number | null;
};

export type ColaboradorResumenLaboral = ColaboradorSelect;

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

export type TipoSolicitudDisponible = {
  tipo: string;
  nombre: string;
  disponible: boolean;
  estado: string;
};

export type SolicitudList = {
  solicitudId: number;
  codigoSolicitud: string;
  tipoSolicitud: string;
  estado: string;
  solicitante: string;
  empresa?: string | null;
  departamento?: string | null;
  fechaSolicitud: string;
  ultimaActualizacion?: string | null;
};

export type RequisicionPersonal = {
  requisicionPersonalId: number;
  solicitudId: number;
  cargoSolicitado: string;
  departamentoSolicitadoId?: number | null;
  departamentoSolicitado?: string | null;
  numeroPlazas: number;
  dependenciaJerarquica?: string | null;
  principalesResponsabilidades?: string | null;
  funcionesEspecificas?: string | null;
  equipoACargo?: string | null;
  centroTrabajo?: string | null;
  salario?: number | null;
  gastoRepresentacion?: number | null;
  salarioVariable?: number | null;
  otrosConceptos?: string | null;
  esPosicionNueva: boolean;
  esReemplazo: boolean;
  colaboradorReemplazadoId?: number | null;
  colaboradorReemplazado?: string | null;
  nombrePersonaReemplazada?: string | null;
  tipoContratoId?: number | null;
  tipoContrato?: string | null;
  periodoPrueba?: string | null;
  formacionRequerida?: string | null;
  formacionComplementaria?: string | null;
  conocimientosTecnicos?: string | null;
  conocimientosValorados?: string | null;
  idiomaNivel?: string | null;
  aniosExperiencia?: number | null;
  funcionesExperiencia?: string | null;
  areaSectorExperiencia?: string | null;
  experienciaValorable?: string | null;
  edadMinima?: number | null;
  edadMaxima?: number | null;
  sexoPreferido?: string | null;
  caracteristicasPersonales?: string | null;
  fechaAperturaProceso?: string | null;
  fechaEntregaCandidatos?: string | null;
  solicitadoPorTexto?: string | null;
  autorizadoPorTexto?: string | null;
  fechaAutorizacion?: string | null;
};

export type TipoAccionPersonalDisponible = {
  tipo: string;
  nombre: string;
  requiereColaborador: boolean;
};

export type AccionPersonalCambioAplicado = {
  accionPersonalCambioAplicadoId: number;
  campo: string;
  valorAnterior?: string | null;
  valorNuevo?: string | null;
  fecha: string;
  usuarioId: number;
  usuario: string;
};

export type AccionPersonal = {
  accionPersonalId: number;
  solicitudId: number;
  tipoAccion: string;
  tipoAccionNombre: string;
  colaboradorId?: number | null;
  colaborador?: string | null;
  noEmpleadoSnapshot?: string | null;
  cedulaSnapshot?: string | null;
  fechaEfectiva: string;
  justificacion: string;
  observaciones?: string | null;
  empresaActualId?: number | null;
  empresaActual?: string | null;
  departamentoActualId?: number | null;
  departamentoActual?: string | null;
  cargoActualId?: number | null;
  cargoActual?: string | null;
  jefeActualId?: number | null;
  jefeActual?: string | null;
  tipoContratoActualId?: number | null;
  tipoContratoActual?: string | null;
  estatusActualId?: number | null;
  estatusActual?: string | null;
  salarioActual?: number | null;
  viaticosActual?: number | null;
  gastosRepresentacionActual?: number | null;
  diasVacaciones?: number | null;
  fechaInicioVacaciones?: string | null;
  fechaFinVacaciones?: string | null;
  periodoVacacionesDesde?: string | null;
  periodoVacacionesHasta?: string | null;
  quienReemplaza?: string | null;
  tipoContratoNuevoId?: number | null;
  tipoContratoNuevo?: string | null;
  fechaInicioContrato?: string | null;
  fechaFinContrato?: string | null;
  esReemplazo?: boolean | null;
  esPosicionNueva?: boolean | null;
  salarioNuevo?: number | null;
  viaticosNuevo?: number | null;
  gastosRepresentacionNuevo?: number | null;
  otrosBeneficios?: string | null;
  salarioAnterior?: number | null;
  salarioNuevoAjuste?: number | null;
  ajustePorMes?: number | null;
  motivoAjuste?: string | null;
  cargoNuevoId?: number | null;
  cargoNuevo?: string | null;
  departamentoNuevoId?: number | null;
  departamentoNuevo?: string | null;
  empresaNuevaId?: number | null;
  empresaNueva?: string | null;
  jefeNuevoId?: number | null;
  jefeNuevo?: string | null;
  cargoTrasladoActualId?: number | null;
  cargoTrasladoActual?: string | null;
  cargoTrasladoNuevoId?: number | null;
  cargoTrasladoNuevo?: string | null;
  departamentoTrasladoActualId?: number | null;
  departamentoTrasladoActual?: string | null;
  departamentoTrasladoNuevoId?: number | null;
  departamentoTrasladoNuevo?: string | null;
  empresaTrasladoActualId?: number | null;
  empresaTrasladoActual?: string | null;
  empresaTrasladoNuevaId?: number | null;
  empresaTrasladoNueva?: string | null;
  jefeTrasladoNuevoId?: number | null;
  jefeTrasladoNuevo?: string | null;
  tipoLicenciaAccion?: string | null;
  licenciaRemunerada?: boolean | null;
  fechaInicioLicencia?: string | null;
  fechaFinLicencia?: string | null;
  especificacionLicencia?: string | null;
  tipoFinalizacion?: string | null;
  fechaSalida?: string | null;
  motivoSalidaId?: number | null;
  motivoSalida?: string | null;
  menosDeDosAnios?: boolean | null;
  terminacionPeriodoPrueba?: boolean | null;
  causaJustificada?: boolean | null;
  mutuoAcuerdo?: boolean | null;
  renovacionExtensionContrato?: boolean | null;
  continuidadLaboral?: boolean | null;
  loRecomienda?: boolean | null;
  puntualidad?: string | null;
  honestidad?: string | null;
  trabajoEquipo?: string | null;
  productividad?: string | null;
  iniciativa?: string | null;
  respetoJefe?: string | null;
  respetoCompaneros?: string | null;
  ejecutada: boolean;
  fechaEjecucion?: string | null;
  ejecutadaPorUsuarioId?: number | null;
  ejecutadaPorUsuario?: string | null;
  resultadoEjecucion?: string | null;
  errorEjecucion?: string | null;
  cambiosAplicados: AccionPersonalCambioAplicado[];
};

export type SolicitudAprobacion = {
  solicitudAprobacionId: number;
  orden: number;
  etapa: string;
  rolAprobador: string;
  usuarioAprobadorId?: number | null;
  usuarioAprobador?: string | null;
  colaboradorAprobadorId?: number | null;
  colaboradorAprobador?: string | null;
  departamentoResponsableId?: number | null;
  tipoResponsable?: string | null;
  estado: string;
  fechaDecision?: string | null;
  comentario?: string | null;
};

export type SolicitudHistorial = {
  solicitudHistorialId: number;
  accion: string;
  estadoAnterior?: string | null;
  estadoNuevo?: string | null;
  comentario?: string | null;
  usuarioId: number;
  usuario: string;
  fecha: string;
};

export type SolicitudDetail = {
  solicitudId: number;
  codigoSolicitud: string;
  tipoSolicitud: string;
  estado: string;
  solicitanteUsuarioId: number;
  solicitante: string;
  colaboradorId?: number | null;
  empresaId?: number | null;
  empresa?: string | null;
  departamentoId?: number | null;
  departamento?: string | null;
  cargoId?: number | null;
  cargo?: string | null;
  fechaSolicitud: string;
  fechaEfectiva?: string | null;
  justificacion?: string | null;
  observaciones?: string | null;
  createdAt: string;
  updatedAt?: string | null;
  requisicion?: RequisicionPersonal | null;
  accionPersonal?: AccionPersonal | null;
  aprobaciones: SolicitudAprobacion[];
  historial: SolicitudHistorial[];
  accionesDisponibles: string[];
};

export type RequisicionPersonalRequest = {
  empresaId?: number | null;
  departamentoSolicitadoId?: number | null;
  fechaEfectiva?: string | null;
  justificacion?: string | null;
  observaciones?: string | null;
  liderAprobadorUsuarioId?: number | null;
  liderAprobadorColaboradorId?: number | null;
  departamentoResponsableId?: number | null;
  cargoSolicitado: string;
  numeroPlazas: number;
  dependenciaJerarquica?: string | null;
  principalesResponsabilidades?: string | null;
  funcionesEspecificas?: string | null;
  equipoACargo?: string | null;
  centroTrabajo?: string | null;
  salario?: number | null;
  gastoRepresentacion?: number | null;
  salarioVariable?: number | null;
  otrosConceptos?: string | null;
  esPosicionNueva: boolean;
  esReemplazo: boolean;
  colaboradorReemplazadoId?: number | null;
  nombrePersonaReemplazada?: string | null;
  tipoContratoId?: number | null;
  periodoPrueba?: string | null;
  formacionRequerida?: string | null;
  formacionComplementaria?: string | null;
  conocimientosTecnicos?: string | null;
  conocimientosValorados?: string | null;
  idiomaNivel?: string | null;
  aniosExperiencia?: number | null;
  funcionesExperiencia?: string | null;
  areaSectorExperiencia?: string | null;
  experienciaValorable?: string | null;
  edadMinima?: number | null;
  edadMaxima?: number | null;
  sexoPreferido?: string | null;
  caracteristicasPersonales?: string | null;
  fechaAperturaProceso?: string | null;
  fechaEntregaCandidatos?: string | null;
  solicitadoPorTexto?: string | null;
  autorizadoPorTexto?: string | null;
  fechaAutorizacion?: string | null;
  enviar?: boolean;
};

export type AccionPersonalRequest = {
  tipoAccion: string;
  colaboradorId?: number | null;
  empresaId?: number | null;
  departamentoId?: number | null;
  cargoId?: number | null;
  fechaEfectiva?: string | null;
  justificacion?: string | null;
  observaciones?: string | null;
  liderAprobadorUsuarioId?: number | null;
  liderAprobadorColaboradorId?: number | null;
  departamentoResponsableId?: number | null;
  diasVacaciones?: number | null;
  fechaInicioVacaciones?: string | null;
  fechaFinVacaciones?: string | null;
  periodoVacacionesDesde?: string | null;
  periodoVacacionesHasta?: string | null;
  quienReemplaza?: string | null;
  tipoContratoNuevoId?: number | null;
  fechaInicioContrato?: string | null;
  fechaFinContrato?: string | null;
  esReemplazo?: boolean | null;
  esPosicionNueva?: boolean | null;
  salarioNuevo?: number | null;
  viaticosNuevo?: number | null;
  gastosRepresentacionNuevo?: number | null;
  otrosBeneficios?: string | null;
  salarioNuevoAjuste?: number | null;
  ajustePorMes?: number | null;
  motivoAjuste?: string | null;
  cargoNuevoId?: number | null;
  departamentoNuevoId?: number | null;
  empresaNuevaId?: number | null;
  jefeNuevoId?: number | null;
  cargoTrasladoNuevoId?: number | null;
  departamentoTrasladoNuevoId?: number | null;
  empresaTrasladoNuevaId?: number | null;
  jefeTrasladoNuevoId?: number | null;
  tipoLicenciaAccion?: string | null;
  licenciaRemunerada?: boolean | null;
  fechaInicioLicencia?: string | null;
  fechaFinLicencia?: string | null;
  especificacionLicencia?: string | null;
  tipoFinalizacion?: string | null;
  fechaSalida?: string | null;
  motivoSalidaId?: number | null;
  menosDeDosAnios?: boolean | null;
  terminacionPeriodoPrueba?: boolean | null;
  causaJustificada?: boolean | null;
  mutuoAcuerdo?: boolean | null;
  renovacionExtensionContrato?: boolean | null;
  continuidadLaboral?: boolean | null;
  loRecomienda?: boolean | null;
  puntualidad?: string | null;
  honestidad?: string | null;
  trabajoEquipo?: string | null;
  productividad?: string | null;
  iniciativa?: string | null;
  respetoJefe?: string | null;
  respetoCompaneros?: string | null;
  enviar?: boolean;
};

export type OrganigramaList = {
  organigramaId: number;
  nombre: string;
  empresaId?: number | null;
  empresa?: string | null;
  descripcion?: string | null;
  fechaInicio: string;
  fechaFin?: string | null;
  nodos: number;
  isActive: boolean;
};

export type OrganigramaDetail = {
  organigramaId: number;
  nombre: string;
  empresaId?: number | null;
  empresa?: string | null;
  descripcion?: string | null;
  fechaInicio: string;
  fechaFin?: string | null;
  isActive: boolean;
  nodos: OrganigramaNodo[];
  historial: OrganigramaHistorialCambio[];
};

export type OrganigramaNodo = {
  organigramaNodoId: number;
  organigramaId: number;
  nodoPadreId?: number | null;
  nodoPadre?: string | null;
  nombreNodo: string;
  empresaId?: number | null;
  empresa?: string | null;
  departamentoId?: number | null;
  departamento?: string | null;
  cargoId?: number | null;
  cargo?: string | null;
  nivel: number;
  orden: number;
  esRolOperativo: boolean;
  colaboradoresActivos: number;
  descripcion?: string | null;
  isActive: boolean;
};

export type DepartamentoResponsable = {
  departamentoResponsableId: number;
  empresaId: number;
  empresa: string;
  departamentoId: number;
  departamento: string;
  colaboradorResponsableId: number;
  noEmpleado: string;
  colaboradorResponsable: string;
  usuarioResponsableId?: number | null;
  usuarioResponsable?: string | null;
  tipoResponsable: string;
  esPrincipal: boolean;
  puedeAprobarSolicitudes: boolean;
  fechaInicio: string;
  fechaFin?: string | null;
  observacion?: string | null;
  isActive: boolean;
  advertencias: string[];
};

export type AprobadorSolicitud = {
  departamentoResponsableId: number;
  empresaId: number;
  empresa: string;
  departamentoId: number;
  departamento: string;
  colaboradorResponsableId: number;
  noEmpleado: string;
  nombreCompleto: string;
  cargo: string;
  tipoResponsable: string;
  usuarioResponsableId?: number | null;
  usuarioResponsable?: string | null;
  esPrincipal: boolean;
  advertencias: string[];
};

export type OrganigramaHistorialCambio = {
  organigramaHistorialCambioId: number;
  entidad: string;
  entidadId: number;
  accion: string;
  valorAnterior?: string | null;
  valorNuevo?: string | null;
  usuarioId: number;
  usuario: string;
  fecha: string;
  comentario?: string | null;
};

export type OrganigramaRequest = {
  nombre: string;
  empresaId?: number | null;
  descripcion?: string | null;
  fechaInicio: string;
  fechaFin?: string | null;
  isActive: boolean;
};

export type OrganigramaNodoRequest = {
  empresaId?: number | null;
  departamentoId?: number | null;
  cargoId?: number | null;
  nodoPadreId?: number | null;
  nombreNodo: string;
  descripcion?: string | null;
  nivel: number;
  orden: number;
  esRolOperativo: boolean;
  isActive: boolean;
};

export type DepartamentoResponsableRequest = {
  empresaId: number;
  departamentoId: number;
  colaboradorResponsableId: number;
  usuarioResponsableId?: number | null;
  tipoResponsable: string;
  esPrincipal: boolean;
  puedeAprobarSolicitudes: boolean;
  fechaInicio: string;
  fechaFin?: string | null;
  observacion?: string | null;
  isActive: boolean;
};
