import { apiFetch } from '@/shared/api/client';
import { downloadFile } from '@/lib/download';

export interface RosterExportParams {
  columns: string[];
}

export type RegistrationSource = 'SelfService' | 'StaffAdded';
export type RegistrationStatus = 'Confirmed' | 'Cancelled';

export interface EnabledCustomFieldDto {
  fieldDefinitionId: string;
  name: string;
  fieldType: 'YesNo' | 'Text' | 'OptionsList';
  allowedValues: string[];
  displayOrder: number;
}

export interface Registration {
  registrationId: string;
  participantName: string;
  participantEmail: string;
  registrationSource: RegistrationSource;
  status: RegistrationStatus;
  registeredAt: string;
  customFieldValues: Record<string, string | null>;
}

export interface RegistrationCreated extends Registration {
  courseId: string;
  cancellationToken: string;
}

export interface RosterPage {
  items: Registration[];
  total: number;
  page: number;
  pageSize: number;
  enabledCustomFields: EnabledCustomFieldDto[];
  fieldValueSummary: Record<string, number>;
}

export const getRoster = (
  courseId: string,
  page: number,
  pageSize: number,
  statusFilter?: string
): Promise<RosterPage> =>
  apiFetch(
    `/api/v1/courses/${courseId}/registrations?page=${page}&pageSize=${pageSize}${statusFilter ? `&statusFilter=${statusFilter}` : ''}`
  );

export const createRegistration = (
  courseId: string,
  data: { participantName: string; participantEmail: string }
): Promise<RegistrationCreated> =>
  apiFetch(`/api/v1/courses/${courseId}/registrations`, { method: 'POST', body: data });

export const cancelRegistration = (courseId: string, registrationId: string): Promise<void> =>
  apiFetch(`/api/v1/courses/${courseId}/registrations/${registrationId}`, { method: 'DELETE' });

export const setParticipantFieldValue = (
  courseId: string,
  registrationId: string,
  data: { fieldDefinitionId: string; value: string | null }
): Promise<void> =>
  apiFetch(
    `/api/v1/courses/${courseId}/registrations/${registrationId}/field-values`,
    { method: 'PATCH', body: data }
  );

export const downloadRosterExport = (courseId: string, params: RosterExportParams): Promise<void> => {
  const today = new Date().toISOString().slice(0, 10);
  return downloadFile(
    `/api/v1/courses/${courseId}/registrations/export`,
    `course-${courseId}-participants-${today}.csv`,
    { columns: params.columns },
  );
};
