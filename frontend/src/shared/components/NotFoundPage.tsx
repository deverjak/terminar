import { Center, Stack, Title, Text, Button } from '@mantine/core';
import { Link } from 'react-router';
import { useTranslation } from 'react-i18next';
import { useEffect } from 'react';

export function NotFoundPage() {
  const { t } = useTranslation();

  useEffect(() => {
    document.title = `${t('common.notFound')} — Termínář`;
  }, [t]);

  return (
    <Center h="100vh">
      <Stack align="center" gap="md">
        <Title order={1}>404</Title>
        <Title order={2}>{t('common.notFound')}</Title>
        <Text c="dimmed">{t('common.notFoundHint')}</Text>
        <Button component={Link} to="/">{t('common.backHome')}</Button>
      </Stack>
    </Center>
  );
}
