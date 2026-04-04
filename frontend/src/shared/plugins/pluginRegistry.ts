import { excusalPluginContribution } from '@/features/excusal-credits/plugin';
import { paymentsPluginContribution } from '@/features/payments/plugin';
import type { PluginContribution } from './types';

const PLUGIN_REGISTRY: Record<string, PluginContribution> = {
  excusals: excusalPluginContribution,
  payments: paymentsPluginContribution,
};

export function getContributionsForActivePlugins(activeIds: string[]): PluginContribution[] {
  return activeIds
    .map(id => PLUGIN_REGISTRY[id])
    .filter(Boolean) as PluginContribution[];
}
