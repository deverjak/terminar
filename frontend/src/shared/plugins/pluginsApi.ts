import { apiFetch } from '@/shared/api/client';
import type { PluginStatus } from './types';

export async function fetchPlugins(): Promise<PluginStatus[]> {
  return apiFetch<PluginStatus[]>('/api/v1/settings/plugins');
}

export async function activatePlugin(pluginId: string): Promise<void> {
  return apiFetch<void>(`/api/v1/settings/plugins/${pluginId}/activate`, { method: 'POST' });
}

export async function deactivatePlugin(pluginId: string): Promise<void> {
  return apiFetch<void>(`/api/v1/settings/plugins/${pluginId}/activate`, { method: 'DELETE' });
}
