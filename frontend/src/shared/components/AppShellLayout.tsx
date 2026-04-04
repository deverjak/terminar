import { AppShell, Burger, Group, Text, NavLink, Button, ActionIcon, Stack } from '@mantine/core';
import { useDisclosure } from '@mantine/hooks';
import { useMantineColorScheme } from '@mantine/core';
import { Outlet, Link, useNavigate } from 'react-router';
import { useTranslation } from 'react-i18next';
import { IconSun, IconMoon, IconBook, IconUsers, IconPuzzle } from '@tabler/icons-react';
import { useAuth } from '@/features/auth/useAuth';
import { useActivePlugins } from '@/shared/plugins/useActivePlugins';
import { getContributionsForActivePlugins } from '@/shared/plugins/pluginRegistry';

export function AppShellLayout() {
  const [opened, { toggle }] = useDisclosure();
  const { colorScheme, toggleColorScheme } = useMantineColorScheme();
  const { t, i18n } = useTranslation();
  const { session, logout } = useAuth();
  const navigate = useNavigate();
  const activePluginIds = useActivePlugins();
  const pluginContributions = getContributionsForActivePlugins(activePluginIds);
  const pluginNavItems = pluginContributions.flatMap(p => p.navItems);

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  const switchLanguage = (lang: string) => {
    i18n.changeLanguage(lang);
  };

  return (
    <AppShell
      header={{ height: 60 }}
      navbar={{ width: 220, breakpoint: 'sm', collapsed: { mobile: !opened } }}
      padding="md"
    >
      <AppShell.Header>
        <Group h="100%" px="md" justify="space-between">
          <Group>
            <Burger opened={opened} onClick={toggle} hiddenFrom="sm" size="sm" />
            <Text fw={700} size="lg" component={Link} to="/app/courses" style={{ textDecoration: 'none', color: 'inherit' }}>
              Termínář
            </Text>
          </Group>
          <Group gap="xs">
            <Button variant="subtle" size="xs" onClick={() => switchLanguage('en')}>EN</Button>
            <Button variant="subtle" size="xs" onClick={() => switchLanguage('cs')}>CS</Button>
            <ActionIcon variant="subtle" onClick={() => toggleColorScheme()} title="Toggle color scheme">
              {colorScheme === 'dark' ? <IconSun size={18} /> : <IconMoon size={18} />}
            </ActionIcon>
            <Button variant="subtle" size="sm" onClick={handleLogout}>
              {t('nav.logout')}
            </Button>
          </Group>
        </Group>
      </AppShell.Header>

      <AppShell.Navbar p="md">
        <Stack gap="xs">
          <NavLink
            component={Link}
            to="/app/courses"
            label={t('nav.courses')}
            leftSection={<IconBook size={16} />}
          />
          {session?.role === 'Admin' && (
            <NavLink
              component={Link}
              to="/app/staff"
              label={t('nav.staff')}
              leftSection={<IconUsers size={16} />}
            />
          )}
          {pluginNavItems
            .filter(item => !item.requiredRole || session?.role === item.requiredRole)
            .map(item => (
              <NavLink
                key={item.path}
                component={Link}
                to={item.path}
                label={t(item.labelKey)}
                leftSection={<item.icon size={16} />}
              />
            ))}
          {session?.role === 'Admin' && (
            <NavLink
              component={Link}
              to="/app/settings/custom-fields"
              label={t('nav.customFieldsSettings')}
              leftSection={<IconPuzzle size={16} />}
            />
          )}
          {session?.role === 'Admin' && (
            <NavLink
              component={Link}
              to="/app/settings/plugins"
              label={t('nav.pluginsSettings')}
              leftSection={<IconPuzzle size={16} />}
            />
          )}
        </Stack>
      </AppShell.Navbar>

      <AppShell.Main>
        <Outlet />
      </AppShell.Main>
    </AppShell>
  );
}
