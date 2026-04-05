import { apiFetch } from '@/shared/api/client';
import { downloadFile } from '@/lib/download';
import type { CourseListItem, CourseDetail, CreateCourseRequest, UpdateCourseRequest } from './types';

export type ExportColumnGroup = 'CourseInfo' | 'ParticipantInfo' | 'CustomFields';

export interface ExportColumnDefinition {
  key: string;
  labelKey: string;
  group: ExportColumnGroup;
  defaultEnabled: boolean;
  requiresParticipants: boolean;
  label?: string;
}

export interface CoursesExportParams {
  includeParticipants: boolean;
  dateFrom?: string;
  dateTo?: string;
  status?: string;
  columns: string[];
}

export interface CourseCustomFieldDto {
  fieldDefinitionId: string;
  name: string;
  fieldType: string;
  allowedValues: string[];
  displayOrder: number;
  isEnabled: boolean;
}

export const listCourses = (): Promise<CourseListItem[]> =>
  apiFetch('/api/v1/courses/');

export const getCourse = (id: string): Promise<CourseDetail> =>
  apiFetch(`/api/v1/courses/${id}`);

export const createCourse = (data: CreateCourseRequest): Promise<{ id: string }> =>
  apiFetch('/api/v1/courses/', { method: 'POST', body: data });

export const updateCourse = (id: string, data: UpdateCourseRequest): Promise<void> =>
  apiFetch(`/api/v1/courses/${id}`, { method: 'PATCH', body: data });

export const cancelCourse = (id: string): Promise<void> =>
  apiFetch(`/api/v1/courses/${id}/cancel`, { method: 'POST' });

export const getCourseCustomFields = (courseId: string): Promise<CourseCustomFieldDto[]> =>
  apiFetch(`/api/v1/courses/${courseId}/custom-fields`);

export const updateCourseCustomFields = (courseId: string, enabledFieldIds: string[]): Promise<void> =>
  apiFetch(`/api/v1/courses/${courseId}/custom-fields`, { method: 'PUT', body: { enabledFieldIds } });

export const getExportColumns = (): Promise<{ columns: ExportColumnDefinition[] }> =>
  apiFetch('/api/v1/courses/export/columns');

export const downloadCoursesExport = (params: CoursesExportParams): Promise<void> => {
  const queryParams: Record<string, string | string[]> = {
    include_participants: params.includeParticipants ? 'true' : 'false',
    columns: params.columns,
  };
  if (params.dateFrom) queryParams['date_from'] = params.dateFrom;
  if (params.dateTo) queryParams['date_to'] = params.dateTo;
  if (params.status) queryParams['status'] = params.status;

  const today = new Date().toISOString().slice(0, 10);
  return downloadFile('/api/v1/courses/export', `courses-export-${today}.csv`, queryParams);
};
