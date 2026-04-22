/**
 * Shared TanStack Query test harness.
 *
 * Hook tests that wrap `useQuery` / `useMutation` need a `QueryClientProvider`
 * to function. Creating the client inline in each test duplicates the same
 * defaults (disable retries, drop cache between tests) and invites drift.
 *
 * `createTestQueryClient` returns a fresh client configured for deterministic
 * tests:
 *   - `retry: false` so an API rejection surfaces immediately as `isError`.
 *   - `staleTime: 0` / `gcTime: 0` so every test starts with a cold cache.
 *
 * `renderHookWithQuery` wraps `renderHook` with a provider that uses this
 * client. Use it for every hook test that relies on a QueryClient.
 */

import type { ReactNode } from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { renderHook, type RenderHookOptions } from '@testing-library/react';

export function createTestQueryClient(): QueryClient {
  return new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
        staleTime: 0,
        gcTime: 0,
        // Vitest's happy-dom environment is always "visible"; disable
        // refetch-on-mount timing effects for deterministic runs.
        refetchOnWindowFocus: false,
      },
      mutations: {
        retry: false,
      },
    },
  });
}

type HarnessOptions<TProps> = RenderHookOptions<TProps> & {
  /** Supply a pre-built client — useful when a test needs to seed cache. */
  client?: QueryClient;
};

export function renderHookWithQuery<TResult, TProps>(
  hook: (props: TProps) => TResult,
  options?: HarnessOptions<TProps>
) {
  const client = options?.client ?? createTestQueryClient();
  const wrapper = ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={client}>{children}</QueryClientProvider>
  );
  const result = renderHook(hook, { wrapper, ...options });
  return { client, ...result };
}
