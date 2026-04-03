const API_BASE = import.meta.env.VITE_API_BASE_URL ?? '';

function tenantHeaders(tenantSlug?: string): HeadersInit {
  const headers: Record<string, string> = { 'Content-Type': 'application/json' };
  if (tenantSlug) headers['X-Tenant-Id'] = tenantSlug;
  return headers;
}

export interface ParticipantSession {
  sessionId: string;
  scheduledAt: string;
  durationMinutes: number;
  location: string | null;
  isPast: boolean;
  excusalDeadlineAt: string | null;
  canExcuse: boolean;
  excusalStatus: 'Excused' | 'CreditActive' | 'CreditRedeemed' | 'CreditCancelled' | 'CreditExpired' | null;
}

export interface ParticipantCourseView {
  enrollmentId: string;
  courseId: string;
  courseTitle: string;
  courseStatus: string;
  participantName: string;
  enrollmentStatus: string;
  unenrollmentDeadlineAt: string | null;
  canUnenroll: boolean;
  sessions: ParticipantSession[];
}

export interface ExcusalCreditSummary {
  creditId: string;
  sourceCourseTitle: string;
  sourceSessionAt: string;
  tags: string[];
  validUntil: string;
  status: 'Active' | 'Redeemed' | 'Expired' | 'Cancelled';
}

export interface ParticipantPortal {
  participantEmail: string;
  participantName: string;
  enrollments: Array<{
    enrollmentId: string;
    safeLinkToken: string;
    courseId: string;
    courseTitle: string;
    status: string;
    firstSessionAt: string | null;
    unenrollmentDeadlineAt: string | null;
    canUnenroll: boolean;
  }>;
  excusalCredits: ExcusalCreditSummary[];
}

export async function getCourseView(safeLinkToken: string, tenantSlug: string): Promise<ParticipantCourseView> {
  const res = await fetch(`${API_BASE}/api/v1/participants/courses/${safeLinkToken}`, {
    headers: tenantHeaders(tenantSlug),
  });
  if (!res.ok) throw new Error(`HTTP ${res.status}`);
  return res.json();
}

export async function unenroll(safeLinkToken: string, tenantSlug: string): Promise<void> {
  const res = await fetch(`${API_BASE}/api/v1/participants/courses/${safeLinkToken}/unenroll`, {
    method: 'POST',
    headers: tenantHeaders(tenantSlug),
  });
  if (!res.ok) throw new Error(`HTTP ${res.status}`);
}

export async function excuseFromSession(safeLinkToken: string, sessionId: string, tenantSlug: string): Promise<{ excusalId: string; excusalCreditId: string | null; creditGenerated: boolean }> {
  const res = await fetch(`${API_BASE}/api/v1/participants/courses/${safeLinkToken}/sessions/${sessionId}/excuse`, {
    method: 'POST',
    headers: tenantHeaders(tenantSlug),
  });
  if (!res.ok) throw new Error(`HTTP ${res.status}`);
  return res.json();
}

export async function requestMagicLink(email: string, tenantSlug: string): Promise<void> {
  const res = await fetch(`${API_BASE}/api/v1/participants/magic-link`, {
    method: 'POST',
    headers: tenantHeaders(tenantSlug),
    body: JSON.stringify({ email }),
  });
  if (!res.ok && res.status !== 202) throw new Error(`HTTP ${res.status}`);
}

export async function redeemMagicLink(token: string): Promise<{ portalToken: string; expiresAt: string }> {
  const res = await fetch(`${API_BASE}/api/v1/participants/portal/redeem`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ token }),
  });
  if (!res.ok) throw new Error(`HTTP ${res.status}`);
  return res.json();
}

export async function getPortal(portalToken: string, tenantSlug: string): Promise<ParticipantPortal> {
  const res = await fetch(`${API_BASE}/api/v1/participants/portal`, {
    headers: { ...tenantHeaders(tenantSlug), 'X-Portal-Token': portalToken },
  });
  if (!res.ok) throw new Error(`HTTP ${res.status}`);
  return res.json();
}

export async function redeemCredit(creditId: string, targetCourseId: string, portalToken: string, tenantSlug: string): Promise<{ newEnrollmentId: string; safeLinkToken: string }> {
  const res = await fetch(`${API_BASE}/api/v1/participants/credits/${creditId}/redeem`, {
    method: 'POST',
    headers: { ...tenantHeaders(tenantSlug), 'X-Portal-Token': portalToken },
    body: JSON.stringify({ targetCourseId }),
  });
  if (!res.ok) throw new Error(`HTTP ${res.status}`);
  return res.json();
}
