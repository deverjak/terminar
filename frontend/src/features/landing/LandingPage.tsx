import { Center, Stack, Title, Text, Button, Group } from '@mantine/core';
import { Link, Navigate } from 'react-router';
import { useTranslation } from 'react-i18next';
import { useEffect } from 'react';
import { useAuth } from '@/features/auth/useAuth';

export function LandingPage() {
  const { t } = useTranslation();
  const { session } = useAuth();

  useEffect(() => {
    document.title = `${t('landing.title')} — Termínář`;
  }, [t]);

  if (session) {
    return <Navigate to="/app/courses" replace />;
  }

  return (
    <Center h="100vh">
      <Stack align="center" gap="xl">
        <Title order={1} size="3rem">
          {t('landing.title')}
        </Title>
        <Text size="xl" c="dimmed">
          {t('landing.tagline')}
        </Text>
        <Group>
          <Button component={Link} to="/register" size="lg">
            {t('landing.createWorkspace')}
          </Button>
          <Button component={Link} to="/login" variant="outline" size="lg">
            {t('landing.login')}
          </Button>
        </Group>
      </Stack>
    </Center>
  );
}
