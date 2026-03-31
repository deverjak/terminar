import { apiFetch } from '@/shared/api/client';

export interface StaffUser {
  staffUserId: string;
  username: string;
  email: string;
  role: 'Admin' | 'Staff';
  status: 'Active' | 'Inactive';
  createdAt: string;
}

export interface CreateStaffUserRequest {
  username: string;
  email: string;
  password: string;
  role: 'Admin' | 'Staff';
}

export const listStaff = (): Promise<StaffUser[]> =>
  apiFetch('/api/v1/staff/');

export const createStaff = (data: CreateStaffUserRequest): Promise<StaffUser> =>
  apiFetch('/api/v1/staff/', { method: 'POST', body: data });

export const deactivateStaff = (id: string): Promise<void> =>
  apiFetch(`/api/v1/staff/${id}/deactivate`, { method: 'POST' });
