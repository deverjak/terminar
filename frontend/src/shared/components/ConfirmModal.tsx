import { Modal, Text, Button, Group } from '@mantine/core';
import { useTranslation } from 'react-i18next';

interface ConfirmModalProps {
  opened: boolean;
  title: string;
  message: string;
  onConfirm: () => void;
  onCancel: () => void;
  loading?: boolean;
  confirmLabel?: string;
  cancelLabel?: string;
  confirmColor?: string;
}

export function ConfirmModal({
  opened,
  title,
  message,
  onConfirm,
  onCancel,
  loading = false,
  confirmLabel,
  cancelLabel,
  confirmColor = 'red',
}: ConfirmModalProps) {
  const { t } = useTranslation();

  return (
    <Modal opened={opened} onClose={onCancel} title={title} centered>
      <Text mb="lg">{message}</Text>
      <Group justify="flex-end">
        <Button variant="default" onClick={onCancel} disabled={loading}>
          {cancelLabel ?? t('common.cancel')}
        </Button>
        <Button color={confirmColor} onClick={onConfirm} loading={loading}>
          {confirmLabel ?? t('common.confirm')}
        </Button>
      </Group>
    </Modal>
  );
}
