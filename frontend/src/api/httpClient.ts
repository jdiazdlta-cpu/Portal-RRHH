import { clearStoredSession, getStoredToken } from '../auth/session';
import type { ApiResponse } from '../types/api';

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? '/api';

type RequestOptions = Omit<RequestInit, 'body'> & {
  body?: unknown;
};

export class ApiError extends Error {
  status: number;
  errors: string[];

  constructor(message: string, status: number, errors: string[] = []) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.errors = errors;
  }
}

export async function apiRequest<T>(
  path: string,
  options: RequestOptions = {},
): Promise<ApiResponse<T>> {
  const token = getStoredToken();
  const headers = new Headers(options.headers);

  if (!headers.has('Content-Type') && options.body !== undefined) {
    headers.set('Content-Type', 'application/json');
  }

  if (token) {
    headers.set('Authorization', `Bearer ${token}`);
  }

  const response = await fetch(`${apiBaseUrl}${path}`, {
    ...options,
    headers,
    body: options.body === undefined ? undefined : JSON.stringify(options.body),
  });

  if (response.status === 401) {
    clearStoredSession();
    window.dispatchEvent(new Event('portalrrhh:unauthorized'));

    if (window.location.pathname !== '/login') {
      window.location.assign('/login');
    }
  }

  const payload = (await response.json().catch(() => null)) as ApiResponse<T> | null;

  if (!response.ok) {
    throw new ApiError(
      payload?.message ?? 'No fue posible completar la solicitud.',
      response.status,
      payload?.errors ?? [],
    );
  }

  if (!payload) {
    throw new ApiError('La API no devolvio una respuesta valida.', response.status);
  }

  return payload;
}

function handleUnauthorized(response: Response) {
  if (response.status !== 401) {
    return;
  }

  clearStoredSession();
  window.dispatchEvent(new Event('portalrrhh:unauthorized'));

  if (window.location.pathname !== '/login') {
    window.location.assign('/login');
  }
}

export async function apiFormRequest<T>(
  path: string,
  formData: FormData,
  options: Omit<RequestInit, 'body'> = {},
): Promise<ApiResponse<T>> {
  const token = getStoredToken();
  const headers = new Headers(options.headers);

  if (token) {
    headers.set('Authorization', `Bearer ${token}`);
  }

  const response = await fetch(`${apiBaseUrl}${path}`, {
    ...options,
    headers,
    body: formData,
  });

  handleUnauthorized(response);

  const payload = (await response.json().catch(() => null)) as ApiResponse<T> | null;

  if (!response.ok) {
    throw new ApiError(
      payload?.message ?? 'No fue posible completar la solicitud.',
      response.status,
      payload?.errors ?? [],
    );
  }

  if (!payload) {
    throw new ApiError('La API no devolvio una respuesta valida.', response.status);
  }

  return payload;
}

export async function apiBlobRequest(path: string) {
  const token = getStoredToken();
  const headers = new Headers();

  if (token) {
    headers.set('Authorization', `Bearer ${token}`);
  }

  const response = await fetch(`${apiBaseUrl}${path}`, { headers });

  handleUnauthorized(response);

  if (!response.ok) {
    const payload = (await response.json().catch(() => null)) as ApiResponse<null> | null;
    throw new ApiError(
      payload?.message ?? 'No fue posible descargar el archivo.',
      response.status,
      payload?.errors ?? [],
    );
  }

  return {
    blob: await response.blob(),
    fileName: getFileNameFromDisposition(response.headers.get('content-disposition')),
  };
}

function getFileNameFromDisposition(contentDisposition: string | null) {
  if (!contentDisposition) {
    return null;
  }

  const utfMatch = /filename\*=UTF-8''([^;]+)/i.exec(contentDisposition);

  if (utfMatch?.[1]) {
    return decodeURIComponent(utfMatch[1]);
  }

  const match = /filename="?([^";]+)"?/i.exec(contentDisposition);
  return match?.[1] ?? null;
}
