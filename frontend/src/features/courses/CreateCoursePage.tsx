import {
  Title, TextInput, Textarea, Select, NumberInput, Button,
  Stack, Group, Paper, ActionIcon, Text
} from '@mantine/core';
import { DateTimePicker } from '@mantine/dates';
import { useForm } from '@mantine/form';
import { useNavigate, Link } from 'react-router';
import { useTranslation } from 'react-i18next';
import { useState, useEffect } from 'react';
import { notifications } from '@mantine/notifications';
import { useQueryClient } from '@tanstack/react-query';
import { IconTrash, IconPlus } from '@tabler/icons-react';
import { createCourse } from './coursesApi';
import { ApiError } from '@/shared/api/client';
import type { CourseType, RegistrationMode, SessionInput } from './types';

interface SessionFormValue {
  scheduledAt: Date | null;
  durationMinutes: number;
  location: string;
}

interface CreateCourseFormValues {
  title: string;
  description: string;
  courseType: CourseType;
  registrationMode: RegistrationMode;
  capacity: number;
  sessions: SessionFormValue[];
}

export function CreateCoursePage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    document.title = `${t('courses.create')} — Termínář`;
  }, [t]);

  const form = useForm<CreateCourseFormValues>({
    initialValues: {
      title: '',
      description: '',
      courseType: 'OneTime',
      registrationMode: 'Open',
      capacity: 10,
      sessions: [],
    },
    validate: {
      title: (v) => (v.trim().length === 0 ? 'Required' : null),
      capacity: (v) => (v >= 1 ? null : 'Must be at least 1'),
      sessions: {
        scheduledAt: (v) => {
          if (!v) return 'Required';
          if (v <= new Date()) return t('courses.form.futureDateRequired');
          return null;
        },
        durationMinutes: (v) => (v >= 1 ? null : 'Must be at least 1'),
      },
    },
  });

  const addSession = () => {
    form.insertListItem('sessions', {
      scheduledAt: null,
      durationMinutes: 60,
      location: '',
    });
  };

  const removeSession = (index: number) => {
    form.removeListItem('sessions', index);
  };

  const handleSubmit = async (values: CreateCourseFormValues) => {
    setSubmitting(true);
    try {
      const sessions: SessionInput[] = values.sessions.map((s) => ({
        scheduledAt: new Date(s.scheduledAt as unknown as number).toISOString(),
        durationMinutes: s.durationMinutes,
        location: s.location || undefined,
      }));

      const result = await createCourse({
        title: values.title,
        description: values.description || undefined,
        courseType: values.courseType,
        registrationMode: values.registrationMode,
        capacity: values.capacity,
        sessions,
      });

      notifications.show({
        title: t('courses.create'),
        message: t('courses.createSuccess'),
        color: 'green',
      });

      await queryClient.invalidateQueries({ queryKey: ['courses'] });
      navigate(`/app/courses/${result.id}`);
    } catch (err) {
      console.error('[CreateCourse]', err);
      if (err instanceof ApiError) {
        if (err.fieldErrors) {
          Object.entries(err.fieldErrors).forEach(([, msgs]) => {
            notifications.show({ title: 'Validation error', message: msgs[0], color: 'red' });
          });
        } else {
          notifications.show({ title: 'Error', message: err.message, color: 'red' });
        }
      } else {
        notifications.show({ title: 'Error', message: String(err), color: 'red' });
      }
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Stack gap="md">
      <Title order={2}>{t('courses.create')}</Title>
      <Paper shadow="sm" p="xl" radius="md">
        <form onSubmit={form.onSubmit(handleSubmit)}>
          <Stack gap="md">
            <TextInput label={t('courses.form.title')} required {...form.getInputProps('title')} />
            <Textarea label={t('courses.form.description')} autosize minRows={3} {...form.getInputProps('description')} />
            <Select
              label={t('courses.form.courseType')}
              data={[
                { value: 'OneTime', label: t('courses.types.OneTime') },
                { value: 'MultiSession', label: t('courses.types.MultiSession') },
              ]}
              {...form.getInputProps('courseType')}
            />
            <Select
              label={t('courses.form.registrationMode')}
              data={[
                { value: 'Open', label: t('courses.modes.Open') },
                { value: 'StaffOnly', label: t('courses.modes.StaffOnly') },
              ]}
              {...form.getInputProps('registrationMode')}
            />
            <NumberInput
              label={t('courses.form.capacity')}
              min={1}
              required
              {...form.getInputProps('capacity')}
            />

            <div>
              <Text fw={500} mb="sm">{t('courses.form.sessions')}</Text>
              <Stack gap="sm">
                {form.values.sessions.map((_, index) => (
                  <Paper key={index} withBorder p="md">
                    <Group justify="space-between" mb="sm">
                      <Text size="sm" fw={500}>Session {index + 1}</Text>
                      <ActionIcon
                        color="red"
                        variant="subtle"
                        onClick={() => removeSession(index)}
                        title={t('courses.form.removeSession')}
                      >
                        <IconTrash size={16} />
                      </ActionIcon>
                    </Group>
                    <Stack gap="sm">
                      <DateTimePicker
                        label={t('courses.form.scheduledAt')}
                        required
                        {...form.getInputProps(`sessions.${index}.scheduledAt`)}
                      />
                      <NumberInput
                        label={t('courses.form.durationMinutes')}
                        min={1}
                        required
                        {...form.getInputProps(`sessions.${index}.durationMinutes`)}
                      />
                      <TextInput
                        label={t('courses.form.location')}
                        {...form.getInputProps(`sessions.${index}.location`)}
                      />
                    </Stack>
                  </Paper>
                ))}
              </Stack>
              <Button
                variant="light"
                leftSection={<IconPlus size={16} />}
                onClick={addSession}
                mt="sm"
              >
                {t('courses.form.addSession')}
              </Button>
            </div>

            <Group justify="flex-end">
              <Button variant="default" component={Link} to="/app/courses">
                {t('courses.form.cancelButton')}
              </Button>
              <Button type="submit" loading={submitting}>
                {t('courses.form.saveButton')}
              </Button>
            </Group>
          </Stack>
        </form>
      </Paper>
    </Stack>
  );
}
