import { ApiError } from '@/shared/api/client';

export interface AuthTokenResponse {
  accessToken: string;
  refreshToken: string;
  tenantSlug: string;
}

export interface JwtClaims {
  sub: string;
  username: string;
  role: string;
  tenantId: string;
  tenantSlug: string;
  email: string;
}

export function decodeJwt(token: string): JwtClaims {
  const payload = token.split('.')[1];
  const decoded = JSON.parse(atob(payload)) as Record<string, unknown>;
  return {
    sub: decoded['sub'] as string,
    username: decoded['username'] as string,
    role: (decoded['role'] ?? decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']) as string,
    tenantId: decoded['tenant_id'] as string,
    tenantSlug: decoded['tenant_slug'] as string,
    email: decoded['email'] as string,
  };
}

export async function login(email: string, password: string): Promise<AuthTokenResponse> {
  const baseUrl = import.meta.env.VITE_API_BASE_URL ?? '';
  const response = await fetch(`${baseUrl}/api/v1/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password }),
  });

  if (!response.ok) {
    let message = response.statusText;
    try {
      const body = (await response.json()) as Record<string, unknown>;
      message = (body.message as string) ?? (body.title as string) ?? message;
    } catch {
      // ignore
    }
    throw new ApiError(response.status, message);
  }

  return response.json() as Promise<AuthTokenResponse>;
}

export async function refreshTokenApi(userId: string, refreshToken: string): Promise<AuthTokenResponse> {
  const baseUrl = import.meta.env.VITE_API_BASE_URL ?? '';
  const response = await fetch(`${baseUrl}/api/v1/auth/refresh`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ userId, refreshToken }),
  });

  if (!response.ok) {
    throw new ApiError(response.status, 'Token refresh failed');
  }

  return response.json() as Promise<AuthTokenResponse>;
}
