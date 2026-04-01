import {
  Title, Group, Button, Table, Text, Stack, Anchor,
  Select, Skeleton, Center, Pagination
} from '@mantine/core';
import { useDisclosure } from '@mantine/hooks';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useParams, Link } from 'react-router';
import { useTranslation } from 'react-i18next';
import { useState, useEffect } from 'react';
import { notifications } from '@mantine/notifications';
import dayjs from 'dayjs';
import { getRoster, cancelRegistration } from './registrationsApi';
import { StatusBadge } from '@/shared/components/StatusBadge';
import { ConfirmModal } from '@/shared/components/ConfirmModal';
import { CreateRegistrationModal } from './CreateRegistrationModal';
import { usePagination } from '@/shared/hooks/usePagination';
import { ApiError } from '@/shared/api/client';

export function CourseRosterPage() {
  const { t } = useTranslation();
  const { courseId } = useParams<{ courseId: string }>();
  const queryClient = useQueryClient();
  const { page, pageSize, setPage } = usePagination(20);
  const [statusFilter, setStatusFilter] = useState<string | null>(null);
  const [addModalOpen, { open: openAdd, close: closeAdd }] = useDisclosure(false);
  const [cancelTarget, setCancelTarget] = useState<string | null>(null);
  const [cancelling, setCancelling] = useState(false);

  useEffect(() => {
    document.title = `${t('registrations.title')} — Termínář`;
  }, [t]);

  const { data, isLoading } = useQuery({
    queryKey: ['roster', courseId, page, pageSize, statusFilter],
    queryFn: () => getRoster(courseId!, page, pageSize, statusFilter ?? undefined),
    enabled: !!courseId,
  });

  const handleCancelRegistration = async () => {
    if (!courseId || !cancelTarget) return;
    setCancelling(true);
    try {
      await cancelRegistration(courseId, cancelTarget);
      notifications.show({
        title: t('registrations.title'),
        message: t('registrations.cancelSuccess'),
        color: 'orange',
      });
      await queryClient.invalidateQueries({ queryKey: ['roster', courseId] });
      setCancelTarget(null);
    } catch (err) {
      const message = err instanceof ApiError ? err.message : t('common.error');
      notifications.show({ title: 'Error', message, color: 'red' });
    } finally {
      setCancelling(false);
    }
  };

  if (isLoading) {
    return (
      <Stack gap="md">
        <Skeleton height={32} width={300} />
        <Skeleton height={200} />
      </Stack>
    );
  }

  const totalPages = data ? Math.ceil(data.total / pageSize) : 1;

  return (
    <Stack gap="md">
      <Group gap="xs">
        <Anchor component={Link} to={`/app/courses/${courseId}`} size="sm">
          ← {t('courses.title')}
        </Anchor>
      </Group>

      <Group justify="space-between" align="center">
        <Title order={2}>{t('registrations.title')}</Title>
        <Button onClick={openAdd}>{t('registrations.addRegistration')}</Button>
      </Group>

      <Select
        placeholder={t('registrations.filter.all')}
        value={statusFilter}
        onChange={(v) => { setStatusFilter(v); setPage(1); }}
        clearable
        data={[
          { value: 'Confirmed', label: t('registrations.filter.confirmed') },
          { value: 'Cancelled', label: t('registrations.filter.cancelled') },
        ]}
        style={{ width: 200 }}
      />

      {!data?.items || data.items.length === 0 ? (
        <Center py="xl">
          <Text c="dimmed">{t('registrations.noRegistrations')}</Text>
        </Center>
      ) : (
        <>
          <Table striped highlightOnHover>
            <Table.Thead>
              <Table.Tr>
                <Table.Th>{t('registrations.columns.name')}</Table.Th>
                <Table.Th>{t('registrations.columns.email')}</Table.Th>
                <Table.Th>{t('registrations.columns.source')}</Table.Th>
                <Table.Th>{t('registrations.columns.status')}</Table.Th>
                <Table.Th>{t('registrations.columns.registeredAt')}</Table.Th>
                <Table.Th></Table.Th>
              </Table.Tr>
            </Table.Thead>
            <Table.Tbody>
              {data.items.map((reg) => (
                <Table.Tr key={reg.registrationId}>
                  <Table.Td>{reg.participantName}</Table.Td>
                  <Table.Td>{reg.participantEmail}</Table.Td>
                  <Table.Td>
                    <StatusBadge type="registrationSource" value={reg.registrationSource} />
                  </Table.Td>
                  <Table.Td>
                    <StatusBadge type="registration" value={reg.status} />
                  </Table.Td>
                  <Table.Td>{dayjs(reg.registeredAt).format('DD MMM YYYY HH:mm')}</Table.Td>
                  <Table.Td>
                    {reg.status === 'Confirmed' && (
                      <Button
                        size="xs"
                        variant="subtle"
                        color="red"
                        onClick={() => setCancelTarget(reg.registrationId)}
                      >
                        {t('common.cancel')}
                      </Button>
                    )}
                  </Table.Td>
                </Table.Tr>
              ))}
            </Table.Tbody>
          </Table>
          {totalPages > 1 && (
            <Pagination total={totalPages} value={page} onChange={setPage} />
          )}
        </>
      )}

      <CreateRegistrationModal
        opened={addModalOpen}
        onClose={closeAdd}
        onSuccess={() => queryClient.invalidateQueries({ queryKey: ['roster', courseId] })}
        courseId={courseId!}
      />

      <ConfirmModal
        opened={!!cancelTarget}
        title={t('common.confirm')}
        message={t('registrations.cancelConfirm')}
        onConfirm={handleCancelRegistration}
        onCancel={() => setCancelTarget(null)}
        loading={cancelling}
      />
    </Stack>
  );
}
