import { useState, type FormEvent } from 'react';
import { useSearchParams } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useScopeKey } from '../../app/Auth';
import { useWorkspace } from '../../app/Workspace';
import { get, request } from '../../shared/api/client';
import type { Agreement, CatalogEntry, Obligation, Page, PayableTitle } from '../../shared/api/contracts';
import { money } from '../../shared/forms/format';
import { Dialog } from '../../shared/ui/Dialog';
import { Loading, Pagination, QueryError } from '../../shared/ui/QueryStatus';
import { Field, SelectField } from '../cadastros/CatalogForm';
import { currencies } from '../cadastros/catalogConfig';
import { agreementLineSchema, agreementStateLabels, paymentSchema } from './settlementConfig';

type Values = Record<string, string | boolean>;

export function AgreementsPage() {
  const scope = useScopeKey();
  const { canEdit } = useWorkspace();
  const client = useQueryClient();
  const [params, setParams] = useSearchParams();
  const parsed = Number(params.get('page') || 1);
  const page = Number.isSafeInteger(parsed) && parsed > 0 && parsed <= 1000000 ? parsed : 1;
  const [creating, setCreating] = useState(false);
  const [confirming, setConfirming] = useState<Agreement | null>(null);
  const [error, setError] = useState('');
  const agreements = useQuery({ queryKey: [...scope, 'agreements', page], queryFn: ({ signal }) => get<Page<Agreement>>(`/api/v1/acordos?${new URLSearchParams({ page: String(page), pageSize: '20' })}`, signal) });
  const obligations = useQuery({ queryKey: [...scope, 'lookup', 'obrigacoes'], queryFn: ({ signal }) => get<Page<Obligation>>('/api/v1/obrigacoes?pageSize=100', signal) });
  const description = (id: string) => obligations.data?.items.find(row => row.id === id)?.description ?? 'Obrigação';
  return <section className="workspace-page"><div className="page-heading"><div><p className="eyebrow">Negocie sem duplicar a dívida</p><h1>Acordos</h1><p className="muted">Simule descontos, confirme o acordo e liquide os títulos existentes de uma só vez.</p></div>{canEdit ? <button className="primary" onClick={() => setCreating(true)}><span aria-hidden>＋</span> Novo acordo</button> : <span className="read-only">Somente leitura</span>}</div>
    {error && <p className="error-banner" role="alert">{error}</p>}
    <div className="data-card">{agreements.isPending ? <Loading /> : agreements.isError ? <QueryError retry={() => void agreements.refetch()} message={agreements.error.message} /> : !agreements.data.items.length ? <div className="empty-state"><span className="empty-symbol" aria-hidden>◇</span><h2>Nenhum acordo registrado</h2><p>Simule um desconto sobre títulos existentes antes de confirmar.</p>{canEdit && <button className="secondary" onClick={() => setCreating(true)}>Novo acordo</button>}</div> : <div className="table-scroll" tabIndex={0} aria-label="Lista de acordos"><table><thead><tr><th>Obrigação</th><th>Proposta</th><th>Validade</th><th>Títulos</th><th>Valor líquido</th><th>Estado</th><th className="actions-heading">Ações</th></tr></thead><tbody>{agreements.data.items.map(row => <tr key={row.id}><td><strong>{description(row.obligationId)}</strong></td><td>{row.proposalDate ? row.proposalDate.split('-').reverse().join('/') : 'Não informada'}</td><td>{row.validUntil ? row.validUntil.split('-').reverse().join('/') : 'Não informada'}</td><td>{row.titles.length}</td><td className="amount">{money(row.proposedAmount)}</td><td><span className={`status-badge ${row.state === 'Confirmed' ? 'active' : ''}`}><span />{agreementStateLabels[row.state] ?? row.state}</span></td><td><div className="row-actions">{canEdit && row.state === 'Draft' && <button className="text-button" onClick={() => setConfirming(row)} aria-label={`Confirmar acordo de ${money(row.proposedAmount)}`}>Confirmar</button>}</div></td></tr>)}</tbody></table></div>}
      {agreements.data && <Pagination page={page} total={agreements.data.total} pageSize={20} onPage={next => setParams({ page: String(next) })} />}
    </div>
    {creating && <AgreementDialog onClose={() => setCreating(false)} />}
    {confirming && <AgreementConfirmDialog agreement={confirming} onClose={() => setConfirming(null)} />}
  </section>;
}

function AgreementDialog({ onClose }: { onClose(): void }) {
  const scope = useScopeKey();
  const client = useQueryClient();
  const [obligationId, setObligationId] = useState('');
  const [selected, setSelected] = useState<Record<string, { checked: boolean; discount: string }>>({});
  const [error, setError] = useState('');
  const obligations = useQuery({ queryKey: [...scope, 'lookup', 'obrigacoes'], queryFn: ({ signal }) => get<Page<Obligation>>('/api/v1/obrigacoes?pageSize=100', signal) });
  const titles = useQuery({
    queryKey: [...scope, 'agreement-titles', obligationId],
    enabled: !!obligationId,
    queryFn: ({ signal }) => get<Page<PayableTitle>>(`/api/v1/titulos?${new URLSearchParams({ obligationId, pageSize: '100' })}`, signal),
  });
  const build = (): { lines?: { titleId: string; nominalAmount: string; discount: string }[]; error?: string } => {
    if (!obligationId) return { error: 'Selecione a obrigação.' };
    const lines: { titleId: string; nominalAmount: string; discount: string }[] = [];
    for (const title of titles.data?.items ?? []) {
      const row = selected[title.id];
      if (!row?.checked) continue;
      const parsed = agreementLineSchema.safeParse({ titleId: title.id, nominalAmount: title.originalAmount, discount: row.discount || '0' });
      if (!parsed.success) return { error: parsed.error.issues[0]?.message ?? 'Revise os descontos informados.' };
      lines.push(parsed.data);
    }
    if (!lines.length) return { error: 'Selecione ao menos um título.' };
    return { lines };
  };
  const invalidate = async () => { await client.invalidateQueries({ queryKey: scope }); };
  const simulate = useMutation({
    mutationFn: (body: unknown) => request('/api/v1/acordos/simulacoes', { method: 'POST', body }),
    onSuccess: async () => { setError(''); await invalidate(); },
    onError: cause => setError(cause instanceof Error ? cause.message : 'Não foi possível simular o acordo.'),
  });
  const create = useMutation({
    mutationFn: (body: unknown) => request('/api/v1/acordos', { method: 'POST', body }),
    onSuccess: async () => { await invalidate(); onClose(); },
    onError: cause => setError(cause instanceof Error ? cause.message : 'Não foi possível criar o acordo.'),
  });
  const today = new Date().toISOString().slice(0, 10);
  const submit = (event: FormEvent) => {
    event.preventDefault(); setError('');
    const { lines, error: invalid } = build();
    if (!lines) { setError(invalid ?? 'Revise o acordo.'); return; }
    create.mutate({ obligationId, proposalDate: today, validUntil: null, titles: lines });
  };
  const runSimulation = () => {
    setError('');
    const { lines, error: invalid } = build();
    if (!lines) { setError(invalid ?? 'Revise o acordo.'); return; }
    simulate.mutate({ obligationId, name: `Simulação ${today}`, simulationDate: today, notes: null, titles: lines });
  };
  const simulated = simulate.data as { proposedAmount?: string } | undefined;
  return <Dialog title="Novo acordo" onClose={onClose}>
    {error && <p className="error-banner" role="alert">{error}</p>}
    <form onSubmit={submit}>
      <fieldset disabled={create.isPending}><div className="form-grid">
        <div className="field field-wide"><label htmlFor="agreement-obligation">Obrigação</label><select id="agreement-obligation" value={obligationId} onChange={event => { setObligationId(event.target.value); setSelected({}); }}><option value="">Selecione…</option>{obligations.data?.items.map(row => <option key={row.id} value={row.id}>{row.description}</option>)}</select></div>
        <div className="field field-wide"><span className="field-hint">Títulos e descontos explícitos</span>{!obligationId ? <p className="muted">Selecione a obrigação para listar os títulos.</p> : !titles.data?.items.length ? <p className="muted">Nenhum título disponível.</p> : <table><thead><tr><th>Incluir</th><th>Título</th><th>Valor nominal</th><th>Desconto</th></tr></thead><tbody>{titles.data.items.map(title => <tr key={title.id}><td><input type="checkbox" aria-label={`Incluir título ${title.occurrenceKey}`} checked={selected[title.id]?.checked ?? false} onChange={event => setSelected(current => ({ ...current, [title.id]: { checked: event.target.checked, discount: current[title.id]?.discount ?? '0,00' } }))} /></td><td>{title.occurrenceKey}</td><td className="amount">{money(title.originalAmount)}</td><td><input aria-label={`Desconto do título ${title.occurrenceKey}`} inputMode="decimal" disabled={!selected[title.id]?.checked} value={selected[title.id]?.discount ?? '0,00'} onChange={event => setSelected(current => ({ ...current, [title.id]: { checked: current[title.id]?.checked ?? false, discount: event.target.value } }))} /></td></tr>)}</tbody></table>}</div>
      </div>
      <div className="dialog-footer"><span className="field-hint">A simulação nunca altera saldos.</span><div className="button-row"><button type="button" className="secondary" onClick={runSimulation} disabled={simulate.isPending || create.isPending}>Simular</button>{simulated?.proposedAmount && <span className="muted">Líquido simulado: {money(simulated.proposedAmount)}</span>}<button type="button" className="secondary" onClick={onClose} disabled={create.isPending}>Cancelar</button><button className="primary" disabled={create.isPending || simulate.isPending}>{create.isPending ? 'Salvando…' : 'Criar acordo'}</button></div></div></fieldset>
    </form>
  </Dialog>;
}

function AgreementConfirmDialog({ agreement, onClose }: { agreement: Agreement; onClose(): void }) {
  const scope = useScopeKey();
  const { currency } = useWorkspace();
  const client = useQueryClient();
  const accounts = useQuery({ queryKey: [...scope, 'lookup', 'contas'], queryFn: ({ signal }) => get<Page<CatalogEntry>>('/api/v1/contas?pageSize=100', signal) });
  const people = useQuery({ queryKey: [...scope, 'lookup', 'pessoas'], queryFn: ({ signal }) => get<Page<CatalogEntry>>('/api/v1/pessoas?pageSize=100', signal) });
  const form = useForm<Values>({ defaultValues: { accountId: '', payerId: '', paymentDate: '', amount: agreement.proposedAmount, currency, method: '' } });
  const [idempotencyKey] = useState(() => (globalThis.crypto?.randomUUID?.() ?? `agreement-${Date.now()}`));
  const [error, setError] = useState('');
  const confirm = useMutation({
    mutationFn: (body: unknown) => request(`/api/v1/acordos/${agreement.id}/confirmar`, { method: 'PUT', body }),
    onSuccess: async () => { await client.invalidateQueries({ queryKey: scope }); onClose(); },
    onError: cause => setError(cause instanceof Error ? cause.message : 'Não foi possível confirmar o acordo.'),
  });
  const submit = (event: FormEvent) => {
    event.preventDefault(); form.clearErrors(); setError('');
    const parsed = paymentSchema.safeParse(form.getValues());
    if (!parsed.success) { for (const issue of parsed.error.issues) form.setError(String(issue.path[0]), { message: issue.message }); return; }
    confirm.mutate({ accountId: parsed.data.accountId, payerId: parsed.data.payerId, paymentDate: parsed.data.paymentDate, currency: parsed.data.currency, method: parsed.data.method, idempotencyKey, version: agreement.version });
  };
  return <Dialog title="Confirmar acordo" onClose={onClose}>
    <p className="context-line">Descontos e alocações serão aplicados juntos, sem criar um novo título a pagar.</p>
    {error && <p className="error-banner" role="alert">{error}</p>}
    <form onSubmit={submit}>
      <fieldset disabled={confirm.isPending}><div className="form-grid">
        <div className="field"><label htmlFor="agreement-account">Conta</label><select id="agreement-account" {...form.register('accountId')}><option value="">Selecione…</option>{accounts.data?.items.map(account => <option key={account.id} value={account.id}>{account.name || `Conta ${account.number ?? ''}`}</option>)}</select>{form.formState.errors.accountId && <small className="field-error">{String(form.formState.errors.accountId.message)}</small>}</div>
        <div className="field"><label htmlFor="agreement-payer">Pagador</label><select id="agreement-payer" {...form.register('payerId')}><option value="">Selecione…</option>{people.data?.items.map(person => <option key={person.id} value={person.id}>{person.name}</option>)}</select>{form.formState.errors.payerId && <small className="field-error">{String(form.formState.errors.payerId.message)}</small>}</div>
        <Field form={form} name="paymentDate" label="Data do pagamento" type="date" required />
        <SelectField form={form} name="currency" label="Moeda" options={currencies.map(value => ({ value, label: value }))} />
        <Field form={form} name="method" label="Forma de pagamento" required maxLength={50} />
        <div className="field"><span className="field-hint">Valor líquido</span><strong className="amount">{money(agreement.proposedAmount)}</strong></div>
      </div>
      <div className="dialog-footer"><span className="field-hint">* Campo obrigatório</span><div className="button-row"><button type="button" className="secondary" onClick={onClose} disabled={confirm.isPending}>Cancelar</button><button className="primary" disabled={confirm.isPending}>{confirm.isPending ? 'Confirmando…' : 'Confirmar acordo'}</button></div></div></fieldset>
    </form>
  </Dialog>;
}
