import { useQuery } from '@tanstack/react-query';
import { fetchPlugins } from './pluginsApi';

export function useActivePlugins(): string[] {
  const { data } = useQuery({
    queryKey: ['plugins'],
    queryFn: fetchPlugins,
    staleTime: 5 * 60 * 1000,
  });
  return (data ?? []).filter(p => p.isEnabled).map(p => p.id);
}
