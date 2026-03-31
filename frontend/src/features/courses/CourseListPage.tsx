import { Title, Button, Group, Table, Anchor, Text, Skeleton, Stack, Center, SegmentedControl } from '@mantine/core';
import { useQuery } from '@tanstack/react-query';
import { Link, useNavigate } from 'react-router';
import { useTranslation } from 'react-i18next';
import { useState, useEffect } from 'react';
import dayjs from 'dayjs';
import { listCourses } from './coursesApi';
import { StatusBadge } from '@/shared/components/StatusBadge';
import { CourseCalendarPage } from './CourseCalendarPage';

export function CourseListPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const [view, setView] = useState<'list' | 'calendar'>('list');

  useEffect(() => {
    document.title = `${t('courses.title')} — Termínář`;
  }, [t]);

  const { data: courses, isLoading } = useQuery({
    queryKey: ['courses'],
    queryFn: listCourses,
  });

  if (isLoading) {
    return (
      <Stack gap="md">
        <Skeleton height={36} width={200} />
        <Skeleton height={200} />
      </Stack>
    );
  }

  return (
    <Stack gap="md">
      <Group justify="space-between" align="center">
        <Title order={2}>{t('courses.title')}</Title>
        <Group>
          <SegmentedControl
            value={view}
            onChange={(v) => setView(v as 'list' | 'calendar')}
            data={[
              { label: t('courses.listView'), value: 'list' },
              { label: t('courses.calendarView'), value: 'calendar' },
            ]}
          />
          <Button onClick={() => navigate('/app/courses/new')}>{t('courses.newCourse')}</Button>
        </Group>
      </Group>

      {view === 'calendar' ? (
        <CourseCalendarPage />
      ) : (
        <>
          {!courses || courses.length === 0 ? (
            <Center py="xl">
              <Stack align="center" gap="sm">
                <Text fw={500}>{t('courses.noCourses')}</Text>
                <Text c="dimmed" size="sm">{t('courses.noCoursesHint')}</Text>
              </Stack>
            </Center>
          ) : (
            <Table striped highlightOnHover>
              <Table.Thead>
                <Table.Tr>
                  <Table.Th>{t('courses.columns.title')}</Table.Th>
                  <Table.Th>{t('courses.columns.type')}</Table.Th>
                  <Table.Th>{t('courses.columns.status')}</Table.Th>
                  <Table.Th>{t('courses.columns.capacity')}</Table.Th>
                  <Table.Th>{t('courses.columns.sessions')}</Table.Th>
                  <Table.Th>{t('courses.columns.firstSession')}</Table.Th>
                </Table.Tr>
              </Table.Thead>
              <Table.Tbody>
                {courses.map((course) => (
                  <Table.Tr key={course.id}>
                    <Table.Td>
                      <Anchor component={Link} to={`/app/courses/${course.id}`}>
                        {course.title}
                      </Anchor>
                    </Table.Td>
                    <Table.Td>{t(`courses.types.${course.courseType}`)}</Table.Td>
                    <Table.Td>
                      <StatusBadge type="course" value={course.status} />
                    </Table.Td>
                    <Table.Td>{course.capacity}</Table.Td>
                    <Table.Td>{course.sessionCount}</Table.Td>
                    <Table.Td>
                      {course.firstSessionAt
                        ? dayjs(course.firstSessionAt).format('DD MMM YYYY HH:mm')
                        : '—'}
                    </Table.Td>
                  </Table.Tr>
                ))}
              </Table.Tbody>
            </Table>
          )}
        </>
      )}
    </Stack>
  );
}
