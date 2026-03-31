import { ApiError } from '@/shared/api/client';

export interface CreateTenantRequest {
  name: string;
  slug: string;
  defaultLanguageCode: string;
  adminUsername: string;
  adminEmail: string;
  adminPassword: string;
}

export interface CreateTenantResponse {
  tenant_id: string;
  name: string;
  slug: string;
  status: string;
  created_at: string;
}

export async function createTenant(data: CreateTenantRequest): Promise<CreateTenantResponse> {
  const baseUrl = import.meta.env.VITE_API_BASE_URL ?? '';
  const response = await fetch(`${baseUrl}/api/v1/tenants`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(data),
  });

  let body: Record<string, unknown> = {};
  try {
    body = (await response.json()) as Record<string, unknown>;
  } catch {
    // ignore parse errors
  }

  if (!response.ok) {
    const message = (body.message as string) ?? (body.title as string) ?? response.statusText;
    const fieldErrors = (body.errors as Record<string, string[]>) ?? undefined;
    throw new ApiError(response.status, message, fieldErrors);
  }

  return body as unknown as CreateTenantResponse;
}
