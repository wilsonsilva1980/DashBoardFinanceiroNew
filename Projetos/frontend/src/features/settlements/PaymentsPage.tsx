import { useState, type FormEvent } from 'react';
import { useSearchParams } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useScopeKey } from '../../app/Auth';
import { useWorkspace } from '../../app/Workspace';
import { request, get } from '../../shared/api/client';
import type { CatalogEntry, Page, PayableTitle, Payment } from '../../shared/api/contracts';
import { date, money } from '../../shared/forms/format';
import { parseMoneyInput } from '../../shared/forms/financialInput';
import { Dialog } from '../../shared/ui/Dialog';
import { Loading, Pagination, QueryError } from '../../shared/ui/QueryStatus';
import { Field, SelectField } from '../cadastros/CatalogForm';
import { currencies } from '../cadastros/catalogConfig';
import { paymentSchema, paymentStateLabels, validAllocations } from './settlementConfig';

type Values = Record<string, string | boolean>;
type Line = { key: string; titleId: string; amount: string };

export function PaymentsPage() {
  const scope = useScopeKey();
  const { canEdit } = useWorkspace();
  const client = useQueryClient();
  const [params, setParams] = useSearchParams();
  const parsed = Number(params.get('page') || 1);
  const page = Number.isSafeInteger(parsed) && parsed > 0 && parsed <= 1000000 ? parsed : 1;
  const [creating, setCreating] = useState(false);
  const [reversing, setReversing] = useState<Payment | null>(null);
  const [error, setError] = useState('');
  const payments = useQuery({ queryKey: [...scope, 'payments', page], queryFn: ({ signal }) => get<Page<Payment>>(`/api/v1/pagamentos?${new URLSearchParams({ page: String(page), pageSize: '20' })}`, signal) });
  const accounts = useQuery({ queryKey: [...scope, 'lookup', 'contas'], queryFn: ({ signal }) => get<Page<CatalogEntry>>('/api/v1/contas?pageSize=100', signal) });
  const people = useQuery({ queryKey: [...scope, 'lookup', 'pessoas'], queryFn: ({ signal }) => get<Page<CatalogEntry>>('/api/v1/pessoas?pageSize=100', signal) });
  const confirm = useMutation({
    mutationFn: (payment: Payment) => request(`/api/v1/pagamentos/${payment.id}/confirmar`, { method: 'PUT', body: { version: payment.version } }),
    onSuccess: async () => { setError(''); await client.invalidateQueries({ queryKey: scope }); },
    onError: cause => { setError(cause instanceof Error ? cause.message : 'Não foi possível confirmar o pagamento.'); void client.invalidateQueries({ queryKey: scope }); },
  });
  const name = (rows: Page<CatalogEntry> | undefined, id: string) => rows?.items.find(row => row.id === id)?.name ?? 'Conta';
  return <section className="workspace-page"><div className="page-heading"><div><p className="eyebrow">Cada baixa, uma única vez</p><h1>Pagamentos</h1><p className="muted">Rascunhos, confirmações idempotentes e reversões que preservam o histórico.</p></div>{canEdit ? <button className="primary" onClick={() => setCreating(true)}><span aria-hidden>＋</span> Novo pagamento</button> : <span className="read-only">Somente leitura</span>}</div>
    {error && <p className="error-banner" role="alert">{error}</p>}
    <div className="data-card">{payments.isPending ? <Loading /> : payments.isError ? <QueryError retry={() => void payments.refetch()} message={payments.error.message} /> : !payments.data.items.length ? <div className="empty-state"><span className="empty-symbol" aria-hidden>◇</span><h2>Nenhum pagamento registrado</h2><p>Crie um rascunho, aloque os títulos e confirme para liquidar.</p>{canEdit && <button className="secondary" onClick={() => setCreating(true)}>Novo pagamento</button>}</div> : <div className="table-scroll" tabIndex={0} aria-label="Lista de pagamentos"><table><thead><tr><th>Data</th><th>Conta</th><th>Pagador</th><th>Forma</th><th>Valor</th><th>Alocações</th><th>Estado</th><th className="actions-heading">Ações</th></tr></thead><tbody>{payments.data.items.map(row => <tr key={row.id}><td>{date(row.paymentDate)}</td><td>{name(accounts.data, row.accountId)}</td><td>{name(people.data, row.payerId)}</td><td>{row.method}</td><td className="amount">{money(row.amount, row.currency)}</td><td>{row.allocations.length}</td><td><span className={`status-badge ${row.state === 'Confirmed' ? 'active' : row.state === 'Reversed' ? 'inactive' : ''}`}><span />{paymentStateLabels[row.state] ?? row.state}</span>{row.reversalReason && <small className="cell-subtitle">{row.reversalReason}</small>}</td><td><div className="row-actions">{canEdit && row.state === 'Draft' && <button className="text-button" onClick={() => confirm.mutate(row)} aria-label={`Confirmar pagamento de ${money(row.amount, row.currency)}`}>Confirmar</button>}{canEdit && row.state === 'Confirmed' && <button className="text-button" onClick={() => setReversing(row)} aria-label={`Reverter pagamento de ${money(row.amount, row.currency)}`}>Reverter</button>}</div></td></tr>)}</tbody></table></div>}
      {payments.data && <Pagination page={page} total={payments.data.total} pageSize={20} onPage={next => setParams({ page: String(next) })} />}
    </div>
    {creating && <PaymentDialog onClose={() => setCreating(false)} />}
    {reversing && <ReverseDialog payment={reversing} onClose={() => setReversing(null)} />}
  </section>;
}

function PaymentDialog({ onClose }: { onClose(): void }) {
  const scope = useScopeKey();
  const { currency } = useWorkspace();
  const client = useQueryClient();
  const accounts = useQuery({ queryKey: [...scope, 'lookup', 'contas'], queryFn: ({ signal }) => get<Page<CatalogEntry>>('/api/v1/contas?pageSize=100', signal) });
  const people = useQuery({ queryKey: [...scope, 'lookup', 'pessoas'], queryFn: ({ signal }) => get<Page<CatalogEntry>>('/api/v1/pessoas?pageSize=100', signal) });
  const titles = useQuery({ queryKey: [...scope, 'lookup', 'titulos'], queryFn: ({ signal }) => get<Page<PayableTitle>>('/api/v1/titulos?pageSize=100', signal) });
  const form = useForm<Values>({ defaultValues: { accountId: '', payerId: '', paymentDate: '', amount: '', currency, method: '' } });
  const [idempotencyKey] = useState(() => (globalThis.crypto?.randomUUID?.() ?? `payment-${Date.now()}`));
  const [lines, setLines] = useState<Line[]>([{ key: idempotencyKey, titleId: '', amount: '' }]);
  const [error, setError] = useState('');
  const save = useMutation({
    mutationFn: (body: unknown) => request('/api/v1/pagamentos', { method: 'POST', body }),
    onSuccess: async () => { await client.invalidateQueries({ queryKey: scope }); onClose(); },
    onError: cause => setError(cause instanceof Error ? cause.message : 'Não foi possível criar o pagamento.'),
  });
  const update = (key: string, patch: Partial<Line>) => setLines(current => current.map(line => line.key === key ? { ...line, ...patch } : line));
  const submit = (event: FormEvent) => {
    event.preventDefault(); form.clearErrors(); setError('');
    const parsed = paymentSchema.safeParse(form.getValues());
    if (!parsed.success) { for (const issue of parsed.error.issues) form.setError(String(issue.path[0]), { message: issue.message }); return; }
    const invalid = validAllocations(lines.map(line => line.titleId), lines.map(line => line.amount), parsed.data.amount!);
    if (invalid) { setError(invalid); return; }
    save.mutate({ ...parsed.data, idempotencyKey, allocations: lines.map(line => ({ titleId: line.titleId, amount: parseMoneyInput(line.amount) })) });
  };
  const currencySymbol = String(form.watch('currency') || currency);
  return <Dialog title="Novo pagamento" onClose={onClose}>
    {error && <p className="error-banner" role="alert">{error}</p>}
    <form onSubmit={submit}>
      <fieldset disabled={save.isPending}><div className="form-grid">
        <div className="field"><label htmlFor="payment-account">Conta</label><select id="payment-account" {...form.register('accountId')}><option value="">Selecione…</option>{accounts.data?.items.map(account => <option key={account.id} value={account.id}>{account.name || `Conta ${account.number ?? ''}`}</option>)}</select>{form.formState.errors.accountId && <small className="field-error">{String(form.formState.errors.accountId.message)}</small>}</div>
        <div className="field"><label htmlFor="payment-payer">Pagador</label><select id="payment-payer" {...form.register('payerId')}><option value="">Selecione…</option>{people.data?.items.map(person => <option key={person.id} value={person.id}>{person.name}</option>)}</select>{form.formState.errors.payerId && <small className="field-error">{String(form.formState.errors.payerId.message)}</small>}</div>
        <Field form={form} name="paymentDate" label="Data do pagamento" type="date" required />
        <Field form={form} name="amount" label="Valor do pagamento" required hint="Até duas casas decimais, sem separador de milhar." />
        <SelectField form={form} name="currency" label="Moeda" options={currencies.map(value => ({ value, label: value }))} />
        <Field form={form} name="method" label="Forma de pagamento" required maxLength={50} />
        <div className="field field-wide"><span className="field-hint">Alocações por título</span>{lines.map(line => <div className="allocation-row" key={line.key}><select aria-label="Título da alocação" value={line.titleId} onChange={event => { const title = titles.data?.items.find(item => item.id === event.target.value); update(line.key, { titleId: event.target.value, amount: title ? title.balance.replace('.', ',') : line.amount }); }}><option value="">Selecione o título…</option>{titles.data?.items.map(title => <option key={title.id} value={title.id}>{title.occurrenceKey} · saldo {money(title.balance, currencySymbol)}</option>)}</select><input aria-label="Valor da alocação" inputMode="decimal" value={line.amount} onChange={event => update(line.key, { amount: event.target.value })} /><button type="button" className="text-button" onClick={() => setLines(current => current.length > 1 ? current.filter(item => item.key !== line.key) : current)}>Remover</button></div>)}<button type="button" className="secondary compact" onClick={() => setLines(current => [...current, { key: `${idempotencyKey}-${current.length}`, titleId: '', amount: '' }])}>Adicionar alocação</button></div>
      </div>
      <div className="dialog-footer"><span className="field-hint">* Campo obrigatório</span><div className="button-row"><button type="button" className="secondary" onClick={onClose} disabled={save.isPending}>Cancelar</button><button className="primary" disabled={save.isPending}>{save.isPending ? 'Salvando…' : 'Salvar'}</button></div></div></fieldset>
    </form>
  </Dialog>;
}

function ReverseDialog({ payment, onClose }: { payment: Payment; onClose(): void }) {
  const scope = useScopeKey();
  const client = useQueryClient();
  const [reason, setReason] = useState('');
  const [error, setError] = useState('');
  const reverse = useMutation({
    mutationFn: () => request(`/api/v1/pagamentos/${payment.id}/reverter`, { method: 'PUT', body: { reason, version: payment.version } }),
    onSuccess: async () => { await client.invalidateQueries({ queryKey: scope }); onClose(); },
    onError: cause => setError(cause instanceof Error ? cause.message : 'Não foi possível reverter o pagamento.'),
  });
  return <Dialog title="Reverter pagamento" onClose={onClose}>
    <p className="context-line">O pagamento original permanece no histórico; apenas seus efeitos de alocação são estornados.</p>
    {error && <p className="error-banner" role="alert">{error}</p>}
    <form onSubmit={event => { event.preventDefault(); if (!reason.trim()) { setError('Informe o motivo da reversão.'); return; } reverse.mutate(); }}>
      <fieldset disabled={reverse.isPending}><div className="form-grid"><div className="field field-wide"><label htmlFor="reverse-reason">Motivo da reversão<span className="required-mark"> *</span></label><input id="reverse-reason" value={reason} maxLength={200} onChange={event => setReason(event.target.value)} /></div></div>
        <div className="dialog-footer"><div className="button-row"><button type="button" className="secondary" onClick={onClose} disabled={reverse.isPending}>Cancelar</button><button className="danger" disabled={reverse.isPending}>Reverter</button></div></div></fieldset>
    </form>
  </Dialog>;
}

