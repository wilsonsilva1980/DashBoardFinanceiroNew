import { useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { useScopeKey } from '../../app/Auth';
import { useWorkspace } from '../../app/Workspace';
import { get } from '../../shared/api/client';
import type { CatalogEntry, Obligation, Page } from '../../shared/api/contracts';
import { Loading, Pagination, QueryError } from '../../shared/ui/QueryStatus';
import { Field, FinancialForm, ReferenceField, SelectField } from '../cadastros/CatalogForm';
import { currencies } from '../cadastros/catalogConfig';
import { obligationKinds, obligationSchema } from './obligationConfig';

export function ObligationsPage() {
  const scope = useScopeKey();
  const { canEdit } = useWorkspace();
  const [params, setParams] = useSearchParams();
  const parsed = Number(params.get('page') || 1);
  const page = Number.isSafeInteger(parsed) && parsed > 0 && parsed <= 1000000 ? parsed : 1;
  const [editing, setEditing] = useState<Obligation | 'new' | null>(null);
  const query = useQuery({ queryKey: [...scope, 'obligations', page], queryFn: ({ signal }) => get<Page<Obligation>>(`/api/v1/obrigacoes?${new URLSearchParams({ page: String(page), pageSize: '20' })}`, signal) });
  const people = useQuery({ queryKey: [...scope, 'lookup', 'pessoas'], queryFn: ({ signal }) => get<Page<CatalogEntry>>('/api/v1/pessoas?pageSize=100', signal) });
  const categories = useQuery({ queryKey: [...scope, 'lookup', 'categorias'], queryFn: ({ signal }) => get<Page<CatalogEntry>>('/api/v1/categorias?pageSize=100', signal) });
  const name = (rows: Page<CatalogEntry> | undefined, id: string | null) => rows?.items.find(row => row.id === id)?.name ?? 'Não informado';
  return <section className="workspace-page"><div className="page-heading"><div><p className="eyebrow">Seus compromissos, com clareza</p><h1>Obrigações</h1><p className="muted">Origens dos títulos a pagar, com parcelas e responsáveis.</p></div>{canEdit ? <button className="primary" onClick={() => setEditing('new')}><span aria-hidden>＋</span> Adicionar obrigação</button> : <span className="read-only">Somente leitura</span>}</div>
    <div className="data-card">{query.isPending ? <Loading /> : query.isError ? <QueryError retry={() => void query.refetch()} message={query.error.message} /> : !query.data.items.length ? <div className="empty-state"><span className="empty-symbol" aria-hidden>◇</span><h2>Nenhuma obrigação cadastrada</h2><p>Registre a origem de um compromisso para começar a lançar títulos.</p>{canEdit && <button className="secondary" onClick={() => setEditing('new')}>Adicionar obrigação</button>}</div> : <div className="table-scroll" tabIndex={0} aria-label="Lista de obrigações"><table><thead><tr><th>Descrição</th><th>Tipo</th><th>Parcelas</th><th>Responsável</th><th>Categoria</th><th>Situação</th><th className="actions-heading">Ações</th></tr></thead><tbody>{query.data.items.map(row => <tr key={row.id}><td><strong>{row.description}</strong>{row.creditor && <small className="cell-subtitle">{row.creditor}</small>}</td><td>{obligationKinds.find(kind => kind.value === row.kind)?.label ?? row.kind}</td><td>{row.installmentCount ?? 'Não informado'}</td><td>{name(people.data, row.responsibleId)}</td><td>{name(categories.data, row.categoryId)}</td><td><span className={`status-badge ${row.active ? 'active' : 'inactive'}`}><span />{row.active ? 'Ativa' : 'Inativa'}</span></td><td><div className="row-actions"><Link to={`/obrigacoes/${row.id}/titulos`} className="text-button">Títulos</Link>{canEdit && <button className="text-button" onClick={() => setEditing(row)} aria-label={`Editar obrigação ${row.description}`}>Editar</button>}</div></td></tr>)}</tbody></table></div>}
      {query.data && <Pagination page={page} total={query.data.total} pageSize={20} onPage={next => setParams({ page: String(next) })} />}
    </div>
    {editing && <ObligationForm row={editing === 'new' ? undefined : editing} onClose={() => setEditing(null)} />}
  </section>;
}

function ObligationForm({ row, onClose }: { row?: Obligation; onClose(): void }) {
  const { currency } = useWorkspace();
  return <FinancialForm title={`${row ? 'Editar' : 'Adicionar'} obrigação`} schema={obligationSchema} endpoint={`/api/v1/obrigacoes${row ? '/' + row.id : ''}`} method={row ? 'PUT' : 'POST'} version={row?.version} onClose={onClose} defaults={{
    description: row?.description ?? '', kind: row?.kind ?? 'Single', creditor: row?.creditor ?? '', purchaseDate: row?.purchaseDate ?? '',
    purchaseDateContext: row?.purchaseDateContext ?? '', dueDay: row?.dueDay?.toString() ?? '', installmentCount: row?.installmentCount?.toString() ?? '',
    currency: row?.currency ?? currency, responsibleId: row?.responsibleId ?? '', categoryId: row?.categoryId ?? '', active: row?.active ?? true,
  }}>{form => <>
    <Field form={form} name="description" label="Descrição" required maxLength={1000} />
    <SelectField form={form} name="kind" label="Tipo" options={obligationKinds.map(kind => ({ value: kind.value, label: kind.label }))} />
    <Field form={form} name="creditor" label="Credor" maxLength={200} />
    <Field form={form} name="dueDay" label="Dia de vencimento" hint="Deixe vazio quando não souber." />
    <Field form={form} name="installmentCount" label="Quantidade de parcelas" hint="Informe apenas quando a obrigação for parcelada." />
    <ReferenceField form={form} name="responsibleId" label="Responsável" path="pessoas" optional />
    <ReferenceField form={form} name="categoryId" label="Categoria" path="categorias" optional />
    <SelectField form={form} name="currency" label="Moeda" options={currencies.map(value => ({ value, label: value }))} />
    <Field form={form} name="purchaseDate" label="Data da compra" type="date" hint="Deixe vazio para manter a data desconhecida." />
    <Field form={form} name="purchaseDateContext" label="Contexto da data" maxLength={200} hint="Descreva o que se sabe quando a data exata é desconhecida." />
    <label className="checkbox-field"><input type="checkbox" {...form.register('active')} /> Obrigação ativa</label>
  </>}</FinancialForm>;
}
