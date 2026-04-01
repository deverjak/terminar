import {
  Table, Badge, ActionIcon, Group, Text, Button, Stack, Alert, Paper,
} from '@mantine/core';
import { DateTimePicker } from '@mantine/dates';
import { useTranslation } from 'react-i18next';
import { useState } from 'react';
import {
  IconTrash, IconEdit, IconCheck, IconX, IconPlus, IconAlertTriangle,
} from '@tabler/icons-react';
import type { SessionPreviewEntry } from '../types';

interface Props {
  entries: SessionPreviewEntry[];
  onDelete: (key: string) => void;
  onEdit: (key: string, scheduledAt: Date) => void;
  onAddManual: (scheduledAt: Date) => void;
}

export function SessionPreviewPanel({ entries, onDelete, onEdit, onAddManual }: Props) {
  const { t } = useTranslation();
  const [editingKey, setEditingKey] = useState<string | null>(null);
  const [editValue, setEditValue] = useState<string | null>(null); // "YYYY-MM-DD HH:mm:ss" from DateTimePicker
  const [addingManual, setAddingManual] = useState(false);
  const [manualDate, setManualDate] = useState<string | null>(null); // "YYYY-MM-DD HH:mm:ss" from DateTimePicker

  const visible = entries.filter((e) => !e.isDeleted);
  const hasDuplicates = visible.some((e) => e.isDuplicate);

  const handleEditStart = (entry: SessionPreviewEntry) => {
    setEditingKey(entry.key);
    // Convert Date → "YYYY-MM-DD HH:mm:ss" for DateTimePicker value
    const d = entry.scheduledAt;
    const pad = (n: number) => String(n).padStart(2, '0');
    setEditValue(
      `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())} ${pad(d.getHours())}:${pad(d.getMinutes())}:00`
    );
  };

  const handleEditConfirm = () => {
    if (editingKey && editValue) {
      onEdit(editingKey, new Date(editValue));
    }
    setEditingKey(null);
    setEditValue(null);
  };

  const handleEditCancel = () => {
    setEditingKey(null);
    setEditValue(null);
  };

  const handleAddManualConfirm = () => {
    if (manualDate) {
      onAddManual(new Date(manualDate));
      setManualDate(null);
      setAddingManual(false);
    }
  };

  const formatDateTime = (date: Date) =>
    date.toLocaleDateString(undefined, {
      weekday: 'short',
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });

  return (
    <Stack gap="sm">
      <Group justify="space-between">
        <Text fw={500}>{t('courses.recurrence.preview.title')}</Text>
        <Text size="sm" c="dimmed">
          {t('courses.recurrence.preview.sessionCount', { count: visible.length })}
        </Text>
      </Group>

      {hasDuplicates && (
        <Alert color="yellow" icon={<IconAlertTriangle size={16} />}>
          {t('courses.recurrence.preview.duplicateAlert')}
        </Alert>
      )}

      {visible.length === 0 && !addingManual ? (
        <Text c="dimmed" size="sm">
          {t('courses.recurrence.preview.empty')}
        </Text>
      ) : (
        <Table withTableBorder withColumnBorders>
          <Table.Thead>
            <Table.Tr>
              <Table.Th>Date &amp; Time</Table.Th>
              <Table.Th style={{ width: 110 }}>Source</Table.Th>
              <Table.Th style={{ width: 80 }}>Actions</Table.Th>
            </Table.Tr>
          </Table.Thead>
          <Table.Tbody>
            {visible.map((entry) => (
              <Table.Tr key={entry.key}>
                <Table.Td>
                  {editingKey === entry.key ? (
                    <Group gap="xs" wrap="nowrap">
                      <DateTimePicker
                        value={editValue}
                        onChange={setEditValue}
                        size="xs"
                        style={{ flex: 1 }}
                      />
                      <ActionIcon size="sm" color="green" variant="subtle" onClick={handleEditConfirm}>
                        <IconCheck size={14} />
                      </ActionIcon>
                      <ActionIcon size="sm" color="gray" variant="subtle" onClick={handleEditCancel}>
                        <IconX size={14} />
                      </ActionIcon>
                    </Group>
                  ) : (
                    <Group gap="xs">
                      <Text size="sm">{formatDateTime(entry.scheduledAt)}</Text>
                      {entry.isDuplicate && (
                        <Badge color="yellow" size="xs">
                          {t('courses.recurrence.preview.duplicateWarning')}
                        </Badge>
                      )}
                    </Group>
                  )}
                </Table.Td>
                <Table.Td>
                  <Badge
                    color={entry.source === 'manual' ? 'blue' : 'gray'}
                    variant="light"
                    size="xs"
                  >
                    {t(`courses.recurrence.sourceBadge.${entry.source}`)}
                  </Badge>
                </Table.Td>
                <Table.Td>
                  {editingKey !== entry.key && (
                    <Group gap={4} wrap="nowrap">
                      <ActionIcon
                        size="sm"
                        variant="subtle"
                        onClick={() => handleEditStart(entry)}
                        title="Edit"
                      >
                        <IconEdit size={14} />
                      </ActionIcon>
                      <ActionIcon
                        size="sm"
                        color="red"
                        variant="subtle"
                        onClick={() => onDelete(entry.key)}
                        title="Delete"
                      >
                        <IconTrash size={14} />
                      </ActionIcon>
                    </Group>
                  )}
                </Table.Td>
              </Table.Tr>
            ))}
          </Table.Tbody>
        </Table>
      )}

      {addingManual ? (
        <Paper withBorder p="sm">
          <Group gap="sm" align="flex-end">
            <DateTimePicker
              label="Date &amp; Time"
              value={manualDate}
              onChange={setManualDate}
              style={{ flex: 1 }}
            />
            <Button size="sm" onClick={handleAddManualConfirm} disabled={!manualDate}>
              Add
            </Button>
            <Button
              size="sm"
              variant="default"
              onClick={() => { setAddingManual(false); setManualDate(null); }}
            >
              Cancel
            </Button>
          </Group>
        </Paper>
      ) : (
        <Button
          variant="light"
          size="xs"
          leftSection={<IconPlus size={14} />}
          onClick={() => setAddingManual(true)}
        >
          {t('courses.recurrence.preview.addManual')}
        </Button>
      )}
    </Stack>
  );
}
