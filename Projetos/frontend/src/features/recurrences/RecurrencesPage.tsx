import { useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useScopeKey } from '../../app/Auth';
import { useWorkspace } from '../../app/Workspace';
import { get, request } from '../../shared/api/client';
import type { Obligation, Page, Recurrence, RecurrenceGenerationResult } from '../../shared/api/contracts';
import { recurrenceGenerationSchema, recurrenceSchema } from './recurrenceConfig';
import { Dialog } from '../../shared/ui/Dialog';
import { Loading, Pagination, QueryError } from '../../shared/ui/QueryStatus';
import { FinancialForm, SelectField, Field } from '../cadastros/CatalogForm';

type Values = Record<string, string | boolean>;

export function RecurrencesPage() {
  const scope = useScopeKey();
  const { canEdit } = useWorkspace();
  const client = useQueryClient();
  const [params, setParams] = useSearchParams();
  const parsed = Number(params.get('page') || 1);
  const page = Number.isSafeInteger(parsed) && parsed > 0 && parsed <= 1000000 ? parsed : 1;
  const [creating, setCreating] = useState(false);
  const [generating, setGenerating] = useState<Recurrence | null>(null);
  const [error, setError] = useState('');
  const recurrences = useQuery({ queryKey: [...scope, 'recorrencias', page], queryFn: ({ signal }) => get<Page<Recurrence>>(`/api/v1/recorrencias?${new URLSearchParams({ page: String(page), pageSize: '20' })}`, signal) });
  const obligations = useQuery({ queryKey: [...scope, 'lookup', 'obrigacoes'], queryFn: ({ signal }) => get<Page<Obligation>>('/api/v1/obrigacoes?pageSize=100', signal) });
  const generate = useMutation({
    mutationFn: (input: { from: string; to: string }) => request<RecurrenceGenerationResult>(`/api/v1/recorrencias/${generating!.id}/gerar`, { method: 'POST', body: input }),
    onSuccess: async () => { setError(''); setGenerating(null); await client.invalidateQueries({ queryKey: scope }); },
    onError: cause => { setError(cause instanceof Error ? cause.message : 'Não foi possível gerar as ocorrências.'); },
  });
  const obligationName = (id: string) => obligations.data?.items.find(row => row.id === id)?.description ?? 'Não informado';
  return <section className="workspace-page"><div className="page-heading"><div><p className="eyebrow">Obrigações recorrentes</p><h1>Recorrências</h1><p className="muted">Definições de recorrência, geração idempotente de ocorrências e preservação de valores históricos.</p></div>{canEdit ? <button className="primary" onClick={() => setCreating(true)}><span aria-hidden>＋</span> Nova recorrência</button> : <span className="read-only">Somente leitura</span>}</div>
    {error && <p className="error-banner" role="alert">{error}</p>}
    <div className="data-card">{recurrences.isPending ? <Loading /> : recurrences.isError ? <QueryError retry={() => void recurrences.refetch()} message={recurrences.error.message} /> : !recurrences.data.items.length ? <div className="empty-state"><span className="empty-symbol" aria-hidden>◇</span><h2>Nenhuma recorrência cadastrada</h2><p>Crie uma obrigação recorrente para gerar ocorrências sem duplicar.</p>{canEdit && <button className="secondary" onClick={() => setCreating(true)}>Nova recorrência</button>}</div> : <div className="table-scroll" tabIndex={0} aria-label="Lista de recorrências"><table><thead><tr><th>Obrigação</th><th>Ancora</th><th>Dia nominal</th><th>Vigência</th><th>Situação</th><th className="actions-heading">Ações</th></tr></thead><tbody>{recurrences.data.items.map(row => <tr key={row.id}><td><strong>{obligationName(row.obligationId)}</strong></td><td>{row.anchorDate}</td><td>{String(row.nominalDay)}</td><td>{row.endDate ?? 'Sem fim'}</td><td><span className={`status-badge ${row.active ? 'active' : 'inactive'}`}><span />{row.active ? 'Ativa' : 'Inativa'}</span></td><td><div className="row-actions">{canEdit && <button className="text-button" onClick={() => setGenerating(row)} aria-label={`Gerar ocorrências da recorrência ${row.obligationId}`}>Gerar ocorrências</button>}</div></td></tr>)}</tbody></table></div>}
      {recurrences.data && <Pagination page={page} total={recurrences.data.total} pageSize={20} onPage={next => setParams({ page: String(next) })} />}
    </div>
    {creating && <RecurrenceForm onClose={() => setCreating(false)} />}
    {generating && <GenerateDialog recurrence={generating} obligationLabel={obligationName(generating.obligationId)} onClose={() => setGenerating(null)} />}
  </section>;
}

function RecurrenceForm({ onClose }: { onClose(): void }) {
  const scope = useScopeKey();
  const obligations = useQuery({ queryKey: [...scope, 'lookup', 'obrigacoes'], queryFn: ({ signal }) => get<Page<Obligation>>('/api/v1/obrigacoes?pageSize=100', signal) });
  return <FinancialForm title="Nova recorrência" defaults={defaultValues} schema={recurrenceSchema} endpoint="/api/v1/recorrencias" method="POST" onClose={onClose}>{form => <>
    <SelectField form={form} name="obligationId" label="Obrigação" options={(obligations.data?.items ?? []).map(obligation => ({ value: obligation.id, label: obligation.description }))} />
    <Field form={form} name="anchorDate" label="Data de ancoragem" type="date" required />
    <Field form={form} name="nominalDay" label="Dia nominal" required hint="O dia da recorrência; meses sem esse dia usam o último dia válido." />
    <Field form={form} name="endDate" label="Data final" type="date" hint="Deixe vazio para recorrência sem fim." />
    <label className="checkbox-field"><input type="checkbox" {...form.register('active')} /> Recorrência ativa</label>
  </>}</FinancialForm>;
}

function GenerateDialog({ recurrence, obligationLabel, onClose }: { recurrence: Recurrence; obligationLabel?: string; onClose(): void }) {
  const [from, setFrom] = useState(recurrence.anchorDate);
  const [to, setTo] = useState(recurrence.anchorDate);
  const [error, setError] = useState('');
  const generate = useMutation({
    mutationFn: (input: { from: string; to: string }) => request<RecurrenceGenerationResult>(`/api/v1/recorrencias/${recurrence.id}/gerar`, { method: 'POST', body: input }),
    onSuccess: async () => { setError(''); onClose(); },
    onError: cause => { setError(cause instanceof Error ? cause.message : 'Não foi possível gerar as ocorrências.'); },
  });
  return <Dialog title="Gerar ocorrências" onClose={onClose}>
    <p className="context-line">Recorrência da obrigação <strong>{obligationLabel ?? recurrence.obligationId}</strong>. A geração só cria faltantes e respeita a vigência informada.</p>
    {error && <p className="error-banner" role="alert">{error}</p>}
    <form onSubmit={event => { event.preventDefault(); const parsed = recurrenceGenerationSchema.safeParse({ from, to }); if (!parsed.success) { setError(parsed.error.issues[0]?.message ?? 'Informe datas válidas.'); return; } generate.mutate({ from: parsed.data.from, to: parsed.data.to }); }}>
      <fieldset disabled={generate.isPending}><div className="form-grid">
        <div className="field"><label htmlFor="recurrence-from">Data inicial</label><input id="recurrence-from" type="date" value={from} onChange={event => setFrom(event.target.value)} /></div>
        <div className="field"><label htmlFor="recurrence-to">Data final</label><input id="recurrence-to" type="date" value={to} onChange={event => setTo(event.target.value)} /></div>
      </div>
        <div className="dialog-footer"><div className="button-row"><button type="button" className="secondary" onClick={onClose} disabled={generate.isPending}>Cancelar</button><button className="primary" disabled={generate.isPending}>{generate.isPending ? 'Gerando…' : 'Gerar'}</button></div></div>
      </fieldset>
    </form>
  </Dialog>;
}

const defaultValues: Values = { obligationId: '', anchorDate: '', nominalDay: '1', endDate: '', active: true };
