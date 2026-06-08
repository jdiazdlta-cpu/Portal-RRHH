import { apiBlobRequest, apiFormRequest, apiRequest } from './httpClient';
import type {
  DocumentoColaboradorDetail,
  DocumentoColaboradorList,
  UpdateDocumentoRequest,
  UploadDocumentoRequest,
} from '../types/documento';

export function getDocumentosColaborador(colaboradorId: number) {
  return apiRequest<DocumentoColaboradorList[]>(`/colaboradores/${colaboradorId}/documentos`);
}

export function getDocumentoById(id: number) {
  return apiRequest<DocumentoColaboradorDetail>(`/documentos/${id}`);
}

export function uploadDocumento(colaboradorId: number, request: UploadDocumentoRequest) {
  const formData = new FormData();
  formData.append('archivo', request.archivo);
  formData.append('tipoDocumentoId', String(request.tipoDocumentoId));
  formData.append('tieneVencimiento', String(request.tieneVencimiento));

  if (request.fechaVencimiento) {
    formData.append('fechaVencimiento', request.fechaVencimiento);
  }

  if (request.observacion) {
    formData.append('observacion', request.observacion);
  }

  return apiFormRequest<DocumentoColaboradorDetail>(
    `/colaboradores/${colaboradorId}/documentos`,
    formData,
    { method: 'POST' },
  );
}

export function updateDocumento(id: number, request: UpdateDocumentoRequest) {
  return apiRequest<DocumentoColaboradorDetail>(`/documentos/${id}`, {
    method: 'PUT',
    body: request,
  });
}

export function desactivarDocumento(id: number) {
  return apiRequest<DocumentoColaboradorDetail>(`/documentos/${id}/desactivar`, {
    method: 'PATCH',
  });
}

export function descargarDocumento(id: number) {
  return apiBlobRequest(`/documentos/${id}/descargar`);
}
