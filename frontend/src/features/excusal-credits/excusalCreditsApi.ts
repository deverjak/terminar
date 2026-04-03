import { apiFetch } from '../../shared/api/client';

export interface ExcusalCreditItem {
  creditId: string;
  participantEmail: string;
  participantName: string;
  sourceCourseId: string;
  sourceSessionId: string;
  tags: string[];
  validWindowIds: string[];
  status: string;
  createdAt: string;
  deletedAt: string | null;
  auditEntries: Array<{
    actorStaffId: string;
    actionType: string;
    fieldChanged: string;
    previousValue: string;
    newValue: string;
    timestamp: string;
  }>;
}

export interface CreditsPage {
  items: ExcusalCreditItem[];
  total: number;
  page: number;
  pageSize: number;
}

export function listCredits(page = 1, pageSize = 20, status?: string): Promise<CreditsPage> {
  const params = new URLSearchParams({ page: String(page), page_size: String(pageSize) });
  if (status) params.set('status', status);
  return apiFetch(`/api/v1/excusal-credits?${params}`);
}

export function updateCredit(id: string, data: { additionalWindowIds?: string[]; tags?: string[] }): Promise<void> {
  return apiFetch(`/api/v1/excusal-credits/${id}`, { method: 'PATCH', body: data });
}

export function deleteCredit(id: string): Promise<void> {
  return apiFetch(`/api/v1/excusal-credits/${id}`, { method: 'DELETE' });
}
