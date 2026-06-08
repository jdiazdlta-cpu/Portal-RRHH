import { apiRequest } from './httpClient';
import type {
  AltasBajas,
  AltasBajasFilters,
  AltaDetalle,
  BajaDetalle,
  ColaboradoresPorDepartamento,
  ColaboradoresPorEstatus,
  DashboardResumen,
  DashboardVencimientos,
  UltimosMovimientos,
} from '../types/dashboard';

export function getDashboardResumen() {
  return apiRequest<DashboardResumen>('/dashboard/resumen');
}

export function getDashboardVencimientos() {
  return apiRequest<DashboardVencimientos>('/dashboard/vencimientos');
}

export function getColaboradoresPorEstatus() {
  return apiRequest<ColaboradoresPorEstatus[]>('/dashboard/colaboradores-por-estatus');
}

export function getColaboradoresPorDepartamento() {
  return apiRequest<ColaboradoresPorDepartamento[]>(
    '/dashboard/colaboradores-por-departamento',
  );
}

function buildAltasBajasQuery(filters: AltasBajasFilters = {}) {
  const params = new URLSearchParams();

  if (filters.anio) params.set('anio', String(filters.anio));
  if (filters.mes) params.set('mes', String(filters.mes));
  if (filters.empresaId) params.set('empresaId', String(filters.empresaId));
  if (filters.departamentoId) params.set('departamentoId', String(filters.departamentoId));

  const query = params.toString();
  return query ? `?${query}` : '';
}

export function getAltasBajas(filters: AltasBajasFilters = {}) {
  const normalizedFilters = {
    anio: filters.anio ?? new Date().getFullYear(),
    empresaId: filters.empresaId,
    departamentoId: filters.departamentoId,
  };

  return apiRequest<AltasBajas[]>(
    `/dashboard/altas-bajas${buildAltasBajasQuery(normalizedFilters)}`,
  );
}

export function getAltasDetalle(filters: AltasBajasFilters = {}) {
  return apiRequest<AltaDetalle[]>(
    `/dashboard/altas-detalle${buildAltasBajasQuery(filters)}`,
  );
}

export function getBajasDetalle(filters: AltasBajasFilters = {}) {
  return apiRequest<BajaDetalle[]>(
    `/dashboard/bajas-detalle${buildAltasBajasQuery(filters)}`,
  );
}

export function getUltimosMovimientos() {
  return apiRequest<UltimosMovimientos>('/dashboard/ultimos-movimientos');
}
