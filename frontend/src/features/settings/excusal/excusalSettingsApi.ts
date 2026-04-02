import { apiFetch } from '../../../shared/api/client';

export interface TenantExcusalSettings {
  creditGenerationEnabled: boolean;
  forwardWindowCount: number;
  unenrollmentDeadlineDays: number;
  excusalDeadlineHours: number;
}

export interface ExcusalValidityWindow {
  windowId: string;
  name: string;
  startDate: string;
  endDate: string;
}

export interface CourseExcusalPolicy {
  courseId: string;
  creditGenerationOverride: boolean | null;
  validityWindowId: string | null;
  tags: string[];
  effectiveCreditGenerationEnabled: boolean;
}

export const getExcusalSettings = (): Promise<TenantExcusalSettings> =>
  apiFetch('/api/v1/settings/excusal-policy');

export const updateExcusalSettings = (data: Partial<TenantExcusalSettings>): Promise<void> =>
  apiFetch('/api/v1/settings/excusal-policy', { method: 'PATCH', body: data });

export const listWindows = (): Promise<ExcusalValidityWindow[]> =>
  apiFetch('/api/v1/settings/excusal-windows');

export const createWindow = (data: { name: string; startDate: string; endDate: string }): Promise<{ windowId: string }> =>
  apiFetch('/api/v1/settings/excusal-windows', { method: 'POST', body: data });

export const updateWindow = (id: string, data: Partial<{ name: string; startDate: string; endDate: string }>): Promise<void> =>
  apiFetch(`/api/v1/settings/excusal-windows/${id}`, { method: 'PATCH', body: data });

export const deleteWindow = (id: string): Promise<void> =>
  apiFetch(`/api/v1/settings/excusal-windows/${id}`, { method: 'DELETE' });

export const getCourseExcusalPolicy = (courseId: string): Promise<CourseExcusalPolicy> =>
  apiFetch(`/api/v1/courses/${courseId}/excusal-policy`);

export const updateCourseExcusalPolicy = (courseId: string, data: {
  creditGenerationOverride?: boolean | null;
  clearOverride?: boolean;
  validityWindowId?: string | null;
  clearWindow?: boolean;
  tags?: string[];
}): Promise<void> =>
  apiFetch(`/api/v1/courses/${courseId}/excusal-policy`, { method: 'PATCH', body: data });
