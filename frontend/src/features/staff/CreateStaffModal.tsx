import { Modal, TextInput, PasswordInput, Select, Button, Stack } from '@mantine/core';
import { useForm } from '@mantine/form';
import { useTranslation } from 'react-i18next';
import { useState } from 'react';
import { notifications } from '@mantine/notifications';
import { createStaff } from './staffApi';
import { ApiError } from '@/shared/api/client';

interface CreateStaffModalProps {
  opened: boolean;
  onClose: () => void;
  onSuccess: () => void;
}

interface FormValues {
  username: string;
  email: string;
  password: string;
  role: 'Admin' | 'Staff';
}

export function CreateStaffModal({ opened, onClose, onSuccess }: CreateStaffModalProps) {
  const { t } = useTranslation();
  const [submitting, setSubmitting] = useState(false);

  const form = useForm<FormValues>({
    initialValues: {
      username: '',
      email: '',
      password: '',
      role: 'Staff',
    },
    validate: {
      username: (v) => (v.trim().length === 0 ? 'Required' : null),
      email: (v) => (/^\S+@\S+\.\S+$/.test(v) ? null : 'Invalid email'),
      password: (v) => (v.length >= 8 ? null : 'Minimum 8 characters'),
    },
  });

  const handleClose = () => {
    form.reset();
    onClose();
  };

  const handleSubmit = async (values: FormValues) => {
    setSubmitting(true);
    try {
      await createStaff(values);
      notifications.show({
        title: t('staff.title'),
        message: t('staff.createSuccess'),
        color: 'green',
      });
      form.reset();
      onSuccess();
      onClose();
    } catch (err) {
      if (err instanceof ApiError) {
        if (err.fieldErrors) {
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
    <Modal opened={opened} onClose={handleClose} title={t('staff.addStaff')} centered>
      <form onSubmit={form.onSubmit(handleSubmit)}>
        <Stack gap="md">
          <TextInput
            label={t('staff.form.username')}
            required
            {...form.getInputProps('username')}
          />
          <TextInput
            label={t('staff.form.email')}
            type="email"
            required
            {...form.getInputProps('email')}
          />
          <PasswordInput
            label={t('staff.form.password')}
            required
            {...form.getInputProps('password')}
          />
          <Select
            label={t('staff.form.role')}
            data={[
              { value: 'Staff', label: t('staff.roles.Staff') },
              { value: 'Admin', label: t('staff.roles.Admin') },
            ]}
            {...form.getInputProps('role')}
          />
          <Button type="submit" loading={submitting}>
            {t('staff.form.submitButton')}
          </Button>
        </Stack>
      </form>
    </Modal>
  );
}
