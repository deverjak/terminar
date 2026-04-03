import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Container, Title, Stack, Switch, NumberInput, Button, Divider, Text, Group, TextInput, Table, Modal } from '@mantine/core';
import { useTranslation } from 'react-i18next';
import { notifications } from '@mantine/notifications';
import { useDisclosure } from '@mantine/hooks';
import { useState } from 'react';
import {
  getExcusalSettings, updateExcusalSettings, listWindows, createWindow, deleteWindow,
} from './excusalSettingsApi';

export default function ExcusalSettingsPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [windowModalOpen, { open: openWindowModal, close: closeWindowModal }] = useDisclosure(false);
  const [newWindowName, setNewWindowName] = useState('');
  const [newWindowStart, setNewWindowStart] = useState('');
  const [newWindowEnd, setNewWindowEnd] = useState('');

  const { data: settings } = useQuery({ queryKey: ['excusal-settings'], queryFn: getExcusalSettings });
  const { data: windows } = useQuery({ queryKey: ['excusal-windows'], queryFn: listWindows });

  const [creditEnabled, setCreditEnabled] = useState<boolean | undefined>();
  const [forwardCount, setForwardCount] = useState<number | string | undefined>();
  const [unenrollDays, setUnenrollDays] = useState<number | string | undefined>();
  const [excusalHours, setExcusalHours] = useState<number | string | undefined>();

  const saveSettingsMutation = useMutation({
    mutationFn: () => updateExcusalSettings({
      creditGenerationEnabled: creditEnabled ?? settings?.creditGenerationEnabled,
      forwardWindowCount: (typeof forwardCount === 'number' ? forwardCount : undefined) ?? settings?.forwardWindowCount,
      unenrollmentDeadlineDays: (typeof unenrollDays === 'number' ? unenrollDays : undefined) ?? settings?.unenrollmentDeadlineDays,
      excusalDeadlineHours: (typeof excusalHours === 'number' ? excusalHours : undefined) ?? settings?.excusalDeadlineHours,
    }),
    onSuccess: () => {
      notifications.show({ color: 'green', message: 'Settings saved.' });
      queryClient.invalidateQueries({ queryKey: ['excusal-settings'] });
    },
  });

  const createWindowMutation = useMutation({
    mutationFn: () => createWindow({ name: newWindowName, startDate: newWindowStart, endDate: newWindowEnd }),
    onSuccess: () => {
      notifications.show({ color: 'green', message: 'Window created.' });
      queryClient.invalidateQueries({ queryKey: ['excusal-windows'] });
      closeWindowModal();
      setNewWindowName('');
      setNewWindowStart('');
      setNewWindowEnd('');
    },
  });

  const deleteWindowMutation = useMutation({
    mutationFn: (id: string) => deleteWindow(id),
    onSuccess: () => {
      notifications.show({ color: 'orange', message: 'Window deleted.' });
      queryClient.invalidateQueries({ queryKey: ['excusal-windows'] });
    },
  });

  return (
    <Container size="md" py="xl">
      <Title order={2} mb="lg">{t('excusalSettings.title')}</Title>

      <Stack gap="md">
        <Switch
          label={t('excusalSettings.creditGenerationEnabled')}
          checked={creditEnabled ?? settings?.creditGenerationEnabled ?? false}
          onChange={e => setCreditEnabled(e.currentTarget.checked)}
        />
        <NumberInput
          label={t('excusalSettings.forwardWindowCount')}
          value={forwardCount ?? settings?.forwardWindowCount ?? 2}
          onChange={v => setForwardCount(v)}
          min={1}
        />
        <NumberInput
          label={t('excusalSettings.unenrollmentDeadlineDays')}
          value={unenrollDays ?? settings?.unenrollmentDeadlineDays ?? 14}
          onChange={v => setUnenrollDays(v)}
          min={0}
        />
        <NumberInput
          label={t('excusalSettings.excusalDeadlineHours')}
          value={excusalHours ?? settings?.excusalDeadlineHours ?? 24}
          onChange={v => setExcusalHours(v)}
          min={0}
        />
        <Button onClick={() => saveSettingsMutation.mutate()} loading={saveSettingsMutation.isPending}>
          {t('excusalSettings.saveButton')}
        </Button>
      </Stack>

      <Divider my="xl" />

      <Group justify="space-between" mb="md">
        <Title order={3}>{t('excusalSettings.windows.title')}</Title>
        <Button size="sm" onClick={openWindowModal}>{t('excusalSettings.windows.addButton')}</Button>
      </Group>

      <Table>
        <Table.Thead>
          <Table.Tr>
            <Table.Th>{t('excusalSettings.windows.name')}</Table.Th>
            <Table.Th>{t('excusalSettings.windows.startDate')}</Table.Th>
            <Table.Th>{t('excusalSettings.windows.endDate')}</Table.Th>
            <Table.Th />
          </Table.Tr>
        </Table.Thead>
        <Table.Tbody>
          {windows?.map(w => (
            <Table.Tr key={w.windowId}>
              <Table.Td>{w.name}</Table.Td>
              <Table.Td>{w.startDate}</Table.Td>
              <Table.Td>{w.endDate}</Table.Td>
              <Table.Td>
                <Button
                  size="xs"
                  color="red"
                  variant="subtle"
                  loading={deleteWindowMutation.isPending}
                  onClick={() => {
                    if (confirm(t('excusalSettings.windows.deleteConfirm')))
                      deleteWindowMutation.mutate(w.windowId);
                  }}
                >
                  {t('excusalSettings.windows.deleteButton')}
                </Button>
              </Table.Td>
            </Table.Tr>
          ))}
        </Table.Tbody>
      </Table>

      <Modal opened={windowModalOpen} onClose={closeWindowModal} title={t('excusalSettings.windows.addButton')}>
        <Stack>
          <TextInput
            label={t('excusalSettings.windows.name')}
            value={newWindowName}
            onChange={e => setNewWindowName(e.target.value)}
            required
          />
          <TextInput
            label={t('excusalSettings.windows.startDate')}
            type="date"
            value={newWindowStart}
            onChange={e => setNewWindowStart(e.target.value)}
            required
          />
          <TextInput
            label={t('excusalSettings.windows.endDate')}
            type="date"
            value={newWindowEnd}
            onChange={e => setNewWindowEnd(e.target.value)}
            required
          />
          <Button onClick={() => createWindowMutation.mutate()} loading={createWindowMutation.isPending}>
            {t('excusalSettings.windows.addButton')}
          </Button>
        </Stack>
      </Modal>
    </Container>
  );
}
