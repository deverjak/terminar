export type CourseType = 'OneTime' | 'MultiSession';
export type RegistrationMode = 'Open' | 'StaffOnly';
export type CourseStatus = 'Draft' | 'Active' | 'Cancelled' | 'Completed';

export interface CourseListItem {
  id: string;
  title: string;
  description: string;
  courseType: CourseType;
  registrationMode: RegistrationMode;
  capacity: number;
  status: CourseStatus;
  sessionCount: number;
  firstSessionAt: string | null;
}

export interface SessionDetail {
  id: string;
  sequence: number;
  scheduledAt: string;
  durationMinutes: number;
  location: string | null;
  endsAt: string;
}

export interface CourseDetail {
  id: string;
  title: string;
  description: string;
  courseType: CourseType;
  registrationMode: RegistrationMode;
  capacity: number;
  status: CourseStatus;
  createdByStaffId: string;
  createdAt: string;
  updatedAt: string;
  sessions: SessionDetail[];
}

export interface SessionInput {
  scheduledAt: string;
  durationMinutes: number;
  location?: string;
}

export interface CreateCourseRequest {
  title: string;
  description?: string;
  courseType: CourseType;
  registrationMode: RegistrationMode;
  capacity: number;
  sessions: SessionInput[];
}

export interface UpdateCourseRequest {
  title?: string;
  description?: string;
  capacity?: number;
  registrationMode?: RegistrationMode;
}

export interface CalendarEvent {
  id: string;
  courseId: string;
  courseTitle: string;
  date: Date;
  durationMinutes: number;
  location: string | null;
}

// --- Recurrence types ---

export type DayOfWeek = 0 | 1 | 2 | 3 | 4 | 5 | 6; // 0=Sun … 6=Sat (matches Date.getDay())

export interface RecurrenceRule {
  id: string;
  dayOfWeek: DayOfWeek | null;
  startTime: string; // "HH:MM" 24-hour
  seriesStartDate: string | null; // "YYYY-MM-DD" — matches DatePickerInput output
  endCondition: 'count' | 'date';
  occurrences: number; // used when endCondition = 'count'
  endDate: string | null; // "YYYY-MM-DD" — matches DatePickerInput output
}

export interface GeneratedSession {
  key: string; // `${ruleId}-${index}`
  ruleId: string;
  scheduledAt: Date;
}

export interface SessionPreviewEntry {
  key: string;
  scheduledAt: Date;
  durationMinutes: number;
  location: string;
  source: 'recurrence' | 'manual';
  ruleId: string | null;
  isDeleted: boolean;
  isDuplicate: boolean;
}
