import {
  Modal, Button, Group, Stack, Switch, Select, Text, Checkbox,
  Loader, Center, Accordion, Alert, Divider
} from '@mantine/core';
import { DatePickerInput } from '@mantine/dates';
import { useState, useEffect } from 'react';
import dayjs from 'dayjs';
import { useTranslation } from 'react-i18next';
import { notifications } from '@mantine/notifications';
import {
  getExportColumns, downloadCoursesExport,
  type ExportColumnDefinition, type ExportColumnGroup
} from './coursesApi';

const SESSION_KEY_COLUMNS = 'export_columns_courses';
const SESSION_KEY_OPTS = 'export_opts_courses';

interface SavedOptions {
  includeParticipants: boolean;
  selectedColumns: string[];
  dateFrom?: string;
  dateTo?: string;
  statusFilter?: string;
}

interface Props {
  opened: boolean;
  onClose: () => void;
}

export function ExportCoursesModal({ opened, onClose }: Props) {
  const { t } = useTranslation();

  const [columns, setColumns] = useState<ExportColumnDefinition[]>([]);
  const [loading, setLoading] = useState(false);
  const [downloading, setDownloading] = useState(false);

  const [includeParticipants, setIncludeParticipants] = useState(false);
  const [selectedColumns, setSelectedColumns] = useState<string[]>([]);
  const [dateFrom, setDateFrom] = useState<Date | string | null>(null);
  const [dateTo, setDateTo] = useState<Date | string | null>(null);
  const [statusFilter, setStatusFilter] = useState<string | null>(null);

  useEffect(() => {
    if (!opened) return;

    setLoading(true);
    getExportColumns()
      .then(({ columns: cols }) => {
        setColumns(cols);

        // Restore saved options
        const savedOpts = sessionStorage.getItem(SESSION_KEY_OPTS);
        const savedCols = sessionStorage.getItem(SESSION_KEY_COLUMNS);

        if (savedOpts) {
          try {
            const opts = JSON.parse(savedOpts) as SavedOptions;
            setIncludeParticipants(opts.includeParticipants ?? false);
            setDateFrom(opts.dateFrom ? new Date(opts.dateFrom) : null);
            setDateTo(opts.dateTo ? new Date(opts.dateTo) : null);
            setStatusFilter(opts.statusFilter ?? null);
          } catch { /* ignore */ }
        }

        if (savedCols) {
          try {
            const parsed = JSON.parse(savedCols) as string[];
            setSelectedColumns(parsed);
          } catch {
            setSelectedColumns(cols.filter(c => c.defaultEnabled).map(c => c.key));
          }
        } else {
          setSelectedColumns(cols.filter(c => c.defaultEnabled).map(c => c.key));
        }
      })
      .catch(() => {
        notifications.show({ message: t('common.error'), color: 'red' });
      })
      .finally(() => setLoading(false));
  }, [opened, t]);

  const visibleColumns = includeParticipants
    ? columns
    : columns.filter(c => !c.requiresParticipants);

  const groups: ExportColumnGroup[] = ['CourseInfo', 'ParticipantInfo', 'CustomFields'];

  const toggleColumn = (key: string) => {
    setSelectedColumns(prev =>
      prev.includes(key) ? prev.filter(k => k !== key) : [...prev, key]
    );
  };

  const selectAll = (group: ExportColumnGroup) => {
    const groupKeys = visibleColumns.filter(c => c.group === group).map(c => c.key);
    setSelectedColumns(prev => [...new Set([...prev, ...groupKeys])]);
  };

  const deselectAll = (group: ExportColumnGroup) => {
    const groupKeys = new Set(visibleColumns.filter(c => c.group === group).map(c => c.key));
    setSelectedColumns(prev => prev.filter(k => !groupKeys.has(k)));
  };

  const activeSelectedColumns = selectedColumns.filter(k =>
    visibleColumns.some(c => c.key === k)
  );
  const noColumnsSelected = activeSelectedColumns.length === 0;

  const toDateStr = (d: Date | string | null | undefined): string | undefined =>
    d ? dayjs(d).format('YYYY-MM-DD') : undefined;

  const handleDownload = async () => {
    // Save options
    const opts: SavedOptions = {
      includeParticipants,
      selectedColumns: activeSelectedColumns,
      dateFrom: toDateStr(dateFrom),
      dateTo: toDateStr(dateTo),
      statusFilter: statusFilter ?? undefined,
    };
    sessionStorage.setItem(SESSION_KEY_OPTS, JSON.stringify(opts));
    sessionStorage.setItem(SESSION_KEY_COLUMNS, JSON.stringify(activeSelectedColumns));

    setDownloading(true);
    try {
      await downloadCoursesExport({
        includeParticipants,
        columns: activeSelectedColumns,
        dateFrom: toDateStr(dateFrom),
        dateTo: toDateStr(dateTo),
        status: statusFilter ?? undefined,
      });
      onClose();
    } catch {
      notifications.show({ message: t('common.error'), color: 'red' });
    } finally {
      setDownloading(false);
    }
  };

  const statusOptions = [
    { value: 'Draft', label: t('courses.statuses.Draft') },
    { value: 'Active', label: t('courses.statuses.Active') },
    { value: 'Cancelled', label: t('courses.statuses.Cancelled') },
    { value: 'Completed', label: t('courses.statuses.Completed') },
  ];

  return (
    <Modal opened={opened} onClose={onClose} title={t('export.title')} size="md">
      {loading ? (
        <Center py="xl"><Loader /></Center>
      ) : (
        <Stack gap="md">
          <Switch
            label={t('export.includeParticipants')}
            checked={includeParticipants}
            onChange={e => setIncludeParticipants(e.currentTarget.checked)}
          />

          <Group grow>
            <DatePickerInput
              label={t('export.dateFrom')}
              value={dateFrom ? dayjs(dateFrom).toDate() : null}
              onChange={setDateFrom}
              clearable
              valueFormat="YYYY-MM-DD"
            />
            <DatePickerInput
              label={t('export.dateTo')}
              value={dateTo ? dayjs(dateTo).toDate() : null}
              onChange={setDateTo}
              clearable
              valueFormat="YYYY-MM-DD"
            />
          </Group>

          <Select
            label={t('export.statusFilter')}
            data={statusOptions}
            value={statusFilter}
            onChange={setStatusFilter}
            clearable
            placeholder={t('courses.filters.status')}
          />

          <Divider />

          <Text fw={500} size="sm">{t('export.columns.title')}</Text>

          <Accordion multiple>
            {groups.map(group => {
              const groupCols = visibleColumns.filter(c => c.group === group);
              if (groupCols.length === 0) return null;

              return (
                <Accordion.Item key={group} value={group}>
                  <Accordion.Control>
                    <Text size="sm" fw={500}>{t(`export.groups.${group}`)}</Text>
                  </Accordion.Control>
                  <Accordion.Panel>
                    <Stack gap="xs">
                      <Group gap="xs">
                        <Button
                          variant="subtle" size="compact-xs"
                          onClick={() => selectAll(group)}
                        >
                          {t('export.columns.selectAll')}
                        </Button>
                        <Button
                          variant="subtle" size="compact-xs" color="gray"
                          onClick={() => deselectAll(group)}
                        >
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
            <Button
              onClick={handleDownload}
              loading={downloading}
              disabled={noColumnsSelected}
            >
              {t('export.download')}
            </Button>
          </Group>
        </Stack>
      )}
    </Modal>
  );
}
