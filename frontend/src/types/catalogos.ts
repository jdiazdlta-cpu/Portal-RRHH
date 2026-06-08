export type EmpresaCatalogo = {
  empresaId: number;
  nombre: string;
  ruc: string | null;
};

export type RolCatalogo = {
  rolId: number;
  nombre: string;
  descripcion: string | null;
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

export type TipoDocumentoCatalogo = {
  tipoDocumentoId: number;
  nombre: string;
  tieneVencimientoSugerido: boolean;
};

export type CatalogosColaborador = {
  empresas: EmpresaCatalogo[];
  departamentos: DepartamentoCatalogo[];
  cargos: CargoCatalogo[];
  tiposContrato: TipoContratoCatalogo[];
  estatus: EstatusColaboradorCatalogo[];
  motivosSalida: MotivoSalidaCatalogo[];
};
