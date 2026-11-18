import { test, expect } from '@playwright/test';

test('importação revisa, decide e confirma registros com CSRF', async ({ page }) => {
  const batch = 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa';
  const row = 'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb';
  let decision = 'Pending';
  let confirmed = false;
  await page.route('**/api/v1/**', async route => {
    const request = route.request();
    const url = new URL(request.url());
    const path = url.pathname.replace('/api/v1/', '');
    if (path === 'auth/csrf') return route.fulfill({ status: 200, json: { token: 'csrf-import' } });
    if (request.method() !== 'GET' && request.headers()['x-csrf-token'] !== 'csrf-import')
      return route.fulfill({ status: 400, json: { title: 'CSRF invalid' } });
    if (path === 'auth/me') return route.fulfill({ status: 200, json: { id: 'user', displayName: 'Ana', email: 'ana@example.test', tenantId: 'tenant', spaceId: 'space' } });
    if (path === 'context/spaces') return route.fulfill({ status: 200, json: [{ tenantId: 'tenant', spaceId: 'space', name: 'Família', currency: 'BRL' }] });
    if (path === 'context/permissions') return route.fulfill({ status: 200, json: ['importacoes.visualizar', 'importacoes.executar', 'dashboard.visualizar'] });
    if (path === 'importacoes' && request.method() === 'POST') return route.fulfill({ status: 201, json: { batchId: batch, state: 'AwaitingReview', totalRows: 1, eligibleRows: 1, errorCount: 0, warningCount: 0, infoCount: 0 } });
    if (path === `importacoes/${batch}` && request.method() === 'GET') return route.fulfill({ status: 200, json: { id: batch, originalFileName: 'dividas.xlsx', sizeBytes: 120, parserVersion: 'test', state: confirmed ? 'Confirmed' : 'AwaitingReview', statisticsJson: '{}', version: 1, createdAt: '2026-11-18T00:00:00Z' } });
    if (path === `importacoes/${batch}/registros`) return route.fulfill({ status: 200, json: { items: [{ id: row, sheetName: '2026', rowNumber: 2, cellReference: 'A2', sourceType: 'Annual', classification: 'Record', derivedValuesJson: '{}', issueCode: null, issueMessage: null, severity: 'Info', decision, version: 1 }], total: 1, page: 1, pageSize: 100 } });
    if (path === `importacoes/${batch}/registros/${row}/decisao`) { decision = JSON.parse(request.postData() ?? '{}').decision; return route.fulfill({ status: 200, json: { id: row, sheetName: '2026', rowNumber: 2, cellReference: 'A2', sourceType: 'Annual', classification: 'Record', derivedValuesJson: '{}', issueCode: null, issueMessage: null, severity: 'Info', decision, version: 2 } }); }
    if (path === `importacoes/${batch}/confirmar`) { confirmed = true; return route.fulfill({ status: 200, json: { batchId: batch, state: 'Confirmed', created: 1, reused: 0, rejected: 0, failed: 0 } }); }
    return route.fulfill({ status: 404, json: { title: 'Not found' } });
  });
  await page.goto('/importacao');
  await expect(page.getByRole('heading', { name: 'Importação de planilha' })).toBeVisible();
  await page.getByLabel('Arquivo XLSX').setInputFiles({ name: 'dividas.xlsx', mimeType: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet', buffer: Buffer.from('xlsx') });
  await expect(page.getByText('dividas.xlsx', { exact: true })).toBeVisible();
  await page.getByRole('button', { name: 'Revisar' }).click();
  await page.getByRole('button', { name: 'Aprovar' }).click();
  await page.getByRole('button', { name: 'Confirmar aprovados' }).click();
  await expect(page.getByText(/Importação confirmada/)).toBeVisible();
});
