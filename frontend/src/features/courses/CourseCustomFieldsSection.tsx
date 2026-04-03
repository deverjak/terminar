import { Paper, Title, Stack, Switch, Text, Anchor } from '@mantine/core';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { notifications } from '@mantine/notifications';
import { Link } from 'react-router';
import { ApiError } from '@/shared/api/client';
import { getCourseCustomFields, updateCourseCustomFields } from './coursesApi';

interface CourseCustomFieldsSectionProps {
  courseId: string;
}

export function CourseCustomFieldsSection({ courseId }: CourseCustomFieldsSectionProps) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();

  const { data: fields = [], isLoading } = useQuery({
    queryKey: ['course-custom-fields', courseId],
    queryFn: () => getCourseCustomFields(courseId),
  });

  const mutation = useMutation({
    mutationFn: (enabledFieldIds: string[]) =>
      updateCourseCustomFields(courseId, enabledFieldIds),
    onSuccess: () => {
      notifications.show({ message: t('customFields.course.saveSuccess'), color: 'green' });
      queryClient.invalidateQueries({ queryKey: ['course-custom-fields', courseId] });
    },
    onError: (err) => {
      const message = err instanceof ApiError ? err.message : t('common.error');
      notifications.show({ title: 'Error', message, color: 'red' });
    },
  });

  const handleToggle = (fieldId: string, currentlyEnabled: boolean) => {
    const current = fields.filter(f => f.isEnabled).map(f => f.fieldDefinitionId);
    const updated = currentlyEnabled
      ? current.filter(id => id !== fieldId)
      : [...current, fieldId];
    mutation.mutate(updated);
  };

  if (isLoading) return null;

  return (
    <Paper withBorder p="md">
      <Stack gap="sm">
        <Title order={4}>{t('customFields.course.sectionTitle')}</Title>
        {fields.length === 0 ? (
          <Text size="sm" c="dimmed">
            {t('customFields.course.noFieldsDefined')}{' '}
            <Anchor component={Link} to="/app/settings/custom-fields" size="sm">
              {t('customFields.title')}
            </Anchor>
          </Text>
        ) : (
          fields.map((field) => (
            <Switch
              key={field.fieldDefinitionId}
              label={`${field.name} (${field.fieldType})`}
              checked={field.isEnabled}
              onChange={() => handleToggle(field.fieldDefinitionId, field.isEnabled)}
              disabled={mutation.isPending}
            />
          ))
        )}
      </Stack>
    </Paper>
  );
}
