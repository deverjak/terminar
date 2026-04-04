# Frontend Plugin Contract

**Feature**: 007-plugin-architecture  
**Date**: 2026-04-04

---

## Overview

Each plugin contributes routes and nav items to the application via a static plugin registry. The active plugin list is fetched once at app load from the backend and cached by TanStack Query. All plugin UI rendering is gated on this list.

---

## Plugin Contribution Type

```typescript
// frontend/src/shared/plugins/types.ts

import type { RouteObject } from 'react-router';
import type { ComponentType } from 'react';

export interface NavItem {
  path: string;
  labelKey: string;            // i18n key (e.g. 'nav.excusalCredits')
  icon: ComponentType<{ size?: number }>;
  requiredRole?: 'Admin' | 'Staff';
}

export interface PluginContribution {
  id: string;
  routes: RouteObject[];
  navItems: NavItem[];
}
```

---

## Plugin Registry

```typescript
// frontend/src/shared/plugins/pluginRegistry.ts

import { excusalPluginContribution } from '@/features/excusal-credits/plugin';
import { paymentsPluginContribution } from '@/features/payments/plugin';
import type { PluginContribution } from './types';

const PLUGIN_REGISTRY: Record<string, PluginContribution> = {
  excusals: excusalPluginContribution,
  payments: paymentsPluginContribution,
};

export function getPluginContributions(activePluginIds: string[]): PluginContribution[] {
  return activePluginIds
    .map(id => PLUGIN_REGISTRY[id])
    .filter(Boolean);
}
```

---

## Plugin Contribution File Convention

Each plugin feature folder exports a `plugin.ts` file:

```typescript
// frontend/src/features/excusal-credits/plugin.ts

import { IconCertificate, IconSettings } from '@tabler/icons-react';
import type { PluginContribution } from '@/shared/plugins/types';

export const excusalPluginContribution: PluginContribution = {
  id: 'excusals',
  routes: [
    {
      path: 'excusal-credits',
      lazy: () => import('./ExcusalCreditsPage').then(m => ({ Component: m.ExcusalCreditsPage })),
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
```

---

## useActivePlugins Hook

```typescript
// frontend/src/shared/plugins/useActivePlugins.ts

import { useQuery } from '@tanstack/react-query';
import { fetchPlugins } from './pluginsApi';

export function useActivePlugins(): string[] {
  const { data } = useQuery({
    queryKey: ['plugins'],
    queryFn: fetchPlugins,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
  return (data ?? []).filter(p => p.isEnabled).map(p => p.id);
}
```

---

## Router Integration

The router dynamically adds plugin routes under `/app`:

```typescript
// frontend/src/app/router.tsx (updated)

import { getPluginContributions } from '@/shared/plugins/pluginRegistry';

// Called once at router creation with active plugin IDs from the initial fetch
// or restructured to use a RouterProvider with dynamic route injection

function buildRouter(activePluginIds: string[]) {
  const pluginRoutes = getPluginContributions(activePluginIds).flatMap(p => p.routes);
  return createBrowserRouter([
    // ... static routes unchanged ...
    {
      path: '/app',
      element: <AuthGuard><AppShellLayout /></AuthGuard>,
      children: [
        { index: true, element: <Navigate to="/app/courses" replace /> },
        // static routes
        { path: 'courses', element: <CourseListPage /> },
        // ... 
        // plugin routes appended
        ...pluginRoutes,
      ],
    },
    // ...
  ]);
}
```

**Alternative (simpler)**: Use React Router's `<Routes>` within the layout with conditional `<Route>` rendering based on `useActivePlugins()` — avoids rebuilding the router on plugin state change but requires Routes inside Outlet.

The preferred approach is to refetch plugins after tenant login and recreate the router via `RouterProvider` with a new router instance.

---

## AppShellLayout Nav Integration

```typescript
// Simplified AppShellLayout nav section (updated)

const activePluginIds = useActivePlugins();
const contributions = getPluginContributions(activePluginIds);
const navItems = contributions.flatMap(p => p.navItems);

// Render dynamic plugin nav items after static ones:
{navItems
  .filter(item => !item.requiredRole || session?.role === item.requiredRole)
  .map(item => (
    <NavLink
      key={item.path}
      component={Link}
      to={item.path}
      label={t(item.labelKey)}
      leftSection={<item.icon size={16} />}
    />
  ))
}
```

---

## Plugins API Client

```typescript
// frontend/src/shared/plugins/pluginsApi.ts

import { apiClient } from '@/shared/api/client';
import type { PluginStatus } from './types';

export async function fetchPlugins(): Promise<PluginStatus[]> {
  const res = await apiClient.get('/api/v1/settings/plugins');
  return res.data;
}

export async function activatePlugin(pluginId: string): Promise<void> {
  await apiClient.post(`/api/v1/settings/plugins/${pluginId}/activate`);
}

export async function deactivatePlugin(pluginId: string): Promise<void> {
  await apiClient.delete(`/api/v1/settings/plugins/${pluginId}/activate`);
}
```
