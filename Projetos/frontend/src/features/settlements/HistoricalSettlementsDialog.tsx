import { useState, type FormEvent } from 'react';
import { useForm } from 'react-hook-form';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useScopeKey } from '../../app/Auth';
import { get, request } from '../../shared/api/client';
import type { HistoricalSettlement, PayableTitle } from '../../shared/api/contracts';
import { date, money } from '../../shared/forms/format';
import { Dialog } from '../../shared/ui/Dialog';
import { Field } from '../cadastros/CatalogForm';
import { historicalSettlementSchema } from './settlementConfig';

type Values = Record<string, string | boolean>;

export function HistoricalSettlementsDialog({ title, onClose }: { title: PayableTitle; onClose(): void }) {
  const scope = useScopeKey();
  const client = useQueryClient();
  const route = `/api/v1/titulos/${title.id}/liquidacoes-historicas`;
  const form = useForm<Values>({ defaultValues: { amount: '', effectiveDate: '', justification: '', previousReference: '', sourceStatus: '' } });
  const [error, setError] = useState('');
  const [reasons, setReasons] = useState<Record<string, string>>({});
  const settlements = useQuery({ queryKey: [...scope, 'historical-settlements', title.id], queryFn: ({ signal }) => get<HistoricalSettlement[]>(route, signal) });
  const refresh = async () => { await client.invalidateQueries({ queryKey: scope }); };
  const create = useMutation({
    mutationFn: (body: unknown) => request(route, { method: 'POST', body }),
    onSuccess: async () => { form.reset(); await refresh(); },
    onError: cause => setError(cause instanceof Error ? cause.message : 'Não foi possível registrar a baixa.'),
  });
  const revoke = useMutation({
    mutationFn: (settlement: HistoricalSettlement) => request(`${route}/${settlement.id}/revogar`, { method: 'POST', body: { reason: reasons[settlement.id], version: settlement.version } }),
    onSuccess: async () => { setError(''); await refresh(); },
    onError: cause => setError(cause instanceof Error ? cause.message : 'Não foi possível revogar a baixa.'),
  });
  const submit = (event: FormEvent) => {
    event.preventDefault(); form.clearErrors(); setError('');
    const parsed = historicalSettlementSchema.safeParse(form.getValues());
    if (!parsed.success) { for (const issue of parsed.error.issues) form.setError(String(issue.path[0]), { message: issue.message }); return; }
    create.mutate(parsed.data);
  };
  return <Dialog title={`Baixas históricas · ${title.occurrenceKey}`} onClose={onClose}>
    <p className="context-line">Uma baixa sem data reduz o saldo, mas não entra no fluxo de caixa por período.</p>
    {error && <p className="error-banner" role="alert">{error}</p>}
    <div className="data-card">{settlements.isPending ? <p className="muted">Carregando…</p> : !settlements.data?.length ? <p className="muted">Nenhuma baixa registrada.</p> : <ul className="settlement-list">{settlements.data.map(settlement => <li key={settlement.id}><div><strong>{money(settlement.amount)}</strong><span className="cell-subtitle">{settlement.effectiveDate ? date(settlement.effectiveDate) : 'Sem data'} · {settlement.justification}</span>{settlement.revokedAt && <span className="cell-subtitle">Revogada: {settlement.revocationReason}</span>}</div>{!settlement.revokedAt && <div className="row-actions"><input aria-label={`Motivo da revogação de ${settlement.id}`} placeholder="Motivo" maxLength={500} value={reasons[settlement.id] ?? ''} onChange={event => setReasons(current => ({ ...current, [settlement.id]: event.target.value }))} /><button type="button" className="text-button" disabled={revoke.isPending} onClick={() => { if (!reasons[settlement.id]?.trim()) { setError('Informe o motivo da revogação.'); return; } revoke.mutate(settlement); }}>Revogar</button></div>}</li>)}</ul>}</div>
    <form onSubmit={submit}>
      <fieldset disabled={create.isPending}><div className="form-grid">
        <Field form={form} name="amount" label="Valor da baixa" required hint="Até duas casas decimais." />
        <Field form={form} name="effectiveDate" label="Data efetiva" type="date" hint="Deixe vazio quando a data é desconhecida." />
        <Field form={form} name="justification" label="Justificativa" required maxLength={1000} />
        <Field form={form} name="previousReference" label="Referência anterior" maxLength={200} />
        <Field form={form} name="sourceStatus" label="Status de origem" maxLength={100} />
      </div>
      <div className="dialog-footer"><span className="field-hint">* Campo obrigatório</span><div className="button-row"><button type="button" className="secondary" onClick={onClose} disabled={create.isPending}>Fechar</button><button className="primary" disabled={create.isPending}>{create.isPending ? 'Registrando…' : 'Registrar baixa'}</button></div></div></fieldset>
    </form>
  </Dialog>;
}
