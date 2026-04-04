import { IconCertificate, IconSettings } from '@tabler/icons-react';
import type { PluginContribution } from '@/shared/plugins/types';

export const excusalPluginContribution: PluginContribution = {
  id: 'excusals',
  routes: [
    {
      path: 'excusal-credits',
      lazy: () => import('./ExcusalCreditsPage').then(m => ({ Component: m.default })),
    },
    {
      path: 'settings/excusal',
      lazy: () => import('@/features/settings/excusal/ExcusalSettingsPage').then(m => ({ Component: m.default })),
    },
  ],
  navItems: [
    {
      path: '/app/excusal-credits',
      labelKey: 'nav.excusalCredits',
      icon: IconCertificate,
    },
    {
      path: '/app/settings/excusal',
      labelKey: 'nav.excusalSettings',
      icon: IconSettings,
      requiredRole: 'Admin',
    },
  ],
};
