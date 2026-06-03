import { apiRequest } from './httpClient';
import type { AuthUser, LoginResponse } from '../types/auth';

export type LoginRequest = {
  email: string;
  password: string;
};

export function loginRequest(request: LoginRequest) {
  return apiRequest<LoginResponse>('/auth/login', {
    method: 'POST',
    body: request,
  });
}

export function getCurrentUserRequest() {
  return apiRequest<AuthUser>('/auth/me');
}
