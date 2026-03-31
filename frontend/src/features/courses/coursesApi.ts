import { apiFetch } from '@/shared/api/client';
import type { CourseListItem, CourseDetail, CreateCourseRequest, UpdateCourseRequest } from './types';

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
