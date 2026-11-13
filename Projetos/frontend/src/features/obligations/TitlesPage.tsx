import { useState } from 'react';
import { useParams, useSearchParams } from 'react-router-dom';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useScopeKey } from '../../app/Auth';
import { useWorkspace } from '../../app/Workspace';
import { get } from '../../shared/api/client';
import type { CatalogEntry, Page, PayableTitle, PlanningEntry } from '../../shared/api/contracts';
import { date, money } from '../../shared/forms/format';
import { Loading, Pagination, QueryError } from '../../shared/ui/QueryStatus';
import { Field, FinancialForm, ReferenceField, SelectField } from '../cadastros/CatalogForm';
import { planningSchema, settlementLabels, stateLabels, titleSchema, titleStates } from './obligationConfig';
import { HistoricalSettlementsDialog } from '../settlements/HistoricalSettlementsDialog';

export function TitlesPage() {
  const scope = useScopeKey();
  const { canEdit } = useWorkspace();
  const { obligationId = '' } = useParams();
  const [params, setParams] = useSearchParams();
  const parsed = Number(params.get('page') || 1);
  const page = Number.isSafeInteger(parsed) && parsed > 0 && parsed <= 1000000 ? parsed : 1;
  const includeForecast = params.get('forecast') === 'true';
  const [editing, setEditing] = useState<PayableTitle | 'new' | null>(null);
  const [planning, setPlanning] = useState<PayableTitle | null>(null);
  const [settlements, setSettlements] = useState<PayableTitle | null>(null);
  const query = useQuery({ queryKey: [...scope, 'titles', obligationId, page, includeForecast], queryFn: ({ signal }) => get<Page<PayableTitle>>(`/api/v1/titulos?${new URLSearchParams({ obligationId, page: String(page), pageSize: '20', includeForecast: String(includeForecast) })}`, signal) });
  return <section className="workspace-page"><div className="page-heading"><div><p className="eyebrow">Cada parcela, no seu lugar</p><h1>Títulos a pagar</h1><p className="muted">Valor original, saldo derivado e situação de pagamento separados do estado do título.</p></div>{canEdit ? <button className="primary" onClick={() => setEditing('new')}><span aria-hidden>＋</span> Adicionar título</button> : <span className="read-only">Somente leitura</span>}</div>
    <div className="table-toolbar"><label className="inline-check"><input type="checkbox" checked={includeForecast} onChange={event => setParams({ ...(page > 1 ? { page: String(page) } : {}), forecast: String(event.target.checked) })} /> Incluir previsões nos totais</label></div>
    <div className="data-card">{query.isPending ? <Loading /> : query.isError ? <QueryError retry={() => void query.refetch()} message={query.error.message} /> : !query.data.items.length ? <div className="empty-state"><span className="empty-symbol" aria-hidden>◇</span><h2>Nenhum título neste recorte</h2><p>Títulos de previsão ficam fora dos totais padrão. Marque a opção acima para incluí-los.</p>{canEdit && <button className="secondary" onClick={() => setEditing('new')}>Adicionar título</button>}</div> : <div className="table-scroll" tabIndex={0} aria-label="Lista de títulos a pagar"><table><thead><tr><th>Ocorrência</th><th>Período</th><th>Vencimento</th><th>Parcela</th><th>Valor original</th><th>Saldo</th><th>Situação</th><th>Estado</th><th className="actions-heading">Ações</th></tr></thead><tbody>{query.data.items.map(row => <tr key={row.id}><td><strong>{row.occurrenceKey}</strong>{row.excessCredit !== '0.00' && <small className="cell-subtitle">Crédito excedente: {money(row.excessCredit)}</small>}</td><td>{date(row.period)}</td><td>{row.dueDate ? date(row.dueDate) : 'Não informado'}</td><td>{row.installmentNumber ?? 'Não informado'}</td><td className="amount">{money(row.originalAmount)}</td><td className="amount">{money(row.balance)}</td><td><span className={`status-badge ${row.settlementStatus === 'Paid' ? 'active' : row.settlementStatus === 'Overdue' ? 'inactive' : ''}`}><span />{settlementLabels[row.settlementStatus] ?? row.settlementStatus}</span></td><td>{stateLabels[row.state] ?? row.state}</td><td><div className="row-actions">{canEdit && <><button className="text-button" onClick={() => setEditing(row)} aria-label={`Editar título ${row.occurrenceKey}`}>Editar</button><button className="text-button" onClick={() => setPlanning(row)} aria-label={`Planejar título ${row.occurrenceKey}`}>Planejar</button><button className="text-button" onClick={() => setSettlements(row)} aria-label={`Baixas históricas do título ${row.occurrenceKey}`}>Baixas</button></>}</div></td></tr>)}</tbody></table></div>}
      {query.data && <Pagination page={page} total={query.data.total} pageSize={20} onPage={next => setParams({ page: String(next), ...(includeForecast ? { forecast: 'true' } : {}) })} />}
    </div>
    {editing && <TitleForm obligationId={obligationId} row={editing === 'new' ? undefined : editing} onClose={() => setEditing(null)} />}
    {planning && <PlanningForm title={planning} onClose={() => setPlanning(null)} />}
    {settlements && <HistoricalSettlementsDialog title={settlements} onClose={() => setSettlements(null)} />}
  </section>;
}

function TitleForm({ obligationId, row, onClose }: { obligationId: string; row?: PayableTitle; onClose(): void }) {
  return <FinancialForm title={`${row ? 'Editar' : 'Adicionar'} título`} schema={titleSchema} endpoint={`/api/v1/titulos${row ? '/' + row.id : ''}`} method={row ? 'PUT' : 'POST'} version={row?.version} onClose={onClose} defaults={{
    obligationId: row?.obligationId ?? obligationId, occurrenceKey: row?.occurrenceKey ?? '', period: row?.period ?? '', dueDate: row?.dueDate ?? '',
    installmentNumber: row?.installmentNumber?.toString() ?? '', originalAmount: row?.originalAmount ?? '', state: row?.state ?? 'Draft',
    responsibleId: row?.responsibleId ?? '', cardId: row?.cardId ?? '',
  }}>{form => <>
    <Field form={form} name="occurrenceKey" label="Chave de ocorrência" required maxLength={200} hint="Identifica a ocorrência dentro da obrigação." />
    <Field form={form} name="period" label="Período" type="date" required hint="Sempre o primeiro dia do mês." />
    <Field form={form} name="dueDate" label="Vencimento" type="date" hint="Deixe vazio quando a data for desconhecida." />
    <Field form={form} name="installmentNumber" label="Número da parcela" hint="Deixe vazio quando não for parcelado." />
    <Field form={form} name="originalAmount" label="Valor original" required hint="Informe até duas casas decimais." />
    <SelectField form={form} name="state" label="Estado" options={titleStates.map(state => ({ value: state.value, label: state.label }))} />
    <ReferenceField form={form} name="responsibleId" label="Responsável" path="pessoas" optional />
    <ReferenceField form={form} name="cardId" label="Cartão" path="cartoes" optional />
  </>}</FinancialForm>;
}

function PlanningForm({ title, onClose }: { title: PayableTitle; onClose(): void }) {
  const scope = useScopeKey();
  const client = useQueryClient();
  const entries = useQuery({ queryKey: [...scope, 'planning', title.id], queryFn: ({ signal }) => get<PlanningEntry[]>(`/api/v1/titulos/${title.id}/planejamentos`, signal) });
  const people = useQuery({ queryKey: [...scope, 'lookup', 'pessoas'], queryFn: ({ signal }) => get<Page<CatalogEntry>>('/api/v1/pessoas?pageSize=100', signal) });
  const personName = (id: string) => people.data?.items.find(person => person.id === id)?.name ?? 'Responsável';
  return <FinancialForm title={`Planejar ${title.occurrenceKey}`} schema={planningSchema} endpoint={`/api/v1/titulos/${title.id}/planejamentos`} method="PUT" onClose={onClose} defaults={{ personId: '', amount: '' }}>{form => <>
    {entries.data?.length ? <div className="field field-wide"><span className="field-hint">Planejamento atual</span><ul>{entries.data.map(entry => <li key={entry.id}>{personName(entry.personId)}: {money(entry.amount)}</li>)}</ul></div> : null}
    <ReferenceField form={form} name="personId" label="Responsável" path="pessoas" />
    <Field form={form} name="amount" label="Valor planejado" required hint="Informe até duas casas decimais." />
  </>}</FinancialForm>;
}
