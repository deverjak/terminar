import {
  Title, Button, Group, Table, Anchor, Text, Skeleton, Stack, Center,
  SegmentedControl, TextInput, MultiSelect, Select, Pagination,
} from '@mantine/core';
import { IconSearch, IconArrowUp, IconArrowDown, IconArrowsUpDown, IconX } from '@tabler/icons-react';
import { useQuery } from '@tanstack/react-query';
import { Link, useNavigate } from 'react-router';
import { useTranslation } from 'react-i18next';
import { useEffect } from 'react';
import dayjs from 'dayjs';
import { listCourses } from './coursesApi';
import { StatusBadge } from '@/shared/components/StatusBadge';
import { CourseCalendarPage } from './CourseCalendarPage';
import { useCoursesFilter } from './hooks/useCoursesFilter';
import { usePagination } from '@/shared/hooks/usePagination';
import type { SortField, DisplayStatus } from './types';
import { useState } from 'react';

const PAGE_SIZE = 25;

/** Returns a display-only status that reflects temporal reality.
 *  Active courses whose last session has already ended show as 'Ended'. */
function getCourseDisplayStatus(status: string, lastSessionEndsAt: string | null): string {
  if (status === 'Active' && lastSessionEndsAt && new Date(lastSessionEndsAt) <= new Date()) {
    return 'Ended';
  }
  return status;
}

export function CourseListPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const [view, setView] = useState<'list' | 'calendar'>('list');

  useEffect(() => {
    document.title = `${t('courses.title')} — Termínář`;
  }, [t]);

  const { data: courses = [], isLoading } = useQuery({
    queryKey: ['courses'],
    queryFn: listCourses,
  });

  const {
    filters,
    filteredCourses,
    availableTags,
    hasActiveFilters,
    setTemporalBucket,
    setSearch,
    setStatuses,
    setCourseType,
    setTagsFilter,
    toggleSort,
    clearAll,
  } = useCoursesFilter(courses);

  const pagination = usePagination(PAGE_SIZE);

  // Reset to page 1 on filter changes
  useEffect(() => {
    pagination.reset();
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [filters]);

  const pagedCourses = filteredCourses.slice(
    (pagination.page - 1) * PAGE_SIZE,
    pagination.page * PAGE_SIZE,
  );

  const totalPages = Math.ceil(filteredCourses.length / PAGE_SIZE);

  const statusOptions: { value: DisplayStatus; label: string }[] = [
    { value: 'Draft', label: t('courses.statuses.Draft') },
    { value: 'Active', label: t('courses.statuses.Active') },
    { value: 'Ended', label: t('courses.statuses.Ended') },
    { value: 'Cancelled', label: t('courses.statuses.Cancelled') },
    { value: 'Completed', label: t('courses.statuses.Completed') },
  ];

  const courseTypeOptions = [
    { value: '', label: t('courses.filters.allTypes') },
    { value: 'OneTime', label: t('courses.types.OneTime') },
    { value: 'MultiSession', label: t('courses.types.MultiSession') },
  ];

  const SortIcon = ({ field }: { field: SortField }) => {
    if (filters.sortField !== field) return <IconArrowsUpDown size={14} opacity={0.4} />;
    return filters.sortDirection === 'asc'
      ? <IconArrowUp size={14} />
      : <IconArrowDown size={14} />;
  };

  const SortableTh = ({ field, children }: { field: SortField; children: React.ReactNode }) => (
    <Table.Th
      style={{ cursor: 'pointer', userSelect: 'none', whiteSpace: 'nowrap' }}
      onClick={() => toggleSort(field)}
    >
      <Group gap={4} wrap="nowrap">
        {children}
        <SortIcon field={field} />
      </Group>
    </Table.Th>
  );

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
          {/* Temporal bucket tabs */}
          <SegmentedControl
            value={filters.temporalBucket}
            onChange={(v) => setTemporalBucket(v as 'all' | 'upcoming' | 'ongoing' | 'past')}
            data={[
              { label: t('courses.temporal.all'), value: 'all' },
              { label: t('courses.temporal.upcoming'), value: 'upcoming' },
              { label: t('courses.temporal.ongoing'), value: 'ongoing' },
              { label: t('courses.temporal.past'), value: 'past' },
            ]}
          />

          {/* Filter bar */}
          <Group align="flex-end" gap="sm" wrap="wrap">
            <TextInput
              placeholder={t('courses.filters.search')}
              leftSection={<IconSearch size={16} />}
              value={filters.search}
              onChange={(e) => setSearch(e.currentTarget.value)}
              style={{ flex: 1, minWidth: 180 }}
            />
            <MultiSelect
              placeholder={t('courses.filters.status')}
              data={statusOptions}
              value={filters.statuses}
              onChange={(v) => setStatuses(v as DisplayStatus[])}
              clearable
              style={{ minWidth: 160 }}
            />
            <Select
              placeholder={t('courses.filters.type')}
              data={courseTypeOptions}
              value={filters.courseType ?? ''}
              onChange={(v) => setCourseType(v === '' ? null : (v as 'OneTime' | 'MultiSession'))}
              style={{ minWidth: 160 }}
            />
            {availableTags.length > 0 && (
              <MultiSelect
                placeholder={t('courses.filters.tags')}
                data={availableTags}
                value={filters.tags}
                onChange={setTagsFilter}
                clearable
                style={{ minWidth: 160 }}
              />
            )}
            {hasActiveFilters && (
              <Button variant="subtle" leftSection={<IconX size={14} />} onClick={clearAll}>
                {t('courses.filters.clearAll')}
              </Button>
            )}
          </Group>

          {/* Results or empty state */}
          {filteredCourses.length === 0 ? (
            <Center py="xl">
              <Stack align="center" gap="sm">
                {hasActiveFilters ? (
                  <>
                    <Text fw={500}>{t('courses.noResults')}</Text>
                    <Text c="dimmed" size="sm">{t('courses.noResultsHint')}</Text>
                    <Button variant="light" size="xs" onClick={clearAll}>
                      {t('courses.filters.clearAll')}
                    </Button>
                  </>
                ) : (
                  <>
                    <Text fw={500}>{t('courses.noCourses')}</Text>
                    <Text c="dimmed" size="sm">{t('courses.noCoursesHint')}</Text>
                  </>
                )}
              </Stack>
            </Center>
          ) : (
            <>
              <Table striped highlightOnHover>
                <Table.Thead>
                  <Table.Tr>
                    <SortableTh field="title">{t('courses.columns.title')}</SortableTh>
                    <Table.Th>{t('courses.columns.type')}</Table.Th>
                    <Table.Th>{t('courses.columns.status')}</Table.Th>
                    <SortableTh field="capacity">{t('courses.columns.capacity')}</SortableTh>
                    <Table.Th>{t('courses.columns.sessions')}</Table.Th>
                    <SortableTh field="firstSessionAt">{t('courses.columns.firstSession')}</SortableTh>
                  </Table.Tr>
                </Table.Thead>
                <Table.Tbody>
                  {pagedCourses.map((course) => (
                    <Table.Tr key={course.id}>
                      <Table.Td>
                        <Anchor component={Link} to={`/app/courses/${course.id}`}>
                          {course.title}
                        </Anchor>
                      </Table.Td>
                      <Table.Td>{t(`courses.types.${course.courseType}`)}</Table.Td>
                      <Table.Td>
                        <StatusBadge type="course" value={getCourseDisplayStatus(course.status, course.lastSessionEndsAt)} />
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

              {totalPages > 1 && (
                <Group justify="center">
                  <Pagination
                    total={totalPages}
                    value={pagination.page}
                    onChange={pagination.setPage}
                  />
                </Group>
              )}
            </>
          )}
        </>
      )}
    </Stack>
  );
}
