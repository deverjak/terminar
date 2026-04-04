import type { ReactNode } from 'react';
import { useActivePlugins } from './useActivePlugins';
import { NotFoundPage } from '@/shared/components/NotFoundPage';

interface PluginGuardProps {
  pluginId: string;
  children: ReactNode;
}

export function PluginGuard({ pluginId, children }: PluginGuardProps) {
  const activePluginIds = useActivePlugins();

  if (!activePluginIds.includes(pluginId)) {
    return <NotFoundPage />;
  }

  return <>{children}</>;
}
