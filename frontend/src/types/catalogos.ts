export type EmpresaCatalogo = {
  empresaId: number;
  nombre: string;
  ruc: string | null;
};

export type DepartamentoCatalogo = {
  departamentoId: number;
  empresaId: number;
  empresaNombre: string;
  nombre: string;
};

export type CargoCatalogo = {
  cargoId: number;
  departamentoId: number;
  departamentoNombre: string;
  empresaId: number;
  empresaNombre: string;
  nombre: string;
};

export type TipoContratoCatalogo = {
  tipoContratoId: number;
  nombre: string;
  requiereFechaVencimiento: boolean;
};

export type EstatusColaboradorCatalogo = {
  estatusId: number;
  nombre: string;
  codigo: string;
};

export type MotivoSalidaCatalogo = {
  motivoSalidaId: number;
  nombre: string;
};

export type CatalogosColaborador = {
  empresas: EmpresaCatalogo[];
  departamentos: DepartamentoCatalogo[];
  cargos: CargoCatalogo[];
  tiposContrato: TipoContratoCatalogo[];
  estatus: EstatusColaboradorCatalogo[];
  motivosSalida: MotivoSalidaCatalogo[];
};
