import {
  Title, Button, Group, Table, Text, Stack, Alert,
  Skeleton, Center, Anchor
} from '@mantine/core';
import { useDisclosure } from '@mantine/hooks';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { Link } from 'react-router';
import { useTranslation } from 'react-i18next';
import { useState, useEffect } from 'react';
import { notifications } from '@mantine/notifications';
import dayjs from 'dayjs';
import { listStaff, deactivateStaff } from './staffApi';
import { StatusBadge } from '@/shared/components/StatusBadge';
import { ConfirmModal } from '@/shared/components/ConfirmModal';
import { CreateStaffModal } from './CreateStaffModal';
import { useAuth } from '@/features/auth/useAuth';
import { ApiError } from '@/shared/api/client';

export function StaffListPage() {
  const { t } = useTranslation();
  const { session } = useAuth();
  const queryClient = useQueryClient();
  const [addModalOpen, { open: openAdd, close: closeAdd }] = useDisclosure(false);
  const [deactivateTarget, setDeactivateTarget] = useState<string | null>(null);
  const [deactivating, setDeactivating] = useState(false);

  useEffect(() => {
    document.title = `${t('staff.title')} — Termínář`;
  }, [t]);

  const { data: staffUsers, isLoading } = useQuery({
    queryKey: ['staff'],
    queryFn: listStaff,
    enabled: session?.role === 'Admin',
  });

  if (session?.role !== 'Admin') {
    return (
      <Stack gap="md">
        <Alert color="red" title={t('staff.title')}>
          {t('staff.accessDenied')}
        </Alert>
        <Anchor component={Link} to="/app/courses">{t('common.back')}</Anchor>
      </Stack>
    );
  }

  if (isLoading) {
    return (
      <Stack gap="md">
        <Skeleton height={36} width={150} />
        <Skeleton height={200} />
      </Stack>
    );
  }

  const handleDeactivate = async () => {
    if (!deactivateTarget) return;
    setDeactivating(true);
    try {
      await deactivateStaff(deactivateTarget);
      notifications.show({
        title: t('staff.title'),
        message: t('staff.deactivateSuccess'),
        color: 'orange',
      });
      await queryClient.invalidateQueries({ queryKey: ['staff'] });
      setDeactivateTarget(null);
    } catch (err) {
      const message = err instanceof ApiError ? err.message : t('common.error');
      notifications.show({ title: 'Error', message, color: 'red' });
    } finally {
      setDeactivating(false);
    }
  };

  return (
    <Stack gap="md">
      <Group justify="space-between" align="center">
        <Title order={2}>{t('staff.title')}</Title>
        <Button onClick={openAdd}>{t('staff.addStaff')}</Button>
      </Group>

      {!staffUsers || staffUsers.length === 0 ? (
        <Center py="xl">
          <Text c="dimmed">{t('staff.noStaff')}</Text>
        </Center>
      ) : (
        <Table striped highlightOnHover>
          <Table.Thead>
            <Table.Tr>
              <Table.Th>{t('staff.columns.username')}</Table.Th>
              <Table.Th>{t('staff.columns.email')}</Table.Th>
              <Table.Th>{t('staff.columns.role')}</Table.Th>
              <Table.Th>{t('staff.columns.status')}</Table.Th>
              <Table.Th>{t('staff.columns.createdAt')}</Table.Th>
              <Table.Th></Table.Th>
            </Table.Tr>
          </Table.Thead>
          <Table.Tbody>
            {staffUsers.map((user) => (
              <Table.Tr key={user.staffUserId}>
                <Table.Td>{user.username}</Table.Td>
                <Table.Td>{user.email}</Table.Td>
                <Table.Td>
                  <StatusBadge type="role" value={user.role} />
                </Table.Td>
                <Table.Td>
                  <StatusBadge type="staff" value={user.status} />
                </Table.Td>
                <Table.Td>{dayjs(user.createdAt).format('DD MMM YYYY')}</Table.Td>
                <Table.Td>
                  {user.status === 'Active' && (
                    <Button
                      size="xs"
                      variant="subtle"
                      color="red"
                      onClick={() => setDeactivateTarget(user.staffUserId)}
                    >
                      {t('staff.deactivate')}
                    </Button>
                  )}
                </Table.Td>
              </Table.Tr>
            ))}
          </Table.Tbody>
        </Table>
      )}

      <CreateStaffModal
        opened={addModalOpen}
        onClose={closeAdd}
        onSuccess={() => queryClient.invalidateQueries({ queryKey: ['staff'] })}
      />

      <ConfirmModal
        opened={!!deactivateTarget}
        title={t('staff.deactivate')}
        message={t('staff.deactivateConfirm')}
        onConfirm={handleDeactivate}
        onCancel={() => setDeactivateTarget(null)}
        loading={deactivating}
      />
    </Stack>
  );
}
