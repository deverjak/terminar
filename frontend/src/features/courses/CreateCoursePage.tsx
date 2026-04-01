import {
  Title, TextInput, Textarea, Select, NumberInput, Button,
  Stack, Group, Paper, ActionIcon, Text, SegmentedControl, Modal,
} from '@mantine/core';
import { DateTimePicker } from '@mantine/dates';
import { useForm } from '@mantine/form';
import { useNavigate, Link } from 'react-router';
import { useTranslation } from 'react-i18next';
import { useState, useEffect, useMemo } from 'react';
import { notifications } from '@mantine/notifications';
import { useQueryClient } from '@tanstack/react-query';
import { IconTrash, IconPlus } from '@tabler/icons-react';
import { createCourse } from './coursesApi';
import { ApiError } from '@/shared/api/client';
import type { CourseType, RegistrationMode, SessionInput, RecurrenceRule, SessionPreviewEntry } from './types';
import { generateSessions } from './utils/recurrenceEngine';
import { RecurrenceRuleForm } from './components/RecurrenceRuleForm';
import { SessionPreviewPanel } from './components/SessionPreviewPanel';

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

function generateId(): string {
  return `${Date.now()}-${Math.random().toString(36).slice(2, 9)}`;
}

function createEmptyRule(): RecurrenceRule {
  return {
    id: generateId(),
    dayOfWeek: null,
    startTime: '',
    seriesStartDate: null,
    endCondition: 'count',
    occurrences: 10,
    endDate: null,
  };
}

export function CreateCoursePage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [submitting, setSubmitting] = useState(false);

  // --- Recurrence state ---
  const [mode, setMode] = useState<'manual' | 'recurrence'>('manual');
  const [pendingMode, setPendingMode] = useState<'manual' | 'recurrence' | null>(null);
  const [rules, setRules] = useState<RecurrenceRule[]>([createEmptyRule()]);
  const [sessionOverrides, setSessionOverrides] = useState<Map<string, { isDeleted?: boolean; scheduledAt?: Date }>>(new Map());
  const [manualAdditions, setManualAdditions] = useState<SessionPreviewEntry[]>([]);
  const [recurrenceDuration, setRecurrenceDuration] = useState<number>(60);
  const [recurrenceLocation, setRecurrenceLocation] = useState<string>('');
  const [showRuleErrors, setShowRuleErrors] = useState(false);

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
      title: (v) => (v.trim().length === 0 ? t('common.required') : null),
      capacity: (v) => (v >= 1 ? null : 'Must be at least 1'),
      sessions: {
        scheduledAt: (v) => {
          if (!v) return t('common.required');
          if (v <= new Date()) return t('courses.form.futureDateRequired');
          return null;
        },
        durationMinutes: (v) => (v >= 1 ? null : 'Must be at least 1'),
      },
    },
  });

  // --- Derived preview list ---
  const previewList = useMemo((): SessionPreviewEntry[] => {
    const generated = generateSessions(rules);

    const entries: SessionPreviewEntry[] = generated.map((g) => {
      const override = sessionOverrides.get(g.key);
      return {
        key: g.key,
        scheduledAt: override?.scheduledAt ?? g.scheduledAt,
        durationMinutes: recurrenceDuration,
        location: recurrenceLocation,
        source: 'recurrence' as const,
        ruleId: g.ruleId,
        isDeleted: override?.isDeleted ?? false,
        isDuplicate: false,
      };
    });

    const additions: SessionPreviewEntry[] = manualAdditions.map((m) => ({
      ...m,
      durationMinutes: recurrenceDuration,
      location: recurrenceLocation,
    }));

    const all = [...entries, ...additions];
    all.sort((a, b) => a.scheduledAt.getTime() - b.scheduledAt.getTime());

    // Flag duplicates at minute precision
    const countByTime = new Map<string, number>();
    for (const e of all) {
      if (e.isDeleted) continue;
      const k = `${e.scheduledAt.getFullYear()}-${e.scheduledAt.getMonth()}-${e.scheduledAt.getDate()}-${e.scheduledAt.getHours()}-${e.scheduledAt.getMinutes()}`;
      countByTime.set(k, (countByTime.get(k) ?? 0) + 1);
    }

    return all.map((e) => {
      const k = `${e.scheduledAt.getFullYear()}-${e.scheduledAt.getMonth()}-${e.scheduledAt.getDate()}-${e.scheduledAt.getHours()}-${e.scheduledAt.getMinutes()}`;
      return { ...e, isDuplicate: !e.isDeleted && (countByTime.get(k) ?? 0) > 1 };
    });
  }, [rules, sessionOverrides, manualAdditions, recurrenceDuration, recurrenceLocation]);

  const visibleSessions = previewList.filter((e) => !e.isDeleted);

  // --- Mode switch ---
  const handleModeChange = (newMode: 'manual' | 'recurrence') => {
    if (newMode === mode) return;
    const hasManualSessions = mode === 'manual' && form.values.sessions.length > 0;
    const hasRecurrenceSessions = mode === 'recurrence' && visibleSessions.length > 0;
    if (hasManualSessions || hasRecurrenceSessions) {
      setPendingMode(newMode);
    } else {
      applyModeSwitch(newMode);
    }
  };

  const applyModeSwitch = (newMode: 'manual' | 'recurrence') => {
    if (newMode === 'manual' && mode === 'recurrence') {
      // T018: copy recurrence sessions into manual form fields
      const converted: SessionFormValue[] = visibleSessions.map((e) => ({
        scheduledAt: e.scheduledAt,
        durationMinutes: e.durationMinutes,
        location: e.location,
      }));
      form.setFieldValue('sessions', converted);
    } else if (newMode === 'recurrence') {
      form.setFieldValue('sessions', []);
      setSessionOverrides(new Map());
      setManualAdditions([]);
    }
    setMode(newMode);
  };

  const confirmModeSwitch = () => {
    if (!pendingMode) return;
    applyModeSwitch(pendingMode);
    setPendingMode(null);
  };

  // --- Session override handlers (T010, T011) ---
  const handleSessionDelete = (key: string) => {
    setSessionOverrides((prev) => {
      const next = new Map(prev);
      next.set(key, { ...(next.get(key) ?? {}), isDeleted: true });
      return next;
    });
  };

  const handleSessionEdit = (key: string, scheduledAt: Date) => {
    setSessionOverrides((prev) => {
      const next = new Map(prev);
      next.set(key, { ...(next.get(key) ?? {}), scheduledAt });
      return next;
    });
  };

  // --- Manual addition handler (T017) ---
  const handleAddManual = (scheduledAt: Date) => {
    const entry: SessionPreviewEntry = {
      key: `manual-${generateId()}`,
      scheduledAt,
      durationMinutes: recurrenceDuration,
      location: recurrenceLocation,
      source: 'manual',
      ruleId: null,
      isDeleted: false,
      isDuplicate: false,
    };
    setManualAdditions((prev) => [...prev, entry]);
  };

  // --- Rule management (T015, T016) ---
  const handleRuleChange = (id: string, updated: RecurrenceRule) => {
    setRules((prev) => prev.map((r) => (r.id === id ? updated : r)));
  };

  const handleRuleRemove = (id: string) => {
    setRules((prev) => prev.filter((r) => r.id !== id));
    setSessionOverrides((prev) => {
      const next = new Map(prev);
      for (const key of [...next.keys()]) {
        if (key.startsWith(`${id}-`)) next.delete(key);
      }
      return next;
    });
  };

  const handleAddRule = () => {
    setRules((prev) => [...prev, createEmptyRule()]);
  };

  // --- Form submission (T009, T019) ---
  const handleSubmit = async (values: CreateCourseFormValues) => {
    if (mode === 'recurrence') {
      setShowRuleErrors(true);
      if (visibleSessions.length === 0) {
        notifications.show({
          title: t('common.error'),
          message: t('courses.recurrence.preview.noSessionsError'),
          color: 'red',
        });
        return;
      }
    }

    setSubmitting(true);
    try {
      let sessions: SessionInput[];

      if (mode === 'manual') {
        sessions = values.sessions.map((s) => ({
          scheduledAt: new Date(s.scheduledAt as unknown as number).toISOString(),
          durationMinutes: s.durationMinutes,
          location: s.location || undefined,
        }));
      } else {
        sessions = visibleSessions.map((e) => ({
          scheduledAt: e.scheduledAt.toISOString(),
          durationMinutes: e.durationMinutes,
          location: e.location || undefined,
        }));
      }

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

  const addSession = () => {
    form.insertListItem('sessions', { scheduledAt: null, durationMinutes: 60, location: '' });
  };

  const removeSession = (index: number) => {
    form.removeListItem('sessions', index);
  };

  return (
    <>
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

              {/* Sessions section */}
              <div>
                <Group justify="space-between" mb="sm">
                  <Text fw={500}>{t('courses.form.sessions')}</Text>
                  <SegmentedControl
                    value={mode}
                    onChange={(v) => handleModeChange(v as 'manual' | 'recurrence')}
                    data={[
                      { value: 'manual', label: t('courses.recurrence.modeToggle.manual') },
                      { value: 'recurrence', label: t('courses.recurrence.modeToggle.recurrent') },
                    ]}
                    size="xs"
                  />
                </Group>

                {mode === 'manual' ? (
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
                    <Button
                      variant="light"
                      leftSection={<IconPlus size={16} />}
                      onClick={addSession}
                      mt="sm"
                    >
                      {t('courses.form.addSession')}
                    </Button>
                  </Stack>
                ) : (
                  <Stack gap="md">
                    {/* Shared recurrence fields */}
                    <Group gap="md" grow>
                      <NumberInput
                        label={t('courses.recurrence.duration.label')}
                        value={recurrenceDuration}
                        onChange={(v) => setRecurrenceDuration(typeof v === 'number' ? v : 60)}
                        min={1}
                        required
                      />
                      <TextInput
                        label={t('courses.recurrence.location.label')}
                        value={recurrenceLocation}
                        onChange={(e) => setRecurrenceLocation(e.currentTarget.value)}
                      />
                    </Group>

                    {/* Recurrence rules (T015, T016) */}
                    <Stack gap="sm">
                      {rules.map((rule, idx) => (
                        <RecurrenceRuleForm
                          key={rule.id}
                          rule={rule}
                          ruleIndex={idx}
                          onChange={(updated) => handleRuleChange(rule.id, updated)}
                          onRemove={() => handleRuleRemove(rule.id)}
                          showErrors={showRuleErrors}
                        />
                      ))}
                    </Stack>

                    <Button
                      variant="light"
                      size="sm"
                      leftSection={<IconPlus size={16} />}
                      onClick={handleAddRule}
                    >
                      {t('courses.recurrence.addRule')}
                    </Button>

                    {/* Session preview (T008–T013, T017) */}
                    <Paper withBorder p="md">
                      <SessionPreviewPanel
                        entries={previewList}
                        onDelete={handleSessionDelete}
                        onEdit={handleSessionEdit}
                        onAddManual={handleAddManual}
                      />
                    </Paper>
                  </Stack>
                )}
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

      {/* Mode-switch confirmation modal (T014) */}
      <Modal
        opened={pendingMode !== null}
        onClose={() => setPendingMode(null)}
        title={t('courses.recurrence.switchMode.confirmTitle')}
      >
        <Stack gap="md">
          <Text>{t('courses.recurrence.switchMode.confirmMessage')}</Text>
          <Group justify="flex-end">
            <Button variant="default" onClick={() => setPendingMode(null)}>
              {t('courses.recurrence.switchMode.cancel')}
            </Button>
            <Button color="red" onClick={confirmModeSwitch}>
              {t('courses.recurrence.switchMode.confirm')}
            </Button>
          </Group>
        </Stack>
      </Modal>
    </>
  );
}
