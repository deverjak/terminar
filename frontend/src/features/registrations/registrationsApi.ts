import { apiFetch } from '@/shared/api/client';

export type RegistrationSource = 'SelfService' | 'StaffAdded';
export type RegistrationStatus = 'Confirmed' | 'Cancelled';

export interface Registration {
  registrationId: string;
  participantName: string;
  participantEmail: string;
  registrationSource: RegistrationSource;
  status: RegistrationStatus;
  registeredAt: string;
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
