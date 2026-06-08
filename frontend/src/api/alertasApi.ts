import { apiRequest } from './httpClient';
import type {
  AlertaFilters,
  AlertaList,
  AlertaResumen,
  GestionarAlertaRequest,
  RecalcularAlertasResult,
} from '../types/alerta';

function buildQuery(filters: AlertaFilters = {}) {
  const params = new URLSearchParams();

  if (filters.estadoAlerta) params.set('estadoAlerta', filters.estadoAlerta);
  if (filters.tipoAlerta) params.set('tipoAlerta', filters.tipoAlerta);
  if (filters.colaboradorId) params.set('colaboradorId', String(filters.colaboradorId));
  if (filters.desde) params.set('desde', filters.desde);
  if (filters.hasta) params.set('hasta', filters.hasta);
  if (filters.incluirInactivas) params.set('incluirInactivas', 'true');

  const query = params.toString();
  return query ? `?${query}` : '';
}

export function getAlertas(filters?: AlertaFilters) {
  return apiRequest<AlertaList[]>(`/alertas${buildQuery(filters)}`);
}

export function getAlertasResumen() {
  return apiRequest<AlertaResumen>('/alertas/resumen');
}

export function gestionarAlerta(id: number, request: GestionarAlertaRequest) {
  return apiRequest<AlertaList>(`/alertas/${id}/gestionar`, {
    method: 'PATCH',
    body: request,
  });
}

export function ignorarAlerta(id: number, request: GestionarAlertaRequest) {
  return apiRequest<AlertaList>(`/alertas/${id}/ignorar`, {
    method: 'PATCH',
    body: request,
  });
}

export function recalcularAlertas() {
  return apiRequest<RecalcularAlertasResult>('/alertas/recalcular', {
    method: 'POST',
  });
}
