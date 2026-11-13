import { test, expect, type Page } from '@playwright/test';

const user = '11111111-1111-4111-8111-111111111111';
const tenant = '22222222-2222-4222-8222-222222222222';
const first = '33333333-3333-4333-8333-333333333333';
const second = '44444444-4444-4444-8444-444444444444';
const bank = '55555555-5555-4555-8555-555555555555';
const person = '66666666-6666-4666-8666-666666666666';
type Row = Record<string, unknown>;
async function backend(page: Page) {
  const state = { authenticated: true, scope: first, reader: false, selectStarted: false, releaseSelect: undefined as undefined | (() => void), holdSelect: false, writes: [] as Row[] };
  const data: Record<string, Record<string, Row[]>> = {
    [first]: { pessoas: [{ id: person, name: 'Pessoa da Família', active: true, version: 1 }], instituicoes: [{ id: bank, name: 'Banco da Família', active: true, version: 1 }], categorias: [], bandeiras: [], contas: [], cartoes: [], 'posicoes-conta': [] },
    [second]: { pessoas: [{ id: '77777777-7777-4777-8777-777777777777', name: 'Pessoa da Viagem', active: true, version: 1 }], instituicoes: [{ id: '88888888-8888-4888-8888-888888888888', name: 'Banco da Viagem', active: true, version: 1 }], categorias: [], bandeiras: [], contas: [], cartoes: [], 'posicoes-conta': [] },
  };
  await page.route('**/api/v1/**', async route => {
    const req = route.request(); const url = new URL(req.url()); const path = url.pathname.replace('/api/v1/', ''); const method = req.method();
    const body = req.postData() ? JSON.parse(req.postData()!) : {};
    const json = (body: unknown, status = 200) => route.fulfill({ status, json: body });
    if (path === 'auth/csrf') return json({ token: 'csrf-browser-test' });
    if (path === 'auth/me') return state.authenticated ? json({ id: state.reader ? '99999999-9999-4999-8999-999999999999' : user, displayName: state.reader ? 'Bruno' : 'Ana', email: 'test@example.test', tenantId: tenant, spaceId: state.scope }) : json({}, 401);
    if (method !== 'GET' && req.headers()['x-csrf-token'] !== 'csrf-browser-test') return json({ title: 'CSRF invalid' }, 400);
    if (path === 'auth/login') { state.authenticated = true; state.reader = String(body.email).startsWith('reader'); return route.fulfill({ status: 204 }); }
    if (path === 'auth/register') return json({ id: user }, 201);
    if (path === 'auth/logout') { state.authenticated = false; return route.fulfill({ status: 204 }); }
    if (!state.authenticated) return json({}, 401);
    if (path === 'context/spaces') return json([{ tenantId: tenant, spaceId: first, name: 'Família', currency: 'BRL' }, { tenantId: tenant, spaceId: second, name: 'Viagem', currency: 'USD' }]);
    if (path === 'context/permissions') return json(state.reader ? ['cadastros.visualizar', 'dashboard.visualizar'] : ['cadastros.visualizar', 'cadastros.editar', 'dashboard.visualizar']);
    if (path === 'context/select') { state.selectStarted = true; if (state.holdSelect) await new Promise<void>(resolve => { state.releaseSelect = resolve; }); state.scope = body.spaceId; return route.fulfill({ status: 204 }); }
    const [kind, id] = path.split('/');
    const rows = data[state.scope][kind]; if (!rows) return json({}, 404);
    if (method === 'GET') {
      if (id) return rows.some(row => row.id === id) ? json(rows.find(row => row.id === id)) : json({}, 404);
      const search = (url.searchParams.get('search') || '').toLowerCase();
      const matches = rows.filter(row => (url.searchParams.get('includeInactive') !== 'false' || row.active) && (!search || String(row.name || row.number || '').toLowerCase().includes(search)) && (!url.searchParams.get('accountId') || row.accountId === url.searchParams.get('accountId')));
      return json({ items: matches, total: matches.length, page: Number(url.searchParams.get('page') || 1), pageSize: 20 });
    }
    if (state.reader) return json({}, 403);
    state.writes.push({ path, ...body });
    if (method === 'POST') {
      const row: Row = { ...body, id: crypto.randomUUID(), active: body.active ?? true, version: 1, availableCredit: null };
      if (kind === 'posicoes-conta') Object.assign(row, { currency: 'BRL', configuredOverdraftLimit: '100.00', availableOverdraft: '80.00', accountNumber: '0000123', institutionName: 'Banco da Família' });
      rows.push(row); return json(row, 201);
    }
    const index = rows.findIndex(row => row.id === id); if (index < 0) return json({}, 404);
    if (method === 'PUT') { rows[index] = { ...rows[index], ...body, version: Number(rows[index].version) + 1 }; return json(rows[index]); }
    if (method === 'DELETE') { rows.splice(index, 1); return route.fulfill({ status: 204 }); }
    return json({}, 405);
  });
  return state;
}

test('cadastros e posições mantêm identificadores literais e valores desconhecidos', async ({ page }, info) => {
  const state = await backend(page);
  await page.goto('/contas');
  await page.getByRole('button', { name: 'Adicionar conta', exact: true }).first().click();
  const dialog = page.getByRole('dialog');
  await dialog.getByLabel('Instituição', { exact: true }).selectOption(bank);
  await dialog.getByLabel('Agência', { exact: true }).fill('0001-A');
  await dialog.getByLabel('Número da conta', { exact: true }).fill('0000123');
  await dialog.getByLabel('Dígito', { exact: true }).fill('X-00');
  await dialog.getByLabel('Limite do cheque especial', { exact: false }).fill('100,00');
  await dialog.getByRole('button', { name: 'Salvar', exact: true }).click();
  await expect(page.getByRole('dialog')).toHaveCount(0);
  await expect(page.getByText('0000123', { exact: true })).toBeVisible();
  expect(state.writes.find(row => row.path === 'contas')).toMatchObject({ agency: '0001-A', number: '0000123', checkDigit: 'X-00', overdraftLimit: '100.00' });
  await page.getByRole('link', { name: 'Posições', exact: true }).click();
  await page.getByRole('button', { name: /Registrar posição/ }).first().click();
  await dialog.getByLabel('Data da posição', { exact: false }).fill('2026-11-09');
  await dialog.getByLabel('Saldo da conta', { exact: true }).fill('500,00');
  await dialog.getByLabel('Cheque especial utilizado', { exact: true }).fill('20,00');
  await dialog.getByRole('button', { name: 'Salvar', exact: true }).click();
  await expect(page.getByRole('dialog')).toHaveCount(0);
  await expect(page.getByRole('cell', { name: 'Não informado', exact: true })).toBeVisible();
  expect(state.writes.find(row => row.path === 'posicoes-conta')).toMatchObject({ positionDate: '2026-11-09', balance: '500.00', savingsBalance: null, overdraftUsed: '20.00' });
  await page.screenshot({ path: info.outputPath('positions-desktop.png'), fullPage: true });
});

test('trocar espaço oculta os dados antigos e logout remove permissões e cache', async ({ page }) => {
  const state = await backend(page);
  await page.goto('/cadastros/pessoas');
  await expect(page.getByText('Pessoa da Família', { exact: true })).toBeVisible();
  state.holdSelect = true;
  await page.getByLabel('Espaço financeiro', { exact: true }).selectOption(`${tenant}/${second}`);
  await expect.poll(() => state.selectStarted).toBe(true);
  await expect(page.getByText('Pessoa da Família', { exact: true })).toHaveCount(0);
  state.holdSelect = false; state.releaseSelect!();
  await expect(page.getByText('Pessoa da Viagem', { exact: true })).toBeVisible();
  await expect(page.getByText('Pessoa da Família', { exact: true })).toHaveCount(0);
  await page.getByRole('button', { name: 'Sair', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Entre na sua conta' })).toBeVisible();
  await expect(page.getByText('Pessoa da Viagem', { exact: true })).toHaveCount(0);
  await page.getByLabel('Email', { exact: true }).fill('reader@example.test');
  await page.getByLabel('Senha', { exact: true }).fill('TestPassword123!');
  await page.getByRole('button', { name: 'Entrar', exact: true }).click();
  await page.getByRole('link', { name: 'Pessoas', exact: true }).click();
  await expect(page.getByText('Pessoa da Viagem', { exact: true })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Adicionar pessoa', exact: true })).toHaveCount(0);
  await expect(page.getByText('Somente leitura', { exact: true })).toBeVisible();
});

test('navegação móvel e formulário de bandeiras são utilizáveis sem rolagem lateral da página', async ({ page }, info) => {
  await backend(page); await page.setViewportSize({ width: 390, height: 844 });
  await page.goto('/cadastros/bandeiras');
  await page.getByRole('button', { name: 'Adicionar bandeira', exact: true }).first().click();
  const dialog = page.getByRole('dialog');
  await dialog.getByLabel('Nome', { exact: false }).fill('Minha bandeira');
  await dialog.getByRole('button', { name: 'Salvar', exact: true }).click();
  await expect(page.getByText('Minha bandeira', { exact: true })).toBeVisible();
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true);
  await page.getByRole('button', { name: 'Abrir navegação' }).click();
  await page.getByRole('link', { name: 'Contas', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Contas', exact: true })).toBeVisible();
  await page.screenshot({ path: info.outputPath('accounts-mobile.png'), fullPage: true });
});
