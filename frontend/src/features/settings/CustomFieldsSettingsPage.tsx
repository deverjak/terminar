import {
  Title, Stack, Button, Table, Text, Group, Modal, TextInput,
  Select, Textarea, ActionIcon, Badge, Center
} from '@mantine/core';
import { useDisclosure } from '@mantine/hooks';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { notifications } from '@mantine/notifications';
import { useState } from 'react';
import { IconEdit, IconTrash } from '@tabler/icons-react';
import {
  listCustomFields,
  createCustomField,
  updateCustomField,
  deleteCustomField,
  type CustomFieldDefinition,
  type CustomFieldType,
} from './customFieldsApi';
import { ConfirmModal } from '@/shared/components/ConfirmModal';
import { ApiError } from '@/shared/api/client';

export default function CustomFieldsSettingsPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();

  const [addOpen, { open: openAdd, close: closeAdd }] = useDisclosure(false);
  const [editOpen, { open: openEdit, close: closeEdit }] = useDisclosure(false);
  const [deleteTarget, setDeleteTarget] = useState<CustomFieldDefinition | null>(null);
  const [editTarget, setEditTarget] = useState<CustomFieldDefinition | null>(null);

  const [formName, setFormName] = useState('');
  const [formType, setFormType] = useState<CustomFieldType>('YesNo');
  const [formAllowedValues, setFormAllowedValues] = useState('');

  const { data: fields = [], isLoading } = useQuery({
    queryKey: ['custom-fields'],
    queryFn: listCustomFields,
  });

  const createMutation = useMutation({
    mutationFn: () => createCustomField({
      name: formName.trim(),
      fieldType: formType,
      allowedValues: formType === 'OptionsList'
        ? formAllowedValues.split('\n').map(v => v.trim()).filter(Boolean)
        : [],
    }),
    onSuccess: () => {
      notifications.show({ message: t('customFields.createSuccess'), color: 'green' });
      queryClient.invalidateQueries({ queryKey: ['custom-fields'] });
      closeAdd();
      resetForm();
    },
    onError: (err) => {
      const message = err instanceof ApiError ? err.message : t('common.error');
      notifications.show({ title: 'Error', message, color: 'red' });
    },
  });

  const updateMutation = useMutation({
    mutationFn: () => updateCustomField(editTarget!.id, {
      name: formName.trim() || undefined,
      allowedValues: editTarget!.fieldType === 'OptionsList'
        ? formAllowedValues.split('\n').map(v => v.trim()).filter(Boolean)
        : undefined,
    }),
    onSuccess: () => {
      notifications.show({ message: t('customFields.updateSuccess'), color: 'green' });
      queryClient.invalidateQueries({ queryKey: ['custom-fields'] });
      closeEdit();
      setEditTarget(null);
      resetForm();
    },
    onError: (err) => {
      const message = err instanceof ApiError ? err.message : t('common.error');
      notifications.show({ title: 'Error', message, color: 'red' });
    },
  });

  const deleteMutation = useMutation({
    mutationFn: () => deleteCustomField(deleteTarget!.id),
    onSuccess: () => {
      notifications.show({ message: t('customFields.deleteSuccess'), color: 'orange' });
      queryClient.invalidateQueries({ queryKey: ['custom-fields'] });
      setDeleteTarget(null);
    },
    onError: (err) => {
      const message = err instanceof ApiError ? err.message : t('common.error');
      notifications.show({ title: 'Error', message, color: 'red' });
    },
  });

  const resetForm = () => {
    setFormName('');
    setFormType('YesNo');
    setFormAllowedValues('');
  };

  const openEditModal = (field: CustomFieldDefinition) => {
    setEditTarget(field);
    setFormName(field.name);
    setFormType(field.fieldType);
    setFormAllowedValues(field.allowedValues.join('\n'));
    openEdit();
  };

  const fieldTypeLabel = (type: CustomFieldType) => {
    const map: Record<CustomFieldType, string> = {
      YesNo: t('customFields.typeYesNo'),
      Text: t('customFields.typeText'),
      OptionsList: t('customFields.typeOptionsList'),
    };
    return map[type];
  };

  return (
    <Stack gap="md">
      <Group justify="space-between" align="center">
        <Title order={3}>{t('customFields.title')}</Title>
        <Button onClick={() => { resetForm(); openAdd(); }}>{t('customFields.addField')}</Button>
      </Group>

      {isLoading ? null : fields.length === 0 ? (
        <Center py="xl">
          <Stack align="center" gap="xs">
            <Text c="dimmed">{t('customFields.noFields')}</Text>
            <Text size="sm" c="dimmed">{t('customFields.noFieldsHint')}</Text>
          </Stack>
        </Center>
      ) : (
        <Table striped highlightOnHover>
          <Table.Thead>
            <Table.Tr>
              <Table.Th>{t('customFields.columns.name')}</Table.Th>
              <Table.Th>{t('customFields.columns.type')}</Table.Th>
              <Table.Th>{t('customFields.columns.allowedValues')}</Table.Th>
              <Table.Th></Table.Th>
            </Table.Tr>
          </Table.Thead>
          <Table.Tbody>
            {fields.map((field) => (
              <Table.Tr key={field.id}>
                <Table.Td>{field.name}</Table.Td>
                <Table.Td>
                  <Badge variant="light">{fieldTypeLabel(field.fieldType)}</Badge>
                </Table.Td>
                <Table.Td>
                  {field.allowedValues.length > 0
                    ? field.allowedValues.join(', ')
                    : '—'}
                </Table.Td>
                <Table.Td>
                  <Group gap="xs" justify="flex-end">
                    <ActionIcon
                      variant="subtle"
                      onClick={() => openEditModal(field)}
                      aria-label={t('customFields.editField')}
                    >
                      <IconEdit size={16} />
                    </ActionIcon>
                    <ActionIcon
                      variant="subtle"
                      color="red"
                      onClick={() => setDeleteTarget(field)}
                      aria-label={t('customFields.deleteField')}
                    >
                      <IconTrash size={16} />
                    </ActionIcon>
                  </Group>
                </Table.Td>
              </Table.Tr>
            ))}
          </Table.Tbody>
        </Table>
      )}

      {/* Add modal */}
      <Modal opened={addOpen} onClose={closeAdd} title={t('customFields.addField')}>
        <Stack gap="sm">
          <TextInput
            label={t('customFields.fieldName')}
            value={formName}
            onChange={(e) => setFormName(e.currentTarget.value)}
            required
          />
          <Select
            label={t('customFields.fieldType')}
            value={formType}
            onChange={(v) => setFormType((v as CustomFieldType) ?? 'YesNo')}
            data={[
              { value: 'YesNo', label: t('customFields.typeYesNo') },
              { value: 'Text', label: t('customFields.typeText') },
              { value: 'OptionsList', label: t('customFields.typeOptionsList') },
            ]}
          />
          {formType === 'OptionsList' && (
            <Textarea
              label={t('customFields.allowedValues')}
              value={formAllowedValues}
              onChange={(e) => setFormAllowedValues(e.currentTarget.value)}
              autosize
              minRows={3}
            />
          )}
          <Button
            onClick={() => createMutation.mutate()}
            loading={createMutation.isPending}
            disabled={!formName.trim()}
          >
            {t('common.save')}
          </Button>
        </Stack>
      </Modal>

      {/* Edit modal */}
      <Modal opened={editOpen} onClose={closeEdit} title={t('customFields.editField')}>
        <Stack gap="sm">
          <TextInput
            label={t('customFields.fieldName')}
            value={formName}
            onChange={(e) => setFormName(e.currentTarget.value)}
            required
          />
          {editTarget?.fieldType === 'OptionsList' && (
            <Textarea
              label={t('customFields.allowedValues')}
              value={formAllowedValues}
              onChange={(e) => setFormAllowedValues(e.currentTarget.value)}
              autosize
              minRows={3}
            />
          )}
          <Button
            onClick={() => updateMutation.mutate()}
            loading={updateMutation.isPending}
            disabled={!formName.trim()}
          >
            {t('common.save')}
          </Button>
        </Stack>
      </Modal>

      <ConfirmModal
        opened={!!deleteTarget}
        title={t('customFields.deleteField')}
        message={t('customFields.deleteConfirm')}
        onConfirm={() => deleteMutation.mutate()}
        onCancel={() => setDeleteTarget(null)}
        loading={deleteMutation.isPending}
        confirmColor="red"
      />
    </Stack>
  );
}
