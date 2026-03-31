import { Container, Title, TextInput, PasswordInput, Button, Stack, Anchor, Text, Paper } from '@mantine/core';
import { useForm } from '@mantine/form';
import { useNavigate, Link, Navigate } from 'react-router';
import { useTranslation } from 'react-i18next';
import { useState, useEffect } from 'react';
import { notifications } from '@mantine/notifications';
import { login as authLogin, decodeJwt } from './authApi';
import { useAuth } from './useAuth';
import { ApiError } from '@/shared/api/client';

interface LoginFormValues {
  email: string;
  password: string;
}

export function LoginPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { login, session } = useAuth();
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    document.title = `${t('auth.login')} — Termínář`;
  }, [t]);

  const form = useForm<LoginFormValues>({
    initialValues: { email: '', password: '' },
    validate: {
      email: (v) => (/^\S+@\S+\.\S+$/.test(v) ? null : t('common.invalidEmail')),
      password: (v) => (v.trim().length === 0 ? t('common.required') : null),
    },
  });

  if (session) {
    return <Navigate to="/app/courses" replace />;
  }

  const handleSubmit = async (values: LoginFormValues) => {
    setSubmitting(true);
    try {
      const tokens = await authLogin(values.email, values.password);
      const claims = decodeJwt(tokens.accessToken);

      login({
        accessToken: tokens.accessToken,
        refreshToken: tokens.refreshToken,
        userId: claims.sub,
        username: claims.username,
        role: claims.role as 'Admin' | 'Staff',
        tenantSlug: tokens.tenantSlug,
        tenantId: claims.tenantId,
      });

      navigate('/app/courses');
    } catch (err) {
      if (err instanceof ApiError) {
        if (err.status === 401 || err.status === 403) {
          form.setFieldError('password', t('auth.invalidCredentials'));
        } else {
          notifications.show({ title: t('common.error'), message: err.message, color: 'red' });
        }
      } else {
        notifications.show({ title: t('common.error'), message: t('common.error'), color: 'red' });
      }
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Container size="xs" py="xl">
      <Paper shadow="sm" p="xl" radius="md">
        <Title order={2} mb="lg">{t('auth.login')}</Title>
        <form onSubmit={form.onSubmit(handleSubmit)}>
          <Stack gap="md">
            <TextInput
              label={t('auth.email')}
              placeholder="you@example.com"
              type="email"
              required
              {...form.getInputProps('email')}
            />
            <PasswordInput
              label={t('auth.password')}
              required
              {...form.getInputProps('password')}
            />
            <Button type="submit" loading={submitting}>
              {submitting ? t('auth.loggingIn') : t('auth.loginButton')}
            </Button>
          </Stack>
        </form>
        <Text mt="md" ta="center" size="sm">
          <Anchor component={Link} to="/register">{t('tenants.register')}</Anchor>
        </Text>
      </Paper>
    </Container>
  );
}
