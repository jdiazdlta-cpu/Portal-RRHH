export type EstadoAlerta = 'Pendiente' | 'Gestionada' | 'Vencida' | 'Ignorada';

export type TipoAlerta =
  | 'Cedula'
  | 'Licencia'
  | 'Contrato'
  | 'PeriodoProbatorio'
  | 'Documento';

export type AlertaFilters = {
  estadoAlerta?: EstadoAlerta;
  tipoAlerta?: TipoAlerta;
  colaboradorId?: number;
  desde?: string;
  hasta?: string;
  incluirInactivas?: boolean;
};

export type AlertaFilterValues = {
  estadoAlerta: string;
  tipoAlerta: string;
  colaboradorId: string;
  desde: string;
  hasta: string;
  incluirInactivas: boolean;
};

export type AlertaList = {
  alertaId: number;
  tipoAlerta: string;
  estadoAlerta: string;
  colaboradorId: number;
  nombreCompletoColaborador: string;
  documentoColaboradorId: number | null;
  tipoDocumentoNombre: string | null;
  fechaVencimiento: string;
  mensaje: string;
  fechaGeneracion: string;
  fechaGestion: string | null;
  gestionadaPor: number | null;
  gestionadaPorNombre: string | null;
  observacionGestion: string | null;
  isActive: boolean;
};

export type AlertaResumen = {
  totalAlertas: number;
  pendientes: number;
  vencidas: number;
  gestionadas: number;
  ignoradas: number;
  porTipoAlerta: AlertaPorTipo[];
  proximasAVencer: number;
  vencidasPendientes: number;
};

export type AlertaPorTipo = {
  tipoAlerta: string;
  total: number;
};

export type GestionarAlertaRequest = {
  observacionGestion: string | null;
};

export type RecalcularAlertasResult = {
  alertasCreadas: number;
  alertasActualizadasAVencidas: number;
  totalAlertasActivas: number;
};
