import { Group, Text, Badge, Grid, Paper, ActionIcon, Title, Stack } from '@mantine/core';
import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router';
import { useState } from 'react';
import dayjs from 'dayjs';
import { listCourses } from './coursesApi';
import type { CourseListItem } from './types';

const DAY_NAMES = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];

interface DaySession {
  courseId: string;
  courseTitle: string;
}

export function CourseCalendarPage() {
  const navigate = useNavigate();
  const [currentMonth, setCurrentMonth] = useState(dayjs().startOf('month'));

  const { data: courses = [] } = useQuery({
    queryKey: ['courses'],
    queryFn: listCourses,
  });

  const sessionsByDay = buildSessionsByDay(courses, currentMonth);

  const startOfMonth = currentMonth.startOf('month');
  const endOfMonth = currentMonth.endOf('month');
  const startPad = startOfMonth.day();
  const totalDays = endOfMonth.date();

  const cells: (number | null)[] = [
    ...Array(startPad).fill(null) as null[],
    ...Array.from({ length: totalDays }, (_, i) => i + 1),
  ];

  // Pad to complete rows
  while (cells.length % 7 !== 0) {
    cells.push(null);
  }

  return (
    <Stack gap="md">
      <Group justify="space-between" align="center">
        <Title order={4}>{currentMonth.format('MMMM YYYY')}</Title>
        <Group gap="xs">
          <ActionIcon variant="subtle" onClick={() => setCurrentMonth(m => m.subtract(1, 'month'))}>
            ‹
          </ActionIcon>
          <ActionIcon variant="subtle" onClick={() => setCurrentMonth(m => m.add(1, 'month'))}>
            ›
          </ActionIcon>
        </Group>
      </Group>

      <Grid columns={7}>
        {DAY_NAMES.map(d => (
          <Grid.Col key={d} span={1}>
            <Text ta="center" size="xs" fw={600} c="dimmed">{d}</Text>
          </Grid.Col>
        ))}
        {cells.map((day, idx) => (
          <Grid.Col key={idx} span={1}>
            {day !== null ? (
              <Paper
                p="xs"
                withBorder
                style={{ minHeight: 80 }}
              >
                <Text size="xs" fw={600} mb={4}>{day}</Text>
                {(sessionsByDay[day] ?? []).map((s, i) => (
                  <Badge
                    key={i}
                    size="xs"
                    style={{ cursor: 'pointer', display: 'block', marginBottom: 2 }}
                    onClick={() => navigate(`/app/courses/${s.courseId}`)}
                    variant="light"
                  >
                    {s.courseTitle}
                  </Badge>
                ))}
              </Paper>
            ) : (
              <div style={{ minHeight: 80 }} />
            )}
          </Grid.Col>
        ))}
      </Grid>
    </Stack>
  );
}

function buildSessionsByDay(
  courses: CourseListItem[],
  month: dayjs.Dayjs
): Record<number, DaySession[]> {
  const result: Record<number, DaySession[]> = {};

  for (const course of courses) {
    if (!course.firstSessionAt) continue;
    const date = dayjs(course.firstSessionAt);
    if (date.month() !== month.month() || date.year() !== month.year()) continue;
    const day = date.date();
    if (!result[day]) result[day] = [];
    result[day].push({ courseId: course.id, courseTitle: course.title });
  }

  return result;
}
