import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Container, Title, Table, Badge, Button, Group, Text, Modal, TextInput, Stack } from '@mantine/core';
import { useTranslation } from 'react-i18next';
import { notifications } from '@mantine/notifications';
import { listCredits, updateCredit, deleteCredit, type ExcusalCreditItem } from './excusalCreditsApi';
import { useDisclosure } from '@mantine/hooks';

export default function ExcusalCreditsPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [page] = useState(1);
  const [editing, setEditing] = useState<ExcusalCreditItem | null>(null);
  const [editTags, setEditTags] = useState('');
  const [editModalOpen, { open: openEdit, close: closeEdit }] = useDisclosure(false);

  const { data, isLoading } = useQuery({
    queryKey: ['excusal-credits', page],
    queryFn: () => listCredits(page),
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, tags }: { id: string; tags: string[] }) => updateCredit(id, { tags }),
    onSuccess: () => {
      notifications.show({ color: 'green', message: 'Credit updated.' });
      queryClient.invalidateQueries({ queryKey: ['excusal-credits'] });
      closeEdit();
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteCredit(id),
    onSuccess: () => {
      notifications.show({ color: 'orange', message: 'Credit cancelled.' });
      queryClient.invalidateQueries({ queryKey: ['excusal-credits'] });
    },
  });

  const handleEdit = (credit: ExcusalCreditItem) => {
    setEditing(credit);
    setEditTags(credit.tags.join(', '));
    openEdit();
  };

  const handleSaveEdit = () => {
    if (!editing) return;
    const newTags = editTags.split(',').map(tag => tag.trim()).filter(Boolean);
    updateMutation.mutate({ id: editing.creditId, tags: newTags });
  };

  const statusColor = (s: string) =>
    ({ Active: 'green', Redeemed: 'blue', Expired: 'gray', Cancelled: 'red' } as Record<string, string>)[s] ?? 'gray';

  return (
    <Container size="xl" py="xl">
      <Title order={2} mb="lg">{t('excusalAdmin.credits.title')}</Title>

      {isLoading ? <Text>{t('common.loading')}</Text> : (
        <Table striped highlightOnHover>
          <Table.Thead>
            <Table.Tr>
              <Table.Th>{t('excusalAdmin.credits.participant')}</Table.Th>
              <Table.Th>{t('excusalAdmin.credits.tags')}</Table.Th>
              <Table.Th>{t('excusalAdmin.credits.status')}</Table.Th>
              <Table.Th>{t('excusalAdmin.credits.actions')}</Table.Th>
            </Table.Tr>
          </Table.Thead>
          <Table.Tbody>
            {data?.items.map(credit => (
              <Table.Tr key={credit.creditId}>
                <Table.Td>
                  <Text size="sm">{credit.participantName}</Text>
                  <Text size="xs" c="dimmed">{credit.participantEmail}</Text>
                </Table.Td>
                <Table.Td>
                  <Group gap="xs">
                    {credit.tags.map(tag => <Badge key={tag} variant="outline" size="xs">{tag}</Badge>)}
                  </Group>
                </Table.Td>
                <Table.Td><Badge color={statusColor(credit.status)}>{credit.status}</Badge></Table.Td>
                <Table.Td>
                  {credit.status === 'Active' && (
                    <Group gap="xs">
                      <Button size="xs" variant="subtle" onClick={() => handleEdit(credit)}>
                        {t('excusalAdmin.credits.editButton')}
                      </Button>
                      <Button
                        size="xs"
                        variant="subtle"
                        color="red"
                        loading={deleteMutation.isPending}
                        onClick={() => {
                          if (confirm(t('excusalAdmin.credits.deleteConfirmBody')))
                            deleteMutation.mutate(credit.creditId);
                        }}
                      >
                        {t('excusalAdmin.credits.deleteButton')}
                      </Button>
                    </Group>
                  )}
                </Table.Td>
              </Table.Tr>
            ))}
          </Table.Tbody>
        </Table>
      )}

      <Modal opened={editModalOpen} onClose={closeEdit} title={t('excusalAdmin.credits.editModal.title')}>
        <Stack>
          <TextInput
            label={t('excusalAdmin.credits.editModal.replaceTags')}
            value={editTags}
            onChange={e => setEditTags(e.target.value)}
          />
          <Button onClick={handleSaveEdit} loading={updateMutation.isPending}>
            {t('excusalAdmin.credits.editModal.saveButton')}
          </Button>
        </Stack>
      </Modal>
    </Container>
  );
}
