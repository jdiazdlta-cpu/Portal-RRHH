export type DashboardResumen = {
  totalColaboradores: number;
  totalActivos: number;
  totalCesantes: number;
  totalVacaciones: number;
  totalServicio: number;
  totalSuspendidos: number;
  totalEmpresasActivas: number;
  totalDepartamentosActivos: number;
  totalCargosActivos: number;
  totalDocumentosActivos: number;
  totalAlertasActivas: number;
  totalAlertasPendientes: number;
  totalAlertasVencidas: number;
  totalAlertasGestionadas: number;
  totalAlertasIgnoradas: number;
};

export type DashboardVencimientos = {
  cedulasPorVencer: number;
  licenciasPorVencer: number;
  contratosPorVencer: number;
  periodosProbatoriosPorVencer: number;
  documentosPorVencer: number;
  cedulasVencidas: number;
  licenciasVencidas: number;
  contratosVencidos: number;
  periodosProbatoriosVencidos: number;
  documentosVencidos: number;
  requiereRecalculo: boolean;
};

export type ColaboradoresPorEstatus = {
  estatusId: number;
  nombre: string;
  codigo: string;
  total: number;
};

export type ColaboradoresPorDepartamento = {
  empresaId: number;
  empresaNombre: string;
  departamentoId: number;
  departamentoNombre: string;
  total: number;
};

export type AltasBajas = {
  mes: number;
  altas: number;
  bajas: number;
};

export type AltasBajasFilters = {
  anio?: number;
  mes?: number;
  empresaId?: number;
  departamentoId?: number;
};

export type AltaDetalle = {
  colaboradorId: number;
  nombreCompleto: string;
  cedula: string;
  empresaNombre: string;
  departamentoNombre: string;
  cargoNombre: string;
  fechaIngreso: string;
  tipoContratoNombre: string;
  estatusNombre: string;
};

export type BajaDetalle = {
  colaboradorId: number;
  nombreCompleto: string;
  cedula: string;
  empresaNombre: string;
  departamentoNombre: string;
  cargoNombre: string;
  fechaSalida: string;
  motivoSalidaNombre: string;
  tipoContratoNombre: string;
};

export type MovimientoColaborador = {
  colaboradorId: number;
  nombreCompleto: string;
  empresaNombre: string;
  departamentoNombre: string;
  cargoNombre: string;
  fechaIngreso: string | null;
  fechaSalida: string | null;
  estatusNombre: string;
};

export type UltimosMovimientos = {
  ultimosIngresos: MovimientoColaborador[];
  ultimasSalidas: MovimientoColaborador[];
};
