import {
  Title, Text, Group, Button, Stack, Paper, Table,
  Badge, Skeleton, Breadcrumbs, Anchor
} from '@mantine/core';
import { useDisclosure } from '@mantine/hooks';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useParams, Link } from 'react-router';
import { useTranslation } from 'react-i18next';
import { useEffect } from 'react';
import { notifications } from '@mantine/notifications';
import dayjs from 'dayjs';
import { getCourse, cancelCourse } from './coursesApi';
import { StatusBadge } from '@/shared/components/StatusBadge';
import { ConfirmModal } from '@/shared/components/ConfirmModal';
import { useAuth } from '@/features/auth/useAuth';
import { ApiError } from '@/shared/api/client';
import { useState } from 'react';
import { CourseExcusalPolicySection } from './components/CourseExcusalPolicySection';

export function CourseDetailPage() {
  const { t } = useTranslation();
  const { id } = useParams<{ id: string }>();
  const queryClient = useQueryClient();
  const { session } = useAuth();
  const [cancelOpened, { open: openCancel, close: closeCancel }] = useDisclosure(false);
  const [cancelling, setCancelling] = useState(false);

  useEffect(() => {
    document.title = `${t('courses.title')} — Termínář`;
  }, [t]);

  const { data: course, isLoading } = useQuery({
    queryKey: ['course', id],
    queryFn: () => getCourse(id!),
    enabled: !!id,
  });

  useEffect(() => {
    if (course) {
      document.title = `${course.title} — Termínář`;
    }
  }, [course]);

  const handleCancel = async () => {
    if (!id) return;
    setCancelling(true);
    try {
      await cancelCourse(id);
      notifications.show({
        title: t('courses.cancel'),
        message: t('courses.cancelSuccess'),
        color: 'orange',
      });
      await queryClient.invalidateQueries({ queryKey: ['course', id] });
      await queryClient.invalidateQueries({ queryKey: ['courses'] });
      closeCancel();
    } catch (err) {
      const message = err instanceof ApiError ? err.message : t('common.error');
      notifications.show({ title: 'Error', message, color: 'red' });
    } finally {
      setCancelling(false);
    }
  };

  if (isLoading) {
    return (
      <Stack gap="md">
        <Skeleton height={32} width={300} />
        <Skeleton height={200} />
        <Skeleton height={150} />
      </Stack>
    );
  }

  if (!course) {
    return <Text>{t('common.notFound')}</Text>;
  }

  const isEditable = course.status !== 'Cancelled' && course.status !== 'Completed';

  return (
    <Stack gap="md">
      <Breadcrumbs>
        <Anchor component={Link} to="/app/courses">{t('courses.title')}</Anchor>
        <Text>{course.title}</Text>
      </Breadcrumbs>

      <Group justify="space-between" align="flex-start">
        <Stack gap="xs">
          <Group gap="sm">
            <Title order={2}>{course.title}</Title>
            <StatusBadge type="course" value={course.status} />
          </Group>
          <Group gap="md">
            <Badge variant="light">{t(`courses.types.${course.courseType}`)}</Badge>
            <Badge variant="light" color="teal">{t(`courses.modes.${course.registrationMode}`)}</Badge>
            <Text size="sm" c="dimmed">Capacity: {course.capacity}</Text>
          </Group>
        </Stack>
        <Group>
          {isEditable && (
            <Button variant="light" component={Link} to={`/app/courses/${id}/edit`}>
              {t('courses.detail.editCourse')}
            </Button>
          )}
          <Button
            variant="light"
            color="blue"
            component={Link}
            to={`/app/courses/${id}/registrations`}
          >
            {t('courses.detail.viewRoster')}
          </Button>
          {isEditable && session?.role === 'Admin' && (
            <Button variant="light" color="red" onClick={openCancel}>
              {t('courses.cancel')}
            </Button>
          )}
        </Group>
      </Group>

      {course.description && (
        <Paper withBorder p="md">
          <Text>{course.description}</Text>
        </Paper>
      )}

      <Paper withBorder p="md">
        <Group gap="xl">
          <div>
            <Text size="xs" c="dimmed">{t('courses.detail.createdAt')}</Text>
            <Text size="sm">{dayjs(course.createdAt).format('DD MMM YYYY HH:mm')}</Text>
          </div>
          <div>
            <Text size="xs" c="dimmed">{t('courses.detail.updatedAt')}</Text>
            <Text size="sm">{dayjs(course.updatedAt).format('DD MMM YYYY HH:mm')}</Text>
          </div>
        </Group>
      </Paper>

      <div>
        <Title order={4} mb="sm">{t('courses.detail.sessions')}</Title>
        {course.sessions.length === 0 ? (
          <Text c="dimmed">{t('courses.detail.noSessions')}</Text>
        ) : (
          <Table striped>
            <Table.Thead>
              <Table.Tr>
                <Table.Th>{t('sessions.columns.sequence')}</Table.Th>
                <Table.Th>{t('sessions.columns.scheduledAt')}</Table.Th>
                <Table.Th>{t('sessions.columns.duration')}</Table.Th>
                <Table.Th>{t('sessions.columns.location')}</Table.Th>
                <Table.Th>{t('sessions.columns.endsAt')}</Table.Th>
              </Table.Tr>
            </Table.Thead>
            <Table.Tbody>
              {course.sessions.map((session) => (
                <Table.Tr key={session.id}>
                  <Table.Td>{session.sequence}</Table.Td>
                  <Table.Td>{dayjs(session.scheduledAt).format('DD MMM YYYY HH:mm')}</Table.Td>
                  <Table.Td>{session.durationMinutes} {t('sessions.minutes')}</Table.Td>
                  <Table.Td>{session.location ?? '—'}</Table.Td>
                  <Table.Td>{dayjs(session.endsAt).format('DD MMM YYYY HH:mm')}</Table.Td>
                </Table.Tr>
              ))}
            </Table.Tbody>
          </Table>
        )}
      </div>

      {isEditable && session?.role === 'Admin' && (
        <CourseExcusalPolicySection courseId={id!} />
      )}

      <ConfirmModal
        opened={cancelOpened}
        title={t('courses.cancel')}
        message={t('courses.cancelConfirm')}
        onConfirm={handleCancel}
        onCancel={closeCancel}
        loading={cancelling}
        confirmColor="red"
      />
    </Stack>
  );
}
