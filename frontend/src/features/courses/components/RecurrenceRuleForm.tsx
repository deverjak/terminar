import { Select, NumberInput, SegmentedControl, Group, Text, Stack, ActionIcon, Paper, Alert } from '@mantine/core';
import { TimeInput, DatePickerInput } from '@mantine/dates';
import { useTranslation } from 'react-i18next';
import { IconTrash, IconAlertTriangle } from '@tabler/icons-react';
import type { RecurrenceRule, DayOfWeek } from '../types';

interface Props {
  rule: RecurrenceRule;
  ruleIndex: number;
  onChange: (rule: RecurrenceRule) => void;
  onRemove: () => void;
  showErrors?: boolean;
}

export function RecurrenceRuleForm({ rule, ruleIndex, onChange, onRemove, showErrors = false }: Props) {
  const { t } = useTranslation();

  const dayOptions = [
    { value: '1', label: t('courses.recurrence.dayOfWeek.monday') },
    { value: '2', label: t('courses.recurrence.dayOfWeek.tuesday') },
    { value: '3', label: t('courses.recurrence.dayOfWeek.wednesday') },
    { value: '4', label: t('courses.recurrence.dayOfWeek.thursday') },
    { value: '5', label: t('courses.recurrence.dayOfWeek.friday') },
    { value: '6', label: t('courses.recurrence.dayOfWeek.saturday') },
    { value: '0', label: t('courses.recurrence.dayOfWeek.sunday') },
  ];

  const showLargeCountWarning = rule.endCondition === 'count' && rule.occurrences > 100;

  return (
    <Paper withBorder p="md">
      <Group justify="space-between" mb="sm">
        <Text size="sm" fw={500}>
          {t('courses.recurrence.ruleLabel', { index: ruleIndex + 1 })}
        </Text>
        <ActionIcon color="red" variant="subtle" onClick={onRemove} title={t('courses.recurrence.removeRule')}>
          <IconTrash size={16} />
        </ActionIcon>
      </Group>

      <Stack gap="sm">
        <Select
          label={t('courses.recurrence.dayOfWeek.label')}
          data={dayOptions}
          value={rule.dayOfWeek !== null ? String(rule.dayOfWeek) : null}
          onChange={(v) => onChange({ ...rule, dayOfWeek: v !== null ? Number(v) as DayOfWeek : null })}
          required
          error={showErrors && rule.dayOfWeek === null ? t('common.required') : undefined}
        />

        <TimeInput
          label={t('courses.recurrence.startTime.label')}
          value={rule.startTime}
          onChange={(e) => onChange({ ...rule, startTime: e.currentTarget.value })}
          required
          error={showErrors && !rule.startTime ? t('common.required') : undefined}
        />

        <DatePickerInput
          label={t('courses.recurrence.seriesStartDate.label')}
          value={rule.seriesStartDate}
          onChange={(v) => onChange({ ...rule, seriesStartDate: v })}
          required
          error={showErrors && !rule.seriesStartDate ? t('common.required') : undefined}
        />

        <div>
          <Text size="sm" mb={4}>{t('courses.recurrence.endCondition.label')}</Text>
          <SegmentedControl
            value={rule.endCondition}
            onChange={(v) => onChange({ ...rule, endCondition: v as 'count' | 'date' })}
            data={[
              { value: 'count', label: t('courses.recurrence.endCondition.byCount') },
              { value: 'date', label: t('courses.recurrence.endCondition.byDate') },
            ]}
            fullWidth
          />
        </div>

        {rule.endCondition === 'count' && (
          <>
            <NumberInput
              label={t('courses.recurrence.occurrences.label')}
              value={rule.occurrences}
              onChange={(v) => onChange({ ...rule, occurrences: typeof v === 'number' ? v : 1 })}
              min={1}
              required
              error={showErrors && rule.occurrences < 1 ? t('common.required') : undefined}
            />
            {showLargeCountWarning && (
              <Alert color="yellow" icon={<IconAlertTriangle size={16} />}>
                {t('courses.recurrence.largeCountWarning', { count: rule.occurrences })}
              </Alert>
            )}
          </>
        )}

        {rule.endCondition === 'date' && (
          <DatePickerInput
            label={t('courses.recurrence.endDate.label')}
            value={rule.endDate}
            onChange={(v) => onChange({ ...rule, endDate: v })}
            minDate={rule.seriesStartDate ?? undefined}
            required
            error={showErrors && !rule.endDate ? t('common.required') : undefined}
          />
        )}
      </Stack>
    </Paper>
  );
}
