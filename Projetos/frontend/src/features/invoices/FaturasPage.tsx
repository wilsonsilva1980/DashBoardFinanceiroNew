import { useState, type FormEvent } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { useScopeKey } from '../../app/Auth';
import { useWorkspace } from '../../app/Workspace';
import { get, request } from '../../shared/api/client';
import type { CardInvoice, CatalogEntry, Page, PayableTitle } from '../../shared/api/contracts';
import { money } from '../../shared/forms/format';
import { invoicePurchaseSchema, invoiceSchema, invoiceModes } from './invoiceConfig';
import { Dialog } from '../../shared/ui/Dialog';
import { Field, FinancialForm, SelectField } from '../cadastros/CatalogForm';
import { Loading, QueryError } from '../../shared/ui/QueryStatus';

type Values = Record<string, string | boolean>;

export function FaturasPage() {
  const scope = useScopeKey();
  const { canEdit } = useWorkspace();
  const client = useQueryClient();
  const [cardId, setCardId] = useState('');
  const [creating, setCreating] = useState(false);
  const [purchaseFor, setPurchaseFor] = useState<CardInvoice | null>(null);
  const [error, setError] = useState('');
  const cards = useQuery({ queryKey: [...scope, 'lookup', 'cartoes'], queryFn: ({ signal }) => get<Page<CatalogEntry>>(`/api/v1/cartoes?pageSize=100`, signal) });
  const invoices = useQuery({ queryKey: [...scope, 'faturas', cardId], enabled: !!cardId, queryFn: ({ signal }) => get<Page<CardInvoice>>(`/api/v1/faturas?${new URLSearchParams({ cardId, page: '1', pageSize: '100' })}`, signal) });
  const savePurchase = useMutation({
    mutationFn: (body: unknown) => request(`/api/v1/faturas/${purchaseFor!.id}/itens`, { method: 'POST', body }),
    onSuccess: async () => { setError(''); setPurchaseFor(null); await client.invalidateQueries({ queryKey: [...scope, 'faturas'] }); await client.invalidateQueries({ queryKey: [...scope, 'titles'] }); },
    onError: cause => { setError(cause instanceof Error ? cause.message : 'Não foi possível registrar a compra analítica.'); },
  });
  const cardOptions = (cards.data?.items ?? []).map(card => ({ value: card.id, label: card.name || 'Cartão' }));
  const cardName = (id: string) => cards.data?.items.find(row => row.id === id)?.name ?? 'Cartão';
  const content = !cardId ? (
    <div className="empty-state"><span className="empty-symbol" aria-hidden>◇</span><h2>Selecione um cartão</h2><p>Escolha um cartão para listar faturas e compras analíticas.</p></div>
  ) : invoices.isPending ? <Loading /> : invoices.isError ? <QueryError retry={() => void invoices.refetch()} message={invoices.error.message} /> : !invoices.data.items.length ? (
    <div className="empty-state"><span className="empty-symbol" aria-hidden>◇</span><h2>Nenhuma fatura registrada</h2><p>Registre uma fatura para transformar compras em fonte única de pagamento.</p>{canEdit && <button className="secondary" onClick={() => setCreating(true)}>Nova fatura</button>}</div>
  ) : (
    <div className="table-scroll" tabIndex={0} aria-label="Lista de faturas"><table><thead><tr><th>Cartão</th><th>Período</th><th>Fonte</th><th>Valor</th><th>Ações</th></tr></thead><tbody>{invoices.data.items.map(row => <tr key={row.id}><td>{cardName(row.cardId)}</td><td>{row.period}</td><td>{row.mode}</td><td className="amount">{money(row.reportedAmount)}</td><td><div className="row-actions">{canEdit && <button className="text-button" onClick={() => setPurchaseFor(row)} aria-label={`Adicionar compra à fatura do período ${row.period}`}>Adicionar compra</button>}</div></td></tr>)}</tbody></table></div>
  );
  return (
    <section className="workspace-page">
      <div className="page-heading"><div><p className="eyebrow">Faturas e compras analíticas</p><h1>Faturas</h1><p className="muted">A fatura é a fonte única de pagamento; compras vinculadas permanecem analíticas.</p></div>{canEdit ? <button className="primary" onClick={() => setCreating(true)} disabled={!cardId}>Nova fatura</button> : <span className="read-only">Somente leitura</span>}</div>
      {error && <p className="error-banner" role="alert">{error}</p>}
      <div className="data-card">
        <div className="table-toolbar"><label>Cartão
          <select value={cardId} onChange={event => setCardId(event.target.value)} aria-label="Cartão">
            <option value="">Selecione um cartão</option>
            {cardOptions.map(option => <option key={option.value} value={option.value}>{option.label}</option>)}
          </select>
        </label></div>
        {content}
      </div>
      {canEdit && creating && <CreateInvoiceForm cardId={cardId} cardOptions={cardOptions} onClose={() => setCreating(false)} />}
      {purchaseFor && <PurchaseDialog invoice={purchaseFor} onClose={() => setPurchaseFor(null)} />}
    </section>
  );
}

function CreateInvoiceForm({ cardId, cardOptions, onClose }: { cardId: string; cardOptions: { value: string; label: string }[]; onClose(): void }) {
  const scope = useScopeKey();
  const titles = useQuery({ queryKey: [...scope, 'titles', cardId], enabled: !!cardId, queryFn: ({ signal }) => get<Page<PayableTitle>>(`/api/v1/titulos?cardId=${cardId}&pageSize=100`, signal) });
  const defaults: Values = { cardId, titleId: '', period: '', mode: 'Consolidado', reportedAmount: '', notes: '' };
  return <FinancialForm title="Nova fatura" defaults={defaults} schema={invoiceSchema} endpoint="/api/v1/faturas" method="POST" onClose={onClose}>{form => (
    <>
      <SelectField form={form} name="cardId" label="Cartão" options={cardOptions} />
      <SelectField form={form} name="titleId" label="Título da fatura" options={(titles.data?.items ?? []).map(title => ({ value: title.id, label: `${title.occurrenceKey} · ${money(title.balance)}` }))} />
      <Field form={form} name="period" label="Período" type="date" required />
      <SelectField form={form} name="mode" label="Modo" options={[...invoiceModes].map(value => ({ value, label: value }))} />
      <Field form={form} name="reportedAmount" label="Valor informado" hint="Deixe vazio quando o valor ainda não foi fechado." />
      <Field form={form} name="notes" label="Observações" maxLength={200} />
    </>
  )}</FinancialForm>;
}

function PurchaseDialog({ invoice, onClose }: { invoice: CardInvoice; onClose(): void }) {
  const scope = useScopeKey();
  const client = useQueryClient();
  const [error, setError] = useState('');
  const form = useForm<Values>({ defaultValues: { description: '', amount: '', purchaseObligationId: '', installmentNumber: '', occurrenceKey: '' } });
  const save = useMutation({
    mutationFn: (body: unknown) => request(`/api/v1/faturas/${invoice.id}/itens`, { method: 'POST', body }),
    onSuccess: async () => { setError(''); onClose(); await client.invalidateQueries({ queryKey: [...scope, 'faturas'] }); await client.invalidateQueries({ queryKey: [...scope, 'titles'] }); },
    onError: cause => { setError(cause instanceof Error ? cause.message : 'Não foi possível registrar a compra analítica.'); },
  });
  const submit = (event: FormEvent) => {
    event.preventDefault();
    const parsed = invoicePurchaseSchema.safeParse(form.getValues());
    if (!parsed.success) { for (const issue of parsed.error.issues) form.setError(String(issue.path[0]), { message: issue.message }); return; }
    save.mutate({
      purchaseObligationId: parsed.data.purchaseObligationId ?? null,
      description: parsed.data.description,
      amount: parsed.data.amount ?? '0.00',
      installmentNumber: parsed.data.installmentNumber ?? null,
      occurrenceKey: parsed.data.occurrenceKey,
    });
  };
  return <Dialog title={`Compras da fatura ${invoice.period}`} onClose={onClose}>
    {error && <p className="error-banner" role="alert">{error}</p>}
    <form onSubmit={submit}>
      <fieldset disabled={save.isPending}><div className="form-grid">
        <Field form={form} name="description" label="Descrição" required maxLength={1000} />
        <Field form={form} name="amount" label="Valor" required />
        <Field form={form} name="purchaseObligationId" label="Obrigação de origem" maxLength={200} />
        <Field form={form} name="installmentNumber" label="Parcela" />
        <Field form={form} name="occurrenceKey" label="Chave de ocorrência" required maxLength={200} />
      </div>
        <div className="dialog-footer"><div className="button-row"><button type="button" className="secondary" onClick={onClose} disabled={save.isPending}>Cancelar</button><button className="primary" disabled={save.isPending}>{save.isPending ? 'Salvando…' : 'Salvar'}</button></div></div>
      </fieldset>
    </form>
  </Dialog>;
}
