import { Container, Title, TextInput, PasswordInput, Select, Button, Stack, Anchor, Text, Paper } from '@mantine/core';
import { useForm } from '@mantine/form';
import { useNavigate, Link } from 'react-router';
import { useTranslation } from 'react-i18next';
import { useState, useEffect } from 'react';
import { createTenant } from './tenantsApi';
import { login as authLogin, decodeJwt } from '@/features/auth/authApi';
import { useAuth } from '@/features/auth/useAuth';
import { ApiError } from '@/shared/api/client';
import { notifications } from '@mantine/notifications';

interface RegisterFormValues {
  name: string;
  slug: string;
  defaultLanguageCode: string;
  adminUsername: string;
  adminEmail: string;
  adminPassword: string;
  confirmPassword: string;
}

export function TenantRegisterPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { login } = useAuth();
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    document.title = `${t('tenants.register')} — Termínář`;
  }, [t]);

  const form = useForm<RegisterFormValues>({
    initialValues: {
      name: '',
      slug: '',
      defaultLanguageCode: 'en',
      adminUsername: '',
      adminEmail: '',
      adminPassword: '',
      confirmPassword: '',
    },
    validate: {
      name: (v) => (v.trim().length === 0 ? 'Required' : null),
      slug: (v) =>
        /^[a-z0-9-]{3,63}$/.test(v) ? null : t('tenants.slugHint'),
      adminUsername: (v) => (v.trim().length === 0 ? 'Required' : null),
      adminEmail: (v) => (/^\S+@\S+\.\S+$/.test(v) ? null : 'Invalid email'),
      adminPassword: (v) => (v.length >= 8 ? null : 'Minimum 8 characters'),
      confirmPassword: (v, values) =>
        v === values.adminPassword ? null : t('tenants.passwordMismatch'),
    },
  });

  const handleSubmit = async (values: RegisterFormValues) => {
    setSubmitting(true);
    try {
      await createTenant({
        name: values.name,
        slug: values.slug,
        defaultLanguageCode: values.defaultLanguageCode,
        adminUsername: values.adminUsername,
        adminEmail: values.adminEmail,
        adminPassword: values.adminPassword,
      });

      const tokens = await authLogin(values.adminEmail, values.adminPassword);
      const claims = decodeJwt(tokens.accessToken);

      login({
        accessToken: tokens.accessToken,
        refreshToken: tokens.refreshToken,
        userId: claims.sub,
        username: claims.username,
        role: claims.role as 'Admin' | 'Staff',
        tenantSlug: values.slug,
        tenantId: claims.tenantId,
      });

      navigate('/app/courses');
    } catch (err) {
      if (err instanceof ApiError) {
        if (err.status === 422 && err.fieldErrors?.slug) {
          form.setFieldError('slug', t('tenants.slugTaken'));
        } else if (err.fieldErrors) {
          Object.entries(err.fieldErrors).forEach(([field, msgs]) => {
            const fieldName = field.charAt(0).toLowerCase() + field.slice(1) as keyof RegisterFormValues;
            form.setFieldError(fieldName, msgs[0]);
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
    <Container size="sm" py="xl">
      <Paper shadow="sm" p="xl" radius="md">
        <Title order={2} mb="lg">{t('tenants.register')}</Title>
        <form onSubmit={form.onSubmit(handleSubmit)}>
          <Stack gap="md">
            <TextInput
              label={t('tenants.orgName')}
              placeholder={t('tenants.orgNamePlaceholder')}
              required
              {...form.getInputProps('name')}
            />
            <TextInput
              label={t('tenants.slug')}
              description={t('tenants.slugHint')}
              required
              {...form.getInputProps('slug')}
            />
            <Select
              label={t('tenants.language')}
              data={[
                { value: 'en', label: t('tenants.langEn') },
                { value: 'cs', label: t('tenants.langCs') },
              ]}
              {...form.getInputProps('defaultLanguageCode')}
            />
            <TextInput
              label={t('tenants.adminUsername')}
              required
              {...form.getInputProps('adminUsername')}
            />
            <TextInput
              label={t('tenants.adminEmail')}
              type="email"
              required
              {...form.getInputProps('adminEmail')}
            />
            <PasswordInput
              label={t('tenants.adminPassword')}
              required
              {...form.getInputProps('adminPassword')}
            />
            <PasswordInput
              label={t('tenants.confirmPassword')}
              required
              {...form.getInputProps('confirmPassword')}
            />
            <Button type="submit" loading={submitting}>
              {submitting ? t('tenants.creating') : t('tenants.createButton')}
            </Button>
          </Stack>
        </form>
        <Text mt="md" ta="center" size="sm">
          <Anchor component={Link} to="/login">{t('auth.login')}</Anchor>
        </Text>
      </Paper>
    </Container>
  );
}
