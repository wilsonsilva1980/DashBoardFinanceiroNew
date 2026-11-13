import { test, expect, type Page } from '@playwright/test';

const user = '11111111-1111-4111-8111-111111111111';
const tenant = '22222222-2222-4222-8222-222222222222';
const space = '33333333-3333-4333-8333-333333333333';
const account = '55555555-5555-4555-8555-555555555555';
const person = '66666666-6666-4666-8666-666666666666';
const obligation = '77777777-7777-4777-8777-777777777777';
const title = '88888888-8888-4888-8888-888888888888';
type Row = Record<string, unknown>;

async function backend(page: Page) {
  const state = { authenticated: true, writes: [] as Array<{ path: string } & Row> };
  const now = new Date().toISOString();
  const titles = [{ id: title, version: 1, obligationId: obligation, responsibleId: null, cardId: null, period: '2026-11-01', dueDate: null, installmentNumber: null, occurrenceKey: 'aluguel-2026-11', originalAmount: '100.00', state: 'Effective', cancelledAt: null, balance: '100.00', settledAmount: '0.00', excessCredit: '0.00', settlementStatus: 'Open' }];
  const payments: Row[] = [];
  const settlements: Row[] = [];
  const agreements: Row[] = [];
  await page.route('**/api/v1/**', async route => {
    const req = route.request(); const url = new URL(req.url()); const path = url.pathname.replace('/api/v1/', ''); const method = req.method();
    const body = req.postData() ? JSON.parse(req.postData()!) : {};
    const json = (payload: unknown, status = 200) => route.fulfill({ status, json: payload });
    if (path === 'auth/csrf') return json({ token: 'csrf-browser-test' });
    if (path === 'auth/me') return state.authenticated ? json({ id: user, displayName: 'Ana', email: 'test@example.test', tenantId: tenant, spaceId: space }) : json({}, 401);
    if (method !== 'GET' && req.headers()['x-csrf-token'] !== 'csrf-browser-test') return json({ title: 'CSRF invalid' }, 400);
    if (!state.authenticated) return json({}, 401);
    if (path === 'context/spaces') return json([{ tenantId: tenant, spaceId: space, name: 'Família', currency: 'BRL' }]);
    if (path === 'context/permissions') return json(['cadastros.visualizar', 'cadastros.editar', 'dashboard.visualizar']);
    const pageResult = (items: Row[], pageSize = 100) => json({ items, total: items.length, page: 1, pageSize });
    if (path === 'pessoas') return pageResult([{ id: person, name: 'Ana', active: true, version: 1 }]);
    if (path === 'contas') return pageResult([{ id: account, name: 'Conta Casa', number: '1', active: true, version: 1 }]);
    if (path === 'obrigacoes') return pageResult([{ id: obligation, description: 'Aluguel', version: 1, active: true, kind: 'Single', currency: 'BRL' }]);
    if (path === 'titulos') return pageResult(titles);
    if (path.startsWith('titulos/')) {
      const [, titleId, section, settlementId, action] = path.split('/');
      if (section !== 'liquidacoes-historicas') return json({}, 404);
      if (method === 'GET') return json(settlements.filter(row => row.titleId === titleId));
      if (!settlementId) {
        const entry = { ...body, id: crypto.randomUUID(), titleId, approvedBy: user, revokedAt: null, revocationReason: null, version: 1 };
        settlements.push(entry); state.writes.push({ path, ...body }); return json(entry, 201);
      }
      if (action === 'revogar') {
        const index = settlements.findIndex(row => row.id === settlementId); if (index < 0) return json({}, 404);
        settlements[index] = { ...settlements[index], revokedAt: now, revocationReason: body.reason, version: Number(settlements[index].version) + 1 };
        state.writes.push({ path, ...body }); return json(settlements[index]);
      }
      return json({}, 405);
    }
    if (path === 'pagamentos') {
      if (method === 'GET') return pageResult(payments);
      if (body.currency !== 'BRL') return json({ detail: 'A moeda do pagamento deve coincidir com a moeda dos títulos.' }, 400);
      const entry = { ...body, id: crypto.randomUUID(), state: 'Draft', reversedAt: null, reversalReason: null, version: 1 };
      payments.push(entry); state.writes.push({ path, ...body }); return json(entry, 201);
    }
    if (path.startsWith('pagamentos/')) {
      const [, id, action] = path.split('/');
      const index = payments.findIndex(row => row.id === id); if (index < 0) return json({}, 404);
      if (action === 'confirmar') payments[index] = { ...payments[index], state: 'Confirmed', version: Number(payments[index].version) + 1 };
      else if (action === 'reverter') payments[index] = { ...payments[index], state: 'Reversed', reversedAt: now, reversalReason: body.reason, version: Number(payments[index].version) + 1 };
      else return json({}, 405);
      state.writes.push({ path, ...body }); return json(payments[index]);
    }
    if (path === 'acordos') {
      if (method === 'GET') return pageResult(agreements);
      const proposed = body.titles.reduce((total: number, line: Row) => total + Number(line.nominalAmount) - Number(line.discount), 0);
      const entry = { ...body, id: crypto.randomUUID(), proposedAmount: proposed.toFixed(2), state: 'Draft', paymentId: null, version: 1 };
      agreements.push(entry); state.writes.push({ path, ...body }); return json(entry, 201);
    }
    if (path === 'acordos/simulacoes') {
      const proposed = body.titles.reduce((total: number, line: Row) => total + Number(line.nominalAmount) - Number(line.discount), 0);
      state.writes.push({ path, ...body });
      return json({ ...body, id: crypto.randomUUID(), proposedAmount: proposed.toFixed(2), titles: body.titles }, 201);
    }
    if (path.startsWith('acordos/')) {
      const [, id, action] = path.split('/');
      const index = agreements.findIndex(row => row.id === id); if (index < 0) return json({}, 404);
      if (action !== 'confirmar') return json({}, 405);
      agreements[index] = { ...agreements[index], state: 'Confirmed', paymentId: crypto.randomUUID(), version: Number(agreements[index].version) + 1 };
      state.writes.push({ path, ...body }); return json(agreements[index]);
    }
    return json({}, 404);
  });
  return state;
}

test('pagamento é criado, confirmado e revertido sem duplicar alocações', async ({ page }) => {
  const state = await backend(page);
  await page.goto('/pagamentos');
  await page.getByRole('button', { name: 'Novo pagamento', exact: true }).first().click();
  const dialog = page.getByRole('dialog');
  await dialog.getByLabel('Conta', { exact: true }).selectOption(account);
  await dialog.getByLabel('Pagador', { exact: true }).selectOption(person);
  await dialog.getByLabel('Data do pagamento', { exact: false }).fill('2026-11-12');
  await dialog.getByLabel('Título da alocação', { exact: true }).selectOption(title);
  await dialog.getByLabel('Valor do pagamento', { exact: false }).fill('200,00');
  await dialog.getByLabel('Forma de pagamento', { exact: false }).fill('Pix');
  await dialog.getByRole('button', { name: 'Salvar', exact: true }).click();
  await expect(page.getByText('A soma das alocações deve coincidir com o valor do pagamento.')).toBeVisible();
  await dialog.getByLabel('Valor do pagamento', { exact: false }).fill('100,00');
  await dialog.getByRole('button', { name: 'Salvar', exact: true }).click();
  await expect(page.getByRole('dialog')).toHaveCount(0);
  await expect(page.getByText('Rascunho', { exact: true })).toBeVisible();
  const created = state.writes.find(row => row.path === 'pagamentos')!;
  expect(created).toMatchObject({ amount: '100.00', currency: 'BRL', method: 'Pix', allocations: [{ titleId: title, amount: '100.00' }] });

  await page.getByRole('button', { name: /Confirmar pagamento/ }).click();
  await expect(page.getByText('Confirmado', { exact: true })).toBeVisible();
  expect(state.writes.find(row => row.path.endsWith('/confirmar'))).toMatchObject({ version: 1 });

  await page.getByRole('button', { name: /Reverter pagamento/ }).click();
  await page.getByLabel('Motivo da reversão', { exact: false }).fill('Pagamento duplicado');
  await page.getByRole('button', { name: 'Reverter', exact: true }).click();
  await expect(page.getByText('Revertido', { exact: true })).toBeVisible();
  expect(state.writes.find(row => row.path.endsWith('/reverter'))).toMatchObject({ reason: 'Pagamento duplicado', version: 2 });
});

test('pagamento com moeda divergente é rejeitado com mensagem', async ({ page }) => {
  await backend(page);
  await page.goto('/pagamentos');
  await page.getByRole('button', { name: 'Novo pagamento', exact: true }).first().click();
  const dialog = page.getByRole('dialog');
  await dialog.getByLabel('Conta', { exact: true }).selectOption(account);
  await dialog.getByLabel('Pagador', { exact: true }).selectOption(person);
  await dialog.getByLabel('Data do pagamento', { exact: false }).fill('2026-11-12');
  await dialog.getByLabel('Título da alocação', { exact: true }).selectOption(title);
  await dialog.getByLabel('Valor do pagamento', { exact: false }).fill('100,00');
  await dialog.getByLabel('Moeda', { exact: true }).selectOption('USD');
  await dialog.getByLabel('Forma de pagamento', { exact: false }).fill('Pix');
  await dialog.getByRole('button', { name: 'Salvar', exact: true }).click();
  await expect(page.getByText('A moeda do pagamento deve coincidir com a moeda dos títulos.')).toBeVisible();
  await expect(page.getByRole('dialog')).toBeVisible();
});

test('baixa histórica sem data afeta o saldo e pode ser revogada com motivo', async ({ page }) => {
  const state = await backend(page);
  await page.goto(`/obrigacoes/${obligation}/titulos`);
  await page.getByRole('button', { name: /Baixas históricas do título/ }).click();
  const dialog = page.getByRole('dialog');
  await expect(dialog.getByText('Nenhuma baixa registrada.')).toBeVisible();
  await dialog.getByLabel('Valor da baixa', { exact: false }).fill('40,00');
  await dialog.getByLabel('Justificativa', { exact: false }).fill('Comprovante sem data');
  await dialog.getByRole('button', { name: 'Registrar baixa', exact: true }).click();
  await expect(dialog.getByText('Sem data', { exact: false })).toBeVisible();
  const write = (suffix: string) => state.writes.find(row => row.path.endsWith(suffix));
  await expect.poll(() => write('/liquidacoes-historicas')).toMatchObject({ amount: '40.00', effectiveDate: null, justification: 'Comprovante sem data' });

  await dialog.getByPlaceholder('Motivo').fill('Baixa duplicada');
  await dialog.getByRole('button', { name: 'Revogar', exact: true }).click();
  await expect(dialog.getByText('Revogada: Baixa duplicada', { exact: false })).toBeVisible();
  await expect.poll(() => write('/revogar')).toMatchObject({ reason: 'Baixa duplicada', version: 1 });
});

test('acordo simula desconto e confirma sem criar novo título', async ({ page }) => {
  const state = await backend(page);
  await page.goto('/acordos');
  await page.getByRole('button', { name: 'Novo acordo', exact: true }).first().click();
  const dialog = page.getByRole('dialog');
  await dialog.getByLabel('Obrigação', { exact: true }).selectOption(obligation);
  await dialog.getByLabel('Incluir título aluguel-2026-11', { exact: true }).check();
  await dialog.getByLabel('Desconto do título aluguel-2026-11', { exact: true }).fill('20,00');
  await dialog.getByRole('button', { name: 'Simular', exact: true }).click();
  await expect(dialog.getByText('Líquido simulado: 80,00', { exact: false })).toBeVisible();
  const simulation = state.writes.find(row => row.path === 'acordos/simulacoes')!;
  expect(simulation).toMatchObject({ titles: [{ titleId: title, nominalAmount: '100.00', discount: '20.00' }] });

  await dialog.getByRole('button', { name: 'Criar acordo', exact: true }).click();
  await expect(page.getByRole('dialog')).toHaveCount(0);
  await expect(page.getByText('Rascunho', { exact: true })).toBeVisible();
  expect(state.writes.find(row => row.path === 'acordos')).toMatchObject({ obligationId: obligation, titles: [{ titleId: title, nominalAmount: '100.00', discount: '20.00' }] });

  await page.getByRole('button', { name: /Confirmar acordo/ }).click();
  await dialog.getByLabel('Conta', { exact: true }).selectOption(account);
  await dialog.getByLabel('Pagador', { exact: true }).selectOption(person);
  await dialog.getByLabel('Data do pagamento', { exact: false }).fill('2026-11-12');
  await dialog.getByLabel('Forma de pagamento', { exact: false }).fill('Pix');
  await dialog.getByRole('button', { name: 'Confirmar acordo', exact: true }).click();
  await expect(page.getByRole('dialog')).toHaveCount(0);
  await expect(page.getByText('Confirmado', { exact: true })).toBeVisible();
  expect(state.writes.find(row => row.path.endsWith('/confirmar'))).toMatchObject({ currency: 'BRL', method: 'Pix', version: 1 });
});
