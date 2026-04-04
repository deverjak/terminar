import { Title, Stack, Card, Group, Text, Badge, Button, Loader, Center } from '@mantine/core';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { notifications } from '@mantine/notifications';
import { fetchPlugins, activatePlugin, deactivatePlugin } from '@/shared/plugins/pluginsApi';
import { useAuth } from '@/features/auth/useAuth';

export function PluginsSettingsPage() {
  const { t } = useTranslation();
  const { session } = useAuth();
  const queryClient = useQueryClient();

  const { data: plugins = [], isLoading } = useQuery({
    queryKey: ['plugins'],
    queryFn: fetchPlugins,
  });

  const activateMutation = useMutation({
    mutationFn: activatePlugin,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['plugins'] });
    },
    onError: () => {
      notifications.show({ color: 'red', message: t('common.error') });
    },
  });

  const deactivateMutation = useMutation({
    mutationFn: deactivatePlugin,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['plugins'] });
    },
    onError: () => {
      notifications.show({ color: 'red', message: t('common.error') });
    },
  });

  if (session?.role !== 'Admin') {
    return null;
  }

  if (isLoading) {
    return (
      <Center h={200}>
        <Loader />
      </Center>
    );
  }

  return (
    <Stack>
      <Title order={2}>{t('plugins.title')}</Title>
      {plugins.map(plugin => (
        <Card key={plugin.id} withBorder padding="md">
          <Group justify="space-between">
            <Stack gap={4}>
              <Group gap="xs">
                <Text fw={600}>{plugin.name}</Text>
                <Badge color={plugin.isEnabled ? 'green' : 'gray'} variant="light">
                  {plugin.isEnabled ? t('plugins.active') : t('plugins.inactive')}
                </Badge>
              </Group>
              <Text size="sm" c="dimmed">{plugin.description}</Text>
            </Stack>
            {plugin.isEnabled ? (
              <Button
                variant="outline"
                color="red"
                size="sm"
                loading={deactivateMutation.isPending && deactivateMutation.variables === plugin.id}
                onClick={() => deactivateMutation.mutate(plugin.id)}
              >
                {t('plugins.deactivate')}
              </Button>
            ) : (
              <Button
                variant="filled"
                size="sm"
                loading={activateMutation.isPending && activateMutation.variables === plugin.id}
                onClick={() => activateMutation.mutate(plugin.id)}
              >
                {t('plugins.activate')}
              </Button>
            )}
          </Group>
        </Card>
      ))}
    </Stack>
  );
}
