import { QueryClient } from '@tanstack/react-query';
import { ApiError } from '../shared/api/client';

export function createQueryClient() {
  return new QueryClient({ defaultOptions: { queries: { staleTime: 30_000, retry: (count, error) => !(error instanceof ApiError && error.status < 500) && count < 1, refetchOnWindowFocus: false }, mutations: { retry: false } } });
}
export async function clearSessionCache(client: QueryClient) {
  await client.cancelQueries();
  client.clear();
}
