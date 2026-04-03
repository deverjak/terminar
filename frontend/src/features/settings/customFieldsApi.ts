import { apiFetch } from '@/shared/api/client';

export type CustomFieldType = 'YesNo' | 'Text' | 'OptionsList';

export interface CustomFieldDefinition {
  id: string;
  name: string;
  fieldType: CustomFieldType;
  allowedValues: string[];
  displayOrder: number;
}

export interface CreateCustomFieldRequest {
  name: string;
  fieldType: CustomFieldType;
  allowedValues: string[];
}

export interface UpdateCustomFieldRequest {
  name?: string;
  allowedValues?: string[];
}

export const listCustomFields = (): Promise<CustomFieldDefinition[]> =>
  apiFetch('/api/v1/settings/custom-fields');

export const createCustomField = (data: CreateCustomFieldRequest): Promise<{ id: string }> =>
  apiFetch('/api/v1/settings/custom-fields', { method: 'POST', body: data });

export const updateCustomField = (id: string, data: UpdateCustomFieldRequest): Promise<void> =>
  apiFetch(`/api/v1/settings/custom-fields/${id}`, { method: 'PATCH', body: data });

export const deleteCustomField = (id: string): Promise<void> =>
  apiFetch(`/api/v1/settings/custom-fields/${id}`, { method: 'DELETE' });
