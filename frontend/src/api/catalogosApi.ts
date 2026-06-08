import { apiRequest } from './httpClient';
import type {
  CargoCatalogo,
  DepartamentoCatalogo,
  EmpresaCatalogo,
  EstatusColaboradorCatalogo,
  MotivoSalidaCatalogo,
  RolCatalogo,
  TipoDocumentoCatalogo,
  TipoContratoCatalogo,
} from '../types/catalogos';

export function getRolesCatalogo() {
  return apiRequest<RolCatalogo[]>('/catalogos/roles');
}

export function getEmpresasCatalogo() {
  return apiRequest<EmpresaCatalogo[]>('/catalogos/empresas');
}

export function getDepartamentosCatalogo(empresaId?: number) {
  const query = empresaId ? `?empresaId=${empresaId}` : '';
  return apiRequest<DepartamentoCatalogo[]>(`/catalogos/departamentos${query}`);
}

export function getCargosCatalogo(departamentoId?: number) {
  const query = departamentoId ? `?departamentoId=${departamentoId}` : '';
  return apiRequest<CargoCatalogo[]>(`/catalogos/cargos${query}`);
}

export function getTiposContratoCatalogo() {
  return apiRequest<TipoContratoCatalogo[]>('/catalogos/tipos-contrato');
}

export function getEstatusColaboradorCatalogo() {
  return apiRequest<EstatusColaboradorCatalogo[]>('/catalogos/estatus-colaborador');
}

export function getMotivosSalidaCatalogo() {
  return apiRequest<MotivoSalidaCatalogo[]>('/catalogos/motivos-salida');
}

export function getTiposDocumentoCatalogo() {
  return apiRequest<TipoDocumentoCatalogo[]>('/catalogos/tipos-documento');
}
