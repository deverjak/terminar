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
