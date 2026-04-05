import {
  Modal, Button, Group, Stack, Text, Checkbox,
  Loader, Center, Accordion, Alert
} from '@mantine/core';
import { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { notifications } from '@mantine/notifications';
import { getExportColumns, type ExportColumnDefinition, type ExportColumnGroup } from '../courses/coursesApi';
import { downloadRosterExport } from './registrationsApi';

const SESSION_KEY = 'export_columns_roster';

interface Props {
  opened: boolean;
  onClose: () => void;
  courseId: string;
}

export function ExportRosterModal({ opened, onClose, courseId }: Props) {
  const { t } = useTranslation();

  const [columns, setColumns] = useState<ExportColumnDefinition[]>([]);
  const [loading, setLoading] = useState(false);
  const [downloading, setDownloading] = useState(false);
  const [selectedColumns, setSelectedColumns] = useState<string[]>([]);

  const participantGroups: ExportColumnGroup[] = ['ParticipantInfo', 'CustomFields'];

  useEffect(() => {
    if (!opened) return;

    setLoading(true);
    getExportColumns()
      .then(({ columns: cols }) => {
        const participantCols = cols.filter(c => c.requiresParticipants);
        setColumns(participantCols);

        const saved = sessionStorage.getItem(SESSION_KEY);
        if (saved) {
          try {
            setSelectedColumns(JSON.parse(saved) as string[]);
          } catch {
            setSelectedColumns(participantCols.filter(c => c.defaultEnabled).map(c => c.key));
          }
        } else {
          setSelectedColumns(participantCols.filter(c => c.defaultEnabled).map(c => c.key));
        }
      })
      .catch(() => notifications.show({ message: t('common.error'), color: 'red' }))
      .finally(() => setLoading(false));
  }, [opened, t]);

  const toggleColumn = (key: string) => {
    setSelectedColumns(prev =>
      prev.includes(key) ? prev.filter(k => k !== key) : [...prev, key]
    );
  };

  const selectAll = (group: ExportColumnGroup) => {
    const groupKeys = columns.filter(c => c.group === group).map(c => c.key);
    setSelectedColumns(prev => [...new Set([...prev, ...groupKeys])]);
  };

  const deselectAll = (group: ExportColumnGroup) => {
    const groupKeys = new Set(columns.filter(c => c.group === group).map(c => c.key));
    setSelectedColumns(prev => prev.filter(k => !groupKeys.has(k)));
  };

  const noColumnsSelected = selectedColumns.length === 0;

  const handleDownload = async () => {
    sessionStorage.setItem(SESSION_KEY, JSON.stringify(selectedColumns));
    setDownloading(true);
    try {
      await downloadRosterExport(courseId, { columns: selectedColumns });
      onClose();
    } catch {
      notifications.show({ message: t('common.error'), color: 'red' });
    } finally {
      setDownloading(false);
    }
  };

  return (
    <Modal opened={opened} onClose={onClose} title={t('export.title')} size="md">
      {loading ? (
        <Center py="xl"><Loader /></Center>
      ) : (
        <Stack gap="md">
          <Text fw={500} size="sm">{t('export.columns.title')}</Text>

          <Accordion multiple>
            {participantGroups.map(group => {
              const groupCols = columns.filter(c => c.group === group);
              if (groupCols.length === 0) return null;

              return (
                <Accordion.Item key={group} value={group}>
                  <Accordion.Control>
                    <Text size="sm" fw={500}>{t(`export.groups.${group}`)}</Text>
                  </Accordion.Control>
                  <Accordion.Panel>
                    <Stack gap="xs">
                      <Group gap="xs">
                        <Button variant="subtle" size="compact-xs" onClick={() => selectAll(group)}>
                          {t('export.columns.selectAll')}
                        </Button>
                        <Button variant="subtle" size="compact-xs" color="gray" onClick={() => deselectAll(group)}>
                          {t('export.columns.deselectAll')}
                        </Button>
                      </Group>
                      {groupCols.map(col => (
                        <Checkbox
                          key={col.key}
                          label={col.label ?? t(col.labelKey)}
                          checked={selectedColumns.includes(col.key)}
                          onChange={() => toggleColumn(col.key)}
                        />
                      ))}
                    </Stack>
                  </Accordion.Panel>
                </Accordion.Item>
              );
            })}
          </Accordion>

          {noColumnsSelected && (
            <Alert color="red" variant="light">
              {t('export.validation.noColumns')}
            </Alert>
          )}

          <Group justify="flex-end">
            <Button variant="subtle" onClick={onClose}>{t('common.cancel')}</Button>
            <Button onClick={handleDownload} loading={downloading} disabled={noColumnsSelected}>
              {t('export.download')}
            </Button>
          </Group>
        </Stack>
      )}
    </Modal>
  );
}
