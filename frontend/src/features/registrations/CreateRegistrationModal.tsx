import { Modal, TextInput, Button, Stack, Alert } from '@mantine/core';
import { useForm } from '@mantine/form';
import { useTranslation } from 'react-i18next';
import { useState } from 'react';
import { notifications } from '@mantine/notifications';
import { createRegistration } from './registrationsApi';
import { ApiError } from '@/shared/api/client';

interface CreateRegistrationModalProps {
  opened: boolean;
  onClose: () => void;
  onSuccess: () => void;
  courseId: string;
}

interface FormValues {
  participantName: string;
  participantEmail: string;
}

export function CreateRegistrationModal({
  opened,
  onClose,
  onSuccess,
  courseId,
}: CreateRegistrationModalProps) {
  const { t } = useTranslation();
  const [submitting, setSubmitting] = useState(false);
  const [capacityError, setCapacityError] = useState<string | null>(null);

  const form = useForm<FormValues>({
    initialValues: {
      participantName: '',
      participantEmail: '',
    },
    validate: {
      participantName: (v) => (v.trim().length === 0 ? 'Required' : null),
      participantEmail: (v) => (/^\S+@\S+\.\S+$/.test(v) ? null : 'Invalid email'),
    },
  });

  const handleClose = () => {
    form.reset();
    setCapacityError(null);
    onClose();
  };

  const handleSubmit = async (values: FormValues) => {
    setSubmitting(true);
    setCapacityError(null);
    try {
      await createRegistration(courseId, values);
      notifications.show({
        title: t('registrations.title'),
        message: t('registrations.addSuccess'),
        color: 'green',
      });
      form.reset();
      onSuccess();
      onClose();
    } catch (err) {
      if (err instanceof ApiError) {
        if (err.status === 409) {
          setCapacityError(t('registrations.form.fullCapacity'));
        } else if (err.fieldErrors) {
          Object.entries(err.fieldErrors).forEach(([field, msgs]) => {
            form.setFieldError(field as keyof FormValues, msgs[0]);
          });
        } else {
          notifications.show({ title: 'Error', message: err.message, color: 'red' });
        }
      } else {
        notifications.show({ title: 'Error', message: t('common.error'), color: 'red' });
      }
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Modal opened={opened} onClose={handleClose} title={t('registrations.addRegistration')} centered>
      <form onSubmit={form.onSubmit(handleSubmit)}>
        <Stack gap="md">
          {capacityError && (
            <Alert color="red">{capacityError}</Alert>
          )}
          <TextInput
            label={t('registrations.form.participantName')}
            required
            {...form.getInputProps('participantName')}
          />
          <TextInput
            label={t('registrations.form.participantEmail')}
            type="email"
            required
            {...form.getInputProps('participantEmail')}
          />
          <Button type="submit" loading={submitting}>
            {t('registrations.form.submitButton')}
          </Button>
        </Stack>
      </form>
    </Modal>
  );
}
