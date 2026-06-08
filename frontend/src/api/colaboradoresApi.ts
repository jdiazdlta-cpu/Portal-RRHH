import { apiRequest } from './httpClient';
import type {
  ColaboradorDetail,
  ColaboradorFilters,
  ColaboradorList,
  ColaboradorPerfil,
  ColaboradorRequest,
  HistorialColaborador,
} from '../types/colaborador';

function buildQuery(filters: ColaboradorFilters = {}) {
  const params = new URLSearchParams();

  if (filters.empresaId) params.set('empresaId', String(filters.empresaId));
  if (filters.departamentoId) params.set('departamentoId', String(filters.departamentoId));
  if (filters.cargoId) params.set('cargoId', String(filters.cargoId));
  if (filters.estatusId) params.set('estatusId', String(filters.estatusId));
  if (filters.tipoContratoId) params.set('tipoContratoId', String(filters.tipoContratoId));
  if (filters.isActive !== undefined) params.set('isActive', String(filters.isActive));
  if (filters.search?.trim()) params.set('search', filters.search.trim());

  const query = params.toString();
  return query ? `?${query}` : '';
}

export function getColaboradores(filters?: ColaboradorFilters) {
  return apiRequest<ColaboradorList[]>(`/colaboradores${buildQuery(filters)}`);
}

export function getColaboradorById(id: number) {
  return apiRequest<ColaboradorDetail>(`/colaboradores/${id}`);
}

export function getColaboradorPerfil(id: number) {
  return apiRequest<ColaboradorPerfil>(`/colaboradores/${id}/perfil`);
}

export function getHistorialColaborador(id: number) {
  return apiRequest<HistorialColaborador[]>(`/colaboradores/${id}/historial`);
}

export function createColaborador(request: ColaboradorRequest) {
  return apiRequest<ColaboradorDetail>('/colaboradores', {
    method: 'POST',
    body: request,
  });
}

export function updateColaborador(id: number, request: ColaboradorRequest) {
  return apiRequest<ColaboradorDetail>(`/colaboradores/${id}`, {
    method: 'PUT',
    body: request,
  });
}

export function activarColaborador(id: number) {
  return apiRequest<ColaboradorDetail>(`/colaboradores/${id}/activar`, {
    method: 'PATCH',
  });
}

export function desactivarColaborador(id: number) {
  return apiRequest<ColaboradorDetail>(`/colaboradores/${id}/desactivar`, {
    method: 'PATCH',
  });
}
