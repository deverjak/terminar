import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Paper, Title, Stack, Switch, Select, TextInput, Button, Text, Group, Badge } from '@mantine/core';
import { useTranslation } from 'react-i18next';
import { notifications } from '@mantine/notifications';
import { useState } from 'react';
import { getCourseExcusalPolicy, updateCourseExcusalPolicy } from '@/features/settings/excusal/excusalSettingsApi';
import { listWindows } from '@/features/settings/excusal/excusalSettingsApi';
import { useActivePlugins } from '@/shared/plugins/useActivePlugins';

interface CourseExcusalPolicySectionProps {
  courseId: string;
}

export function CourseExcusalPolicySection({ courseId }: CourseExcusalPolicySectionProps) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const activePluginIds = useActivePlugins();

  if (!activePluginIds.includes('excusals')) {
    return null;
  }

  const { data: policy, isLoading: policyLoading } = useQuery({
    queryKey: ['course-excusal-policy', courseId],
    queryFn: () => getCourseExcusalPolicy(courseId),
  });

  const { data: windows } = useQuery({
    queryKey: ['excusal-windows'],
    queryFn: listWindows,
  });

  const [overrideEnabled, setOverrideEnabled] = useState<boolean | undefined>();
  const [clearOverride, setClearOverride] = useState(false);
  const [selectedWindowId, setSelectedWindowId] = useState<string | null | undefined>();
  const [tagsInput, setTagsInput] = useState<string | undefined>();

  const updateMutation = useMutation({
    mutationFn: () => {
      const tags = tagsInput !== undefined
        ? tagsInput.split(',').map(t => t.trim()).filter(Boolean)
        : undefined;
      return updateCourseExcusalPolicy(courseId, {
        creditGenerationOverride: clearOverride ? null : overrideEnabled,
        clearOverride,
        validityWindowId: selectedWindowId ?? null,
        clearWindow: selectedWindowId === null,
        tags,
      });
    },
    onSuccess: () => {
      notifications.show({ color: 'green', message: t('excusalSettings.coursePolicy.saveButton') });
      queryClient.invalidateQueries({ queryKey: ['course-excusal-policy', courseId] });
    },
  });

  if (policyLoading) return null;

  const windowOptions = (windows ?? []).map(w => ({ value: w.windowId, label: w.name }));

  const currentOverrideValue = clearOverride
    ? undefined
    : (overrideEnabled ?? policy?.creditGenerationOverride ?? undefined);

  return (
    <Paper withBorder p="md">
      <Title order={4} mb="md">{t('excusalSettings.coursePolicy.title')}</Title>

      {policy && (
        <Group mb="md" gap="xs">
          <Text size="sm" c="dimmed">Effective:</Text>
          <Badge color={policy.effectiveCreditGenerationEnabled ? 'green' : 'gray'}>
            {policy.effectiveCreditGenerationEnabled ? 'Credits enabled' : 'Credits disabled'}
          </Badge>
          {policy.tags.map(tag => <Badge key={tag} variant="outline" size="xs">{tag}</Badge>)}
        </Group>
      )}

      <Stack gap="md">
        <Group gap="md" align="center">
          <Switch
            label={t('excusalSettings.coursePolicy.override')}
            checked={currentOverrideValue ?? false}
            onChange={e => {
              setClearOverride(false);
              setOverrideEnabled(e.currentTarget.checked);
            }}
          />
          <Button
            size="xs"
            variant="subtle"
            color="gray"
            onClick={() => {
              setClearOverride(true);
              setOverrideEnabled(undefined);
            }}
          >
            {t('common.cancel')} override
          </Button>
        </Group>

        <Select
          label={t('excusalSettings.coursePolicy.window')}
          placeholder="Select window..."
          data={windowOptions}
          value={selectedWindowId ?? policy?.validityWindowId ?? null}
          onChange={val => setSelectedWindowId(val)}
          clearable
        />

        <TextInput
          label={t('excusalSettings.coursePolicy.tags')}
          placeholder={policy?.tags.join(', ') ?? ''}
          value={tagsInput ?? policy?.tags.join(', ') ?? ''}
          onChange={e => setTagsInput(e.target.value)}
        />

        <Button onClick={() => updateMutation.mutate()} loading={updateMutation.isPending}>
          {t('excusalSettings.coursePolicy.saveButton')}
        </Button>
      </Stack>
    </Paper>
  );
}
