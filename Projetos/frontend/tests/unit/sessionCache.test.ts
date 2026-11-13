import { describe, expect, it } from 'vitest';
import { createQueryClient, clearSessionCache } from '../../src/app/queryClient';

describe('isolamento do cache da sessão', () => {
  it('cancela consultas antigas e remove dados ao trocar contexto', async () => {
    const client = createQueryClient();
    const oldKey = ['finance', 'user-a', 'tenant-a', 'space-a', 'pessoas'];
    client.setQueryData(oldKey, [{ name: 'Pessoa privada' }]);
    let aborted = false;
    const pending = client.fetchQuery({ queryKey: [...oldKey, 'pending'], queryFn: ({ signal }) => new Promise((_resolve, reject) => {
      signal.addEventListener('abort', () => { aborted = true; reject(new Error('aborted')); });
    }) }).catch(() => undefined);
    await clearSessionCache(client);
    await pending;
    expect(aborted).toBe(true);
    expect(client.getQueryCache().getAll()).toHaveLength(0);
    const newKey = ['finance', 'user-b', 'tenant-a', 'space-b', 'pessoas'];
    client.setQueryData(newKey, []);
    expect(client.getQueryData(oldKey)).toBeUndefined();
    expect(client.getQueryData(newKey)).toEqual([]);
    client.clear();
  });
});
