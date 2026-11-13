import { afterEach, describe, expect, it, vi } from 'vitest';
import { onUnauthorized, request, resetCsrf } from '../../src/shared/api/client';

afterEach(() => { resetCsrf(); vi.unstubAllGlobals(); });
describe('fronteira HTTP', () => {
  it('envia cookie de mesma origem e token CSRF sem converter texto monetário', async () => {
    const fetch = vi.fn().mockResolvedValueOnce(new Response(JSON.stringify({ token: 'csrf-test' }), { status: 200 })).mockResolvedValueOnce(new Response(JSON.stringify({ id: 'created' }), { status: 201 }));
    vi.stubGlobal('fetch', fetch);
    await request('/api/v1/contas', { method: 'POST', body: { overdraftLimit: '99999999999999999.99', number: '00000123' } });
    const options = fetch.mock.calls[1][1];
    expect(options.credentials).toBe('same-origin');
    expect(options.headers['X-CSRF-TOKEN']).toBe('csrf-test');
    expect(JSON.parse(options.body)).toEqual({ overdraftLimit: '99999999999999999.99', number: '00000123' });
  });
  it('notifica expiração para limpar o contexto e não aceita URLs externas', async () => {
    const handler = vi.fn(); const unsubscribe = onUnauthorized(handler);
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('{}', { status: 401 })));
    await expect(request('/api/v1/pessoas')).rejects.toMatchObject({ status: 401 });
    expect(handler).toHaveBeenCalledOnce();
    await expect(request('https://example.com/private')).rejects.toThrow('Endereço');
    unsubscribe();
  });
});
