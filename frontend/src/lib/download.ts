const LS_REFRESH_TOKEN = 'terminar_refresh_token';
const LS_USER_ID = 'terminar_user_id';

/**
 * Fetches a file from the backend with auth headers and triggers a browser download.
 * `params` can include repeated keys (e.g., `{ columns: ['course_title', 'course_status'] }`).
 */
export async function downloadFile(
  url: string,
  filename: string,
  params?: Record<string, string | string[]>,
): Promise<void> {
  const baseUrl = import.meta.env.VITE_API_BASE_URL ?? '';

  // Build query string, supporting repeated keys for array values
  let fullUrl = `${baseUrl}${url}`;
  if (params) {
    const qs = new URLSearchParams();
    for (const [key, value] of Object.entries(params)) {
      if (Array.isArray(value)) {
        for (const v of value) qs.append(key, v);
      } else {
        qs.append(key, value);
      }
    }
    const queryString = qs.toString();
    if (queryString) fullUrl += `?${queryString}`;
  }

  // Retrieve access token from the auth store via client.ts's registered store
  // We read from the module-level authStore via a helper exported from client.ts
  const { getDownloadToken } = await import('@/shared/api/client');
  const token = getDownloadToken();

  const headers: Record<string, string> = {};
  if (token) headers['Authorization'] = `Bearer ${token}`;

  const response = await fetch(fullUrl, { headers });

  if (!response.ok) {
    throw new Error(`Download failed: ${response.status} ${response.statusText}`);
  }

  const blob = await response.blob();
  const objectUrl = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = objectUrl;
  a.download = filename;
  document.body.appendChild(a);
  a.click();
  document.body.removeChild(a);
  URL.revokeObjectURL(objectUrl);
}
