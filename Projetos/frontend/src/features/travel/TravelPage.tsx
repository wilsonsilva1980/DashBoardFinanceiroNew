import { useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useScopeKey } from '../../app/Auth';
import { useWorkspace } from '../../app/Workspace';
import { get, request } from '../../shared/api/client';
import type { ExchangeRate, ItemConversion, Page, TravelBudget, TravelBudgetReconciliation, TravelItem } from '../../shared/api/contracts';
import { Dialog } from '../../shared/ui/Dialog';
import { Loading, Pagination, QueryError } from '../../shared/ui/QueryStatus';

export function TravelPage() {
  const scope = useScopeKey();
  const { canEdit } = useWorkspace();
  const [params, setParams] = useSearchParams();
  const parsed = Number(params.get('page') || 1);
  const page = Number.isSafeInteger(parsed) && parsed > 0 ? parsed : 1;
  const [creating, setCreating] = useState(false);
  const [selected, setSelected] = useState<TravelBudget | null>(null);
  const [ratesOpen, setRatesOpen] = useState(false);
  const budgets = useQuery({
    queryKey: [...scope, 'viagens', page],
    queryFn: ({ signal }) => get<Page<TravelBudget>>(`/api/v1/viagens?${new URLSearchParams({ page: String(page), pageSize: '20' })}`, signal),
  });

  return (
    <section className="workspace-page">
      <div className="page-heading">
        <div>
          <p className="eyebrow">Orçamento de viagem</p>
          <h1>Viagens</h1>
          <p className="muted">Orçamentos multimoeda, itens, conversões com taxa capturada e reconciliação.</p>
        </div>
        <div className="button-row">
          {canEdit && <button className="secondary" onClick={() => setRatesOpen(true)}>Cotações</button>}
          {canEdit ? <button className="primary" onClick={() => setCreating(true)}><span aria-hidden>＋</span> Nova viagem</button> : <span className="read-only">Somente leitura</span>}
        </div>
      </div>

      {selected ? (
        <TravelDetail budget={selected} onBack={() => setSelected(null)} />
      ) : (
        <div className="data-card">
          {budgets.isPending ? <Loading /> : budgets.isError ? <QueryError retry={() => void budgets.refetch()} message={budgets.error.message} /> : !budgets.data.items.length ? (
            <div className="empty-state">
              <span className="empty-symbol" aria-hidden>✈</span>
              <h2>Nenhuma viagem cadastrada</h2>
              <p>Crie um orçamento de viagem para registrar itens em várias moedas.</p>
              {canEdit && <button className="secondary" onClick={() => setCreating(true)}>Nova viagem</button>}
            </div>
          ) : (
            <div className="table-scroll">
              <table>
                <thead><tr><th>Nome</th><th>Período</th><th className="actions-heading">Ações</th></tr></thead>
                <tbody>
                  {budgets.data.items.map(row => (
                    <tr key={row.id}>
                      <td><strong>{row.name}</strong></td>
                      <td>{row.startDate || '—'}{row.endDate ? ` · ${row.endDate}` : ''}</td>
                      <td><div className="row-actions"><button className="text-button" onClick={() => setSelected(row)}>Gerenciar</button></div></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
          {budgets.data && <Pagination page={page} total={budgets.data.total} pageSize={20} onPage={next => setParams({ page: String(next) })} />}
        </div>
      )}

      {creating && <BudgetForm onClose={() => setCreating(false)} />}
      {ratesOpen && <RatesDialog onClose={() => setRatesOpen(false)} />}
    </section>
  );
}

function TravelDetail({ budget, onBack }: { budget: TravelBudget; onBack(): void }) {
  const scope = useScopeKey();
  const { canEdit } = useWorkspace();
  const [addingItem, setAddingItem] = useState(false);
  const [converting, setConverting] = useState<TravelItem | null>(null);
  const items = useQuery({
    queryKey: [...scope, 'viagens', budget.id, 'itens'],
    queryFn: ({ signal }) => get<Page<TravelItem>>(`/api/v1/viagens/${budget.id}/itens?pageSize=100`, signal),
  });
  const reconciliation = useQuery({
    queryKey: [...scope, 'viagens', budget.id, 'reconciliacao'],
    queryFn: ({ signal }) => get<TravelBudgetReconciliation>(`/api/v1/viagens/${budget.id}/reconciliacao`, signal),
  });

  return (
    <div className="data-card">
      <div className="table-toolbar">
        <div>
          <strong>{budget.name}</strong>
          <p className="muted">{budget.startDate || 'Sem início'} {budget.endDate ? `até ${budget.endDate}` : ''}</p>
        </div>
        <div className="button-row">
          <button className="secondary" onClick={onBack}>Voltar</button>
          {canEdit && <button className="primary" onClick={() => setAddingItem(true)}>Adicionar item</button>}
        </div>
      </div>
      {reconciliation.data && (reconciliation.data.discrepancies.length > 0 || reconciliation.data.balance) ? (
        <div className="notice" role="status">
          <p><strong>Reconciliação:</strong> saldo {reconciliation.data.balance ?? '—'}</p>
          {reconciliation.data.discrepancies.map((d, i) => (
            <p key={i}>{d.kind}: {d.message}{d.amount ? ` (${d.amount})` : ''}</p>
          ))}
        </div>
      ) : null}
      {items.isPending ? <Loading /> : items.isError ? <QueryError retry={() => void items.refetch()} message={items.error.message} /> : !items.data.items.length ? (
        <div className="empty-state"><h2>Nenhum item</h2><p>Adicione despesas/previsões para este orçamento.</p></div>
      ) : (
        <div className="table-scroll">
          <table>
            <thead><tr><th>Descrição</th><th>Moeda</th><th>Base</th><th>Taxas</th><th>Estado</th><th className="actions-heading">Ações</th></tr></thead>
            <tbody>
              {items.data.items.map(item => (
                <tr key={item.id}>
                  <td><strong>{item.description}</strong></td>
                  <td>{item.currency ?? '—'}</td>
                  <td>{item.baseAmount ?? '—'}</td>
                  <td>{item.fees ?? '—'}</td>
                  <td>{item.state}</td>
                  <td><div className="row-actions">{canEdit && item.currency && <button className="text-button" onClick={() => setConverting(item)}>Converter</button>}</div></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
      {addingItem && <ItemForm budgetId={budget.id} onClose={() => setAddingItem(false)} />}
      {converting && <ConversionForm budgetId={budget.id} item={converting} onClose={() => setConverting(null)} />}
    </div>
  );
}

function BudgetForm({ onClose }: { onClose(): void }) {
  const scope = useScopeKey();
  const client = useQueryClient();
  const [name, setName] = useState('');
  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');
  const [error, setError] = useState('');
  const save = useMutation({
    mutationFn: () => request<TravelBudget>('/api/v1/viagens', { method: 'POST', body: { name, startDate: startDate || null, endDate: endDate || null } }),
    onSuccess: async () => { await client.invalidateQueries({ queryKey: scope }); onClose(); },
    onError: cause => setError(cause instanceof Error ? cause.message : 'Não foi possível salvar.'),
  });
  return (
    <Dialog title="Nova viagem" onClose={onClose}>
      <p className="context-line">Crie um orçamento multimoeda.</p>
      {error && <p className="error-banner" role="alert">{error}</p>}
      <form onSubmit={e => { e.preventDefault(); save.mutate(); }}>
        <fieldset disabled={save.isPending}>
          <div className="form-grid">
            <div className="field"><label>Nome</label><input value={name} onChange={e => setName(e.target.value)} required /></div>
            <div className="field"><label>Início</label><input type="date" value={startDate} onChange={e => setStartDate(e.target.value)} /></div>
            <div className="field"><label>Fim</label><input type="date" value={endDate} onChange={e => setEndDate(e.target.value)} /></div>
          </div>
          <div className="dialog-footer"><div className="button-row"><button type="button" className="secondary" onClick={onClose}>Cancelar</button><button className="primary">{save.isPending ? 'Salvando…' : 'Salvar'}</button></div></div>
        </fieldset>
      </form>
    </Dialog>
  );
}

function ItemForm({ budgetId, onClose }: { budgetId: string; onClose(): void }) {
  const scope = useScopeKey();
  const client = useQueryClient();
  const [description, setDescription] = useState('');
  const [currency, setCurrency] = useState('USD');
  const [baseAmount, setBaseAmount] = useState('');
  const [fees, setFees] = useState('0.00');
  const [error, setError] = useState('');
  const save = useMutation({
    mutationFn: () => request<TravelItem>(`/api/v1/viagens/${budgetId}/itens`, { method: 'POST', body: { travelId: budgetId, description, state: 'Budget', currency, baseAmount: baseAmount || null, fees: fees || null } }),
    onSuccess: async () => { await client.invalidateQueries({ queryKey: scope }); onClose(); },
    onError: cause => setError(cause instanceof Error ? cause.message : 'Não foi possível salvar.'),
  });
  return (
    <Dialog title="Novo item" onClose={onClose}>
      <p className="context-line">Itens podem ter moeda original, valor base e taxas.</p>
      {error && <p className="error-banner" role="alert">{error}</p>}
      <form onSubmit={e => { e.preventDefault(); save.mutate(); }}>
        <fieldset disabled={save.isPending}>
          <div className="form-grid">
            <div className="field"><label>Descrição</label><input value={description} onChange={e => setDescription(e.target.value)} required /></div>
            <div className="field"><label>Moeda</label><select value={currency} onChange={e => setCurrency(e.target.value)}><option>USD</option><option>EUR</option><option>CLP</option><option>BRL</option><option>ARS</option><option>GBP</option></select></div>
            <div className="field"><label>Valor base</label><input value={baseAmount} onChange={e => setBaseAmount(e.target.value)} inputMode="decimal" /></div>
            <div className="field"><label>Taxas</label><input value={fees} onChange={e => setFees(e.target.value)} inputMode="decimal" /></div>
          </div>
          <div className="dialog-footer"><div className="button-row"><button type="button" className="secondary" onClick={onClose}>Cancelar</button><button className="primary">{save.isPending ? 'Salvando…' : 'Salvar'}</button></div></div>
        </fieldset>
      </form>
    </Dialog>
  );
}

function ConversionForm({ budgetId, item, onClose }: { budgetId: string; item: TravelItem; onClose(): void }) {
  const scope = useScopeKey();
  const client = useQueryClient();
  const [destinationCurrency, setDestinationCurrency] = useState('BRL');
  const [rateId, setRateId] = useState('');
  const [mode, setMode] = useState('Calculated');
  const [reportedAmount, setReportedAmount] = useState('');
  const [error, setError] = useState('');
  const rates = useQuery({
    queryKey: [...scope, 'cotacoes', item.currency],
    queryFn: ({ signal }) => get<Page<ExchangeRate>>('/api/v1/cotacoes?pageSize=100', signal),
  });
  const conversions = useQuery({
    queryKey: [...scope, 'viagens', budgetId, item.id, 'conversoes'],
    queryFn: ({ signal }) => get<Page<ItemConversion>>(`/api/v1/viagens/${budgetId}/itens/${item.id}/conversoes?pageSize=50`, signal),
  });
  const save = useMutation({
    mutationFn: () => request<ItemConversion>(`/api/v1/viagens/${budgetId}/itens/${item.id}/conversoes`, { method: 'POST', body: { itemId: item.id, rateId: rateId || null, destinationCurrency, mode, reportedAmount: reportedAmount || null } }),
    onSuccess: async () => { await client.invalidateQueries({ queryKey: scope }); setError(''); },
    onError: cause => setError(cause instanceof Error ? cause.message : 'Não foi possível salvar.'),
  });
  const rateOptions = (rates.data?.items ?? []).filter(r => !item.currency || r.sourceCurrency === item.currency).map(r => ({ value: r.id, label: `${r.sourceCurrency}→${r.destinationCurrency} ${r.rate} (${r.rateDate ?? 's/data'})` }));

  return (
    <Dialog title="Conversão do item" onClose={onClose}>
      <p className="context-line">{item.description} · {item.currency} · {(parseFloat(item.baseAmount || '0') + parseFloat(item.fees || '0')).toFixed(2)}</p>
      {error && <p className="error-banner" role="alert">{error}</p>}
      <form onSubmit={e => { e.preventDefault(); save.mutate(); }}>
        <fieldset disabled={save.isPending}>
          <div className="form-grid">
            <div className="field"><label>Moeda destino</label><select value={destinationCurrency} onChange={e => setDestinationCurrency(e.target.value)}><option>BRL</option><option>USD</option><option>EUR</option><option>CLP</option></select></div>
            <div className="field"><label>Modo</label><select value={mode} onChange={e => setMode(e.target.value)}><option>Calculated</option><option>Reported</option></select></div>
            <div className="field"><label>Cotação</label><select value={rateId} onChange={e => setRateId(e.target.value)}><option value="">Nenhuma</option>{rateOptions.map(o => <option key={o.value} value={o.value}>{o.label}</option>)}</select></div>
            {mode === 'Reported' && <div className="field"><label>Valor informado</label><input value={reportedAmount} onChange={e => setReportedAmount(e.target.value)} inputMode="decimal" /></div>}
          </div>
          <div className="dialog-footer"><div className="button-row"><button type="button" className="secondary" onClick={onClose}>Fechar</button><button className="primary">{save.isPending ? 'Salvando…' : 'Salvar conversão'}</button></div></div>
        </fieldset>
      </form>
      <div className="data-card" style={{ marginTop: '16px' }}>
        <div className="table-toolbar"><strong>Conversões</strong></div>
        {conversions.data?.items.length ? (
          <div className="table-scroll">
            <table>
              <thead><tr><th>Destino</th><th>Modo</th><th>Valor</th><th>Taxa aplicada</th><th>Data da taxa</th></tr></thead>
              <tbody>{conversions.data.items.map(c => <tr key={c.id}><td>{c.destinationCurrency}</td><td>{c.mode}</td><td>{c.reportedAmount ?? '—'}</td><td>{c.appliedRate ?? '—'}</td><td>{c.appliedRateDate ?? '—'}</td></tr>)}</tbody>
            </table>
          </div>
        ) : <p className="muted" style={{ padding: '12px' }}>Sem conversões ainda.</p>}
      </div>
    </Dialog>
  );
}

function RatesDialog({ onClose }: { onClose(): void }) {
  const scope = useScopeKey();
  const client = useQueryClient();
  const [sourceCurrency, setSourceCurrency] = useState('USD');
  const [destinationCurrency, setDestinationCurrency] = useState('BRL');
  const [rateDate, setRateDate] = useState('');
  const [rate, setRate] = useState('');
  const [source, setSource] = useState('Manual');
  const [error, setError] = useState('');
  const rates = useQuery({
    queryKey: [...scope, 'cotacoes', 'dialog'],
    queryFn: ({ signal }) => get<Page<ExchangeRate>>('/api/v1/cotacoes?pageSize=50', signal),
  });
  const save = useMutation({
    mutationFn: () => request<ExchangeRate>('/api/v1/cotacoes', { method: 'POST', body: { sourceCurrency, destinationCurrency, rateDate: rateDate || null, rate, source } }),
    onSuccess: async () => { await client.invalidateQueries({ queryKey: scope }); setRate(''); setError(''); },
    onError: cause => setError(cause instanceof Error ? cause.message : 'Não foi possível salvar.'),
  });
  return (
    <Dialog title="Cotações" onClose={onClose}>
      <p className="context-line">Registre taxas com data e origem para conversões rastreáveis.</p>
      {error && <p className="error-banner" role="alert">{error}</p>}
      <form onSubmit={e => { e.preventDefault(); save.mutate(); }}>
        <fieldset disabled={save.isPending}>
          <div className="form-grid">
            <div className="field"><label>Origem</label><input value={sourceCurrency} onChange={e => setSourceCurrency(e.target.value.toUpperCase())} maxLength={3} /></div>
            <div className="field"><label>Destino</label><input value={destinationCurrency} onChange={e => setDestinationCurrency(e.target.value.toUpperCase())} maxLength={3} /></div>
            <div className="field"><label>Data</label><input type="date" value={rateDate} onChange={e => setRateDate(e.target.value)} /></div>
            <div className="field"><label>Taxa</label><input value={rate} onChange={e => setRate(e.target.value)} inputMode="decimal" required /></div>
            <div className="field"><label>Fonte</label><input value={source} onChange={e => setSource(e.target.value)} /></div>
          </div>
          <div className="dialog-footer"><div className="button-row"><button type="button" className="secondary" onClick={onClose}>Fechar</button><button className="primary">{save.isPending ? 'Salvando…' : 'Salvar cotação'}</button></div></div>
        </fieldset>
      </form>
      <div className="table-scroll" style={{ marginTop: '12px' }}>
        {rates.data?.items.length ? (
          <table><thead><tr><th>Par</th><th>Taxa</th><th>Data</th><th>Fonte</th></tr></thead><tbody>{rates.data.items.map(r => <tr key={r.id}><td>{r.sourceCurrency}/{r.destinationCurrency}</td><td>{r.rate}</td><td>{r.rateDate ?? '—'}</td><td>{r.source}</td></tr>)}</tbody></table>
        ) : <p className="muted">Sem cotações.</p>}
      </div>
    </Dialog>
  );
}
