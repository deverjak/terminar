import {
  Title, TextInput, Textarea, Select, NumberInput, Button,
  Stack, Group, Paper, Skeleton
} from '@mantine/core';
import { useForm } from '@mantine/form';
import { useNavigate, useParams, Link } from 'react-router';
import { useTranslation } from 'react-i18next';
import { useState, useEffect } from 'react';
import { notifications } from '@mantine/notifications';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { getCourse, updateCourse } from './coursesApi';
import { ApiError } from '@/shared/api/client';
import type { RegistrationMode } from './types';

interface EditCourseFormValues {
  title: string;
  description: string;
  capacity: number;
  registrationMode: RegistrationMode;
}

export function EditCoursePage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const queryClient = useQueryClient();
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    document.title = `${t('courses.edit')} — Termínář`;
  }, [t]);

  const { data: course, isLoading } = useQuery({
    queryKey: ['course', id],
    queryFn: () => getCourse(id!),
    enabled: !!id,
  });

  const form = useForm<EditCourseFormValues>({
    initialValues: {
      title: '',
      description: '',
      capacity: 10,
      registrationMode: 'Open',
    },
    validate: {
      title: (v) => (v.trim().length === 0 ? 'Required' : null),
      capacity: (v) => (v >= 1 ? null : 'Must be at least 1'),
    },
  });

  useEffect(() => {
    if (course) {
      form.setValues({
        title: course.title,
        description: course.description ?? '',
        capacity: course.capacity,
        registrationMode: course.registrationMode,
      });
    }
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [course]);

  const handleSubmit = async (values: EditCourseFormValues) => {
    if (!id) return;
    setSubmitting(true);
    try {
      await updateCourse(id, {
        title: values.title,
        description: values.description || undefined,
        capacity: values.capacity,
        registrationMode: values.registrationMode,
      });

      notifications.show({
        title: t('courses.edit'),
        message: t('courses.updateSuccess'),
        color: 'green',
      });

      await queryClient.invalidateQueries({ queryKey: ['course', id] });
      await queryClient.invalidateQueries({ queryKey: ['courses'] });
      navigate(`/app/courses/${id}`);
    } catch (err) {
      if (err instanceof ApiError) {
        if (err.fieldErrors) {
          Object.entries(err.fieldErrors).forEach(([field, msgs]) => {
            form.setFieldError(field as keyof EditCourseFormValues, msgs[0]);
          });
        } else {
          notifications.show({ title: 'Error', message: err.message, color: 'red' });
        }
      } else {
        notifications.show({ title: 'Error', message: t('common.error'), color: 'red' });
      }
    } finally {
      setSubmitting(false);
    }
  };

  if (isLoading) {
    return (
      <Stack gap="md">
        <Skeleton height={36} width={200} />
        <Skeleton height={300} />
      </Stack>
    );
  }

  return (
    <Stack gap="md">
      <Title order={2}>{t('courses.edit')}</Title>
      <Paper shadow="sm" p="xl" radius="md">
        <form onSubmit={form.onSubmit(handleSubmit)}>
          <Stack gap="md">
            <TextInput label={t('courses.form.title')} required {...form.getInputProps('title')} />
            <Textarea
              label={t('courses.form.description')}
              autosize
              minRows={3}
              {...form.getInputProps('description')}
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
            <Group justify="flex-end">
              <Button variant="default" component={Link} to={`/app/courses/${id}`}>
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
