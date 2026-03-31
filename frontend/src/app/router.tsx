import { createBrowserRouter, Navigate } from 'react-router';
import { LandingPage } from '@/features/landing/LandingPage';
import { TenantRegisterPage } from '@/features/tenants/TenantRegisterPage';
import { LoginPage } from '@/features/auth/LoginPage';
import { AppShellLayout } from '@/shared/components/AppShellLayout';
import { AuthGuard } from './AuthGuard';
import { CourseListPage } from '@/features/courses/CourseListPage';
import { CreateCoursePage } from '@/features/courses/CreateCoursePage';
import { CourseDetailPage } from '@/features/courses/CourseDetailPage';
import { EditCoursePage } from '@/features/courses/EditCoursePage';
import { CourseRosterPage } from '@/features/registrations/CourseRosterPage';
import { StaffListPage } from '@/features/staff/StaffListPage';
import { NotFoundPage } from '@/shared/components/NotFoundPage';

export const router = createBrowserRouter([
  {
    path: '/',
    element: <LandingPage />,
  },
  {
    path: '/register',
    element: <TenantRegisterPage />,
  },
  {
    path: '/login',
    element: <LoginPage />,
  },
  {
    path: '/app',
    element: (
      <AuthGuard>
        <AppShellLayout />
      </AuthGuard>
    ),
    children: [
      {
        index: true,
        element: <Navigate to="/app/courses" replace />,
      },
      {
        path: 'courses',
        element: <CourseListPage />,
      },
      {
        path: 'courses/new',
        element: <CreateCoursePage />,
      },
      {
        path: 'courses/:id',
        element: <CourseDetailPage />,
      },
      {
        path: 'courses/:id/edit',
        element: <EditCoursePage />,
      },
      {
        path: 'courses/:courseId/registrations',
        element: <CourseRosterPage />,
      },
      {
        path: 'staff',
        element: <StaffListPage />,
      },
    ],
  },
  {
    path: '*',
    element: <NotFoundPage />,
  },
]);
