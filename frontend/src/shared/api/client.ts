export class ApiError extends Error {
  status: number;
  fieldErrors?: Record<string, string[]>;

  constructor(status: number, message: string, fieldErrors?: Record<string, string[]>) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.fieldErrors = fieldErrors;
  }
}

interface AuthStore {
  getSession: () => { accessToken: string } | null;
  refreshSession: () => Promise<void>;
}

let authStore: AuthStore | null = null;

export function setAuthStore(store: AuthStore) {
  authStore = store;
}

export function getDownloadToken(): string | null {
  return authStore?.getSession()?.accessToken ?? null;
}

interface FetchOptions {
  method?: string;
  body?: unknown;
  headers?: Record<string, string>;
}

export async function apiFetch<T>(path: string, options: FetchOptions = {}): Promise<T> {
  const baseUrl = import.meta.env.VITE_API_BASE_URL ?? '';
  const session = authStore?.getSession();

  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    ...options.headers,
  };

  if (session?.accessToken) {
    headers['Authorization'] = `Bearer ${session.accessToken}`;
  }

  const fetchOptions: RequestInit = {
    method: options.method ?? 'GET',
    headers,
  };

  if (options.body !== undefined) {
    fetchOptions.body = JSON.stringify(options.body);
  }

  const response = await fetch(`${baseUrl}${path}`, fetchOptions);

  if (response.status === 401 && authStore) {
    try {
      await authStore.refreshSession();
      const newSession = authStore.getSession();
      if (newSession?.accessToken) {
        headers['Authorization'] = `Bearer ${newSession.accessToken}`;
      }
      const retryResponse = await fetch(`${baseUrl}${path}`, { ...fetchOptions, headers });
      return handleResponse<T>(retryResponse);
    } catch {
      throw new ApiError(401, 'Unauthorized');
    }
  }

  return handleResponse<T>(response);
}

async function handleResponse<T>(response: Response): Promise<T> {
  if (response.status === 204) {
    return undefined as T;
  }

  let body: unknown;
  const contentType = response.headers.get('Content-Type') ?? '';
  if (contentType.includes('application/json')) {
    body = await response.json();
  } else {
    body = await response.text();
  }

  if (!response.ok) {
    const errorBody = body as Record<string, unknown>;
    const message = (errorBody?.message as string) ?? (errorBody?.title as string) ?? response.statusText;
    const fieldErrors = (errorBody?.errors as Record<string, string[]>) ?? undefined;
    throw new ApiError(response.status, message, fieldErrors);
  }

  return body as T;
}
