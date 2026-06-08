import { apiRequest } from './httpClient';
import type {
  CreateUsuarioRequest,
  ResetPasswordRequest,
  UpdateUsuarioRequest,
  UsuarioDetail,
  UsuarioList,
} from '../types/usuario';

export function getUsuarios() {
  return apiRequest<UsuarioList[]>('/usuarios');
}

export function getUsuarioById(id: number) {
  return apiRequest<UsuarioDetail>(`/usuarios/${id}`);
}

export function createUsuario(request: CreateUsuarioRequest) {
  return apiRequest<UsuarioDetail>('/usuarios', {
    method: 'POST',
    body: request,
  });
}

export function updateUsuario(id: number, request: UpdateUsuarioRequest) {
  return apiRequest<UsuarioDetail>(`/usuarios/${id}`, {
    method: 'PUT',
    body: request,
  });
}

export function activarUsuario(id: number) {
  return apiRequest<UsuarioDetail>(`/usuarios/${id}/activar`, {
    method: 'PATCH',
  });
}

export function desactivarUsuario(id: number) {
  return apiRequest<UsuarioDetail>(`/usuarios/${id}/desactivar`, {
    method: 'PATCH',
  });
}

export function resetUsuarioPassword(id: number, request: ResetPasswordRequest) {
  return apiRequest<UsuarioDetail>(`/usuarios/${id}/reset-password`, {
    method: 'PUT',
    body: request,
  });
}
