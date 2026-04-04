import type { RouteObject } from 'react-router';
import type { ComponentType } from 'react';

export interface PluginStatus {
  id: string;
  name: string;
  description: string;
  isEnabled: boolean;
}

export interface NavItem {
  path: string;
  labelKey: string;
  icon: ComponentType<{ size?: number }>;
  requiredRole?: 'Admin' | 'Staff';
}

export interface PluginContribution {
  id: string;
  routes: RouteObject[];
  navItems: NavItem[];
}
