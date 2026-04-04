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
import ParticipantCourseViewPage from '@/features/participant/ParticipantCourseViewPage';
import ParticipantPortalRequestPage from '@/features/participant/ParticipantPortalRequestPage';
import ParticipantPortalPage from '@/features/participant/ParticipantPortalPage';
import ExcusalCreditsPage from '@/features/excusal-credits/ExcusalCreditsPage';
import ExcusalSettingsPage from '@/features/settings/excusal/ExcusalSettingsPage';
import CustomFieldsSettingsPage from '@/features/settings/CustomFieldsSettingsPage';
import { PluginsSettingsPage } from '@/features/plugins/PluginsSettingsPage';
import { PluginGuard } from '@/shared/plugins/PluginGuard';

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
      {
        path: 'excusal-credits',
        element: (
          <PluginGuard pluginId="excusals">
            <ExcusalCreditsPage />
          </PluginGuard>
        ),
      },
      {
        path: 'settings/excusal',
        element: (
          <PluginGuard pluginId="excusals">
            <ExcusalSettingsPage />
          </PluginGuard>
        ),
      },
      {
        path: 'settings/custom-fields',
        element: <CustomFieldsSettingsPage />,
      },
      {
        path: 'settings/plugins',
        element: <PluginsSettingsPage />,
      },
    ],
  },
  {
    path: '/participant',
    element: <ParticipantPortalRequestPage />,
  },
  {
    path: '/participant/portal',
    element: <ParticipantPortalPage />,
  },
  {
    path: '/participant/course/:safeLinkToken',
    element: <ParticipantCourseViewPage />,
  },
  {
    path: '*',
    element: <NotFoundPage />,
  },
]);
