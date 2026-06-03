import { apiRequest } from './httpClient';
import type {
  AltasBajas,
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

export function getAltasBajas(anio = new Date().getFullYear()) {
  return apiRequest<AltasBajas[]>(`/dashboard/altas-bajas?anio=${anio}`);
}

export function getUltimosMovimientos() {
  return apiRequest<UltimosMovimientos>('/dashboard/ultimos-movimientos');
}
