import { test, expect, type Page } from '@playwright/test';

const user = '11111111-1111-4111-8111-111111111111';
const tenant = '22222222-2222-4222-8222-222222222222';
const first = '33333333-3333-4333-8333-333333333333';
const second = '44444444-4444-4444-8444-444444444444';
const person = '66666666-6666-4666-8666-666666666666';
type Row = Record<string, unknown>;
async function backend(page: Page) {
  const state = { authenticated: true, scope: first, writes: [] as Array<{ path: string } & Record<string, unknown>> };
  const data: Record<string, Row[]> = {
    [first]: [], [second]: [],
  };
  await page.route('**/api/v1/**', async route => {
    const req = route.request(); const url = new URL(req.url()); const path = url.pathname.replace('/api/v1/', ''); const method = req.method();
    const body = req.postData() ? JSON.parse(req.postData()!) : {};
    const json = (payload: unknown, status = 200) => route.fulfill({ status, json: payload });
    if (path === 'auth/csrf') return json({ token: 'csrf-browser-test' });
    if (path === 'auth/me') return state.authenticated ? json({ id: user, displayName: 'Ana', email: 'test@example.test', tenantId: tenant, spaceId: state.scope }) : json({}, 401);
    if (method !== 'GET' && req.headers()['x-csrf-token'] !== 'csrf-browser-test') return json({ title: 'CSRF invalid' }, 400);
    if (!state.authenticated) return json({}, 401);
    if (path === 'context/spaces') return json([{ tenantId: tenant, spaceId: first, name: 'Família', currency: 'BRL' }, { tenantId: tenant, spaceId: second, name: 'Viagem', currency: 'USD' }]);
    if (path === 'context/permissions') return json(['cadastros.visualizar', 'cadastros.editar', 'dashboard.visualizar']);
    if (path === 'context/select') { state.scope = body.spaceId; return route.fulfill({ status: 204 }); }
    if (path === 'pessoas') return json({ items: state.scope === first ? [{ id: person, name: 'Pessoa da Família', active: true, version: 1 }] : [], total: 1, page: 1, pageSize: 100 });
    if (path === 'categorias') return json({ items: [], total: 0, page: 1, pageSize: 100 });
    if (path === 'cartoes') return json({ items: [], total: 0, page: 1, pageSize: 100 });
    const [kind, id, action] = path.split('/');
    if (kind !== 'obrigacoes' && kind !== 'titulos') return json({}, 404);
    if (action === 'planejamentos') {
      const key = `${state.scope}:${kind}:${id}:planejamentos`;
      if (method === 'GET') return json((data[key] as Row[] | undefined) ?? []);
      data[key] ??= [];
      const entry = { id: crypto.randomUUID(), titleId: id, personId: body.personId, amount: body.amount, version: 1 };
      (data[key] as Row[]).push(entry); state.writes.push({ path, ...body }); return json(entry);
    }
    const key = `${state.scope}:${kind}`;
    data[key] ??= [];
    const rows = data[key];
    if (method === 'GET') {
      if (id) { const row = rows.find(item => item.id === id); return row ? json(row) : json({}, 404); }
      const obligationId = url.searchParams.get('obligationId');
      const matching = rows.filter(row => !obligationId || row.obligationId === obligationId);
      return json({ items: matching, total: matching.length, page: Number(url.searchParams.get('page') || 1), pageSize: 20 });
    }
    state.writes.push({ path, ...body });
    if (method === 'POST') {
      const row: Row = {
        ...body, id: crypto.randomUUID(), version: 1,
        ...(kind === 'titulos' ? { balance: body.originalAmount ?? '0.00', settledAmount: '0.00', excessCredit: '0.00', settlementStatus: 'Open', cancelledAt: null } : {}),
      };
      rows.push(row); return json(row, 201);
    }
    const index = rows.findIndex(row => row.id === id); if (index < 0) return json({}, 404);
    if (method === 'PUT') { rows[index] = { ...rows[index], ...body, version: Number(rows[index].version) + 1 }; return json(rows[index]); }
    return json({}, 405);
  });
  return state;
}

test('cria e edita obrigações e títulos com valores e datas validados', async ({ page }, info) => {
  const state = await backend(page);
  await page.goto('/obrigacoes');
  await page.getByRole('button', { name: 'Adicionar obrigação', exact: true }).first().click();
  const dialog = page.getByRole('dialog');
  await dialog.getByLabel('Descrição', { exact: false }).fill('Seguro do carro');
  await dialog.getByLabel('Tipo', { exact: true }).selectOption('Installment');
  await dialog.getByLabel('Quantidade de parcelas', { exact: true }).fill('2');
  await dialog.getByRole('button', { name: 'Salvar', exact: true }).click();
  await expect(page.getByRole('dialog')).toHaveCount(0);
  await expect(page.getByText('Seguro do carro', { exact: true })).toBeVisible();
  expect(state.writes.find(row => row.path === 'obrigacoes')).toMatchObject({ description: 'Seguro do carro', kind: 'Installment', installmentCount: 2, purchaseDate: null, dueDay: null });

  await page.getByRole('link', { name: 'Títulos', exact: true }).click();
  await page.getByRole('button', { name: 'Adicionar título', exact: true }).first().click();
  await dialog.getByLabel('Chave de ocorrência', { exact: false }).fill('seguro-2026-12');
  await dialog.getByLabel('Período', { exact: false }).fill('2026-12-01');
  await dialog.getByLabel('Valor original', { exact: false }).fill('1.234,56');
  await dialog.getByLabel('Número da parcela', { exact: true }).fill('1');
  await dialog.getByRole('button', { name: 'Salvar', exact: true }).click();
  await expect(page.getByText(/Informe um valor/)).toBeVisible();
  await expect(page.getByRole('dialog')).toBeVisible();
  await dialog.getByLabel('Valor original', { exact: false }).fill('1234,56');
  await dialog.getByRole('button', { name: 'Salvar', exact: true }).click();
  await expect(page.getByRole('dialog')).toHaveCount(0);
  await expect(page.getByText('seguro-2026-12', { exact: true })).toBeVisible();
  expect(state.writes.find(row => row.path === 'titulos')).toMatchObject({ occurrenceKey: 'seguro-2026-12', period: '2026-12-01', dueDate: null, originalAmount: '1234.56', installmentNumber: 1, state: 'Draft' });

  await page.getByRole('button', { name: 'Editar título seguro-2026-12', exact: true }).click();
  await dialog.getByLabel('Estado', { exact: true }).selectOption('Effective');
  await dialog.getByRole('button', { name: 'Salvar', exact: true }).click();
  await expect(page.getByRole('dialog')).toHaveCount(0);
  expect(state.writes.filter(row => row.path.startsWith('titulos/')).at(-1)).toMatchObject({ state: 'Effective' });

  await page.getByRole('button', { name: 'Planejar título seguro-2026-12', exact: true }).click();
  await dialog.getByLabel('Responsável', { exact: true }).selectOption(person);
  await dialog.getByLabel('Valor planejado', { exact: false }).fill('600,00');
  await dialog.getByRole('button', { name: 'Salvar', exact: true }).click();
  await expect(page.getByRole('dialog')).toHaveCount(0);
  expect(state.writes.find(row => row.path.endsWith('/planejamentos'))).toMatchObject({ personId: person, amount: '600.00' });
  await page.screenshot({ path: info.outputPath('titles-desktop.png'), fullPage: true });
});

test('obrigações e títulos não vazam entre espaços financeiros', async ({ page }) => {
  await backend(page);
  await page.goto('/obrigacoes');
  await page.getByRole('button', { name: 'Adicionar obrigação', exact: true }).first().click();
  const dialog = page.getByRole('dialog');
  await dialog.getByLabel('Descrição', { exact: false }).fill('Compromisso da Família');
  await dialog.getByRole('button', { name: 'Salvar', exact: true }).click();
  await expect(page.getByText('Compromisso da Família', { exact: true })).toBeVisible();
  await page.getByLabel('Espaço financeiro', { exact: true }).selectOption(`${tenant}/${second}`);
  await expect(page.getByText('Compromisso da Família', { exact: true })).toHaveCount(0);
  await expect(page.getByText('Nenhuma obrigação cadastrada', { exact: true })).toBeVisible();
  await page.getByLabel('Espaço financeiro', { exact: true }).selectOption(`${tenant}/${first}`);
  await expect(page.getByText('Compromisso da Família', { exact: true })).toBeVisible();
});
