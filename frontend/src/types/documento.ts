export type DocumentoColaboradorList = {
  documentoColaboradorId: number;
  tipoDocumentoId: number;
  tipoDocumentoNombre: string;
  nombreArchivo: string;
  fechaCarga: string;
  fechaVencimiento: string | null;
  tieneVencimiento: boolean;
  observacion: string | null;
  isActive: boolean;
  subidoPor: number;
  subidoPorNombre: string;
};

export type DocumentoColaboradorDetail = {
  documentoColaboradorId: number;
  colaboradorId: number;
  colaboradorNombre: string;
  tipoDocumentoId: number;
  tipoDocumentoNombre: string;
  nombreArchivo: string;
  rutaArchivo: string;
  fechaCarga: string;
  fechaVencimiento: string | null;
  tieneVencimiento: boolean;
  observacion: string | null;
  subidoPor: number;
  subidoPorNombre: string;
  createdAt: string;
  updatedAt: string | null;
  createdBy: string | null;
  updatedBy: string | null;
  isActive: boolean;
};

export type UploadDocumentoRequest = {
  archivo: File;
  tipoDocumentoId: number;
  tieneVencimiento: boolean;
  fechaVencimiento: string | null;
  observacion: string | null;
};

export type UpdateDocumentoRequest = {
  tipoDocumentoId: number;
  tieneVencimiento: boolean;
  fechaVencimiento: string | null;
  observacion: string | null;
  isActive: boolean;
};

export type DocumentoFormValues = {
  tipoDocumentoId: string;
  tieneVencimiento: boolean;
  fechaVencimiento: string;
  observacion: string;
  isActive: boolean;
};
