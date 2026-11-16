import { useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useScopeKey } from '../../app/Auth';
import { useWorkspace } from '../../app/Workspace';
import { get, request } from '../../shared/api/client';
import type { CatalogEntry, Page } from '../../shared/api/contracts';
import { money } from '../../shared/forms/format';
import { Dialog } from '../../shared/ui/Dialog';
import { Loading, Pagination, QueryError } from '../../shared/ui/QueryStatus';
import { CatalogForm } from './CatalogForm';
import { catalogConfig, type CatalogKind } from './catalogConfig';

export function CatalogPage({ kind }: { kind: CatalogKind }) {
  const config = catalogConfig[kind];
  const scope = useScopeKey();
  const { canEdit } = useWorkspace();
  const client = useQueryClient();
  const [params, setParams] = useSearchParams();
  const parsed = Number(params.get('page') || 1);
  const page = Number.isSafeInteger(parsed) && parsed > 0 && parsed <= 1000000 ? parsed : 1;
  const search = (params.get('q') || '').slice(0, 200);
  const inactive = params.get('inactive') !== 'false';
  const [editing, setEditing] = useState<CatalogEntry | 'new' | null>(null);
  const [deleting, setDeleting] = useState<CatalogEntry | null>(null);
  const [error, setError] = useState('');
  const query = useQuery({ queryKey: [...scope, 'catalog', kind, page, search, inactive], queryFn: ({ signal }) => get<Page<CatalogEntry>>(`/api/v1/${kind}?${new URLSearchParams({ page: String(page), pageSize: '20', search, includeInactive: String(inactive) })}`, signal) });
  const remove = useMutation({ mutationFn: (row: CatalogEntry) => request(`/api/v1/${kind}/${row.id}?version=${row.version}`, { method: 'DELETE' }), onSuccess: async () => { await client.invalidateQueries({ queryKey: scope }); setDeleting(null); setError(''); }, onError: cause => setError(cause instanceof Error ? cause.message : 'Não foi possível excluir.') });
  const updateFilter = (changes: Record<string, string>) => { const next = new URLSearchParams(params); for (const [key, value] of Object.entries(changes)) next.set(key, value); setParams(next); };
  const label = (row: CatalogEntry) => kind === 'contas' ? row.number || 'Conta sem número' : row.name || 'Não informado';
  return <section className="workspace-page"><div className="page-heading"><div><p className="eyebrow">Seu espaço, organizado</p><h1>{config.title}</h1><p className="muted">{config.description}</p></div>{canEdit ? <button className="primary" onClick={() => setEditing('new')}><span aria-hidden>＋</span> {config.action}</button> : <span className="read-only">Somente leitura</span>}</div>
    <div className="context-summary"><div className="summary-icon" aria-hidden>{kind === 'contas' ? '▤' : kind === 'cartoes' ? '▰' : '◫'}</div><div><span>{config.title} neste espaço</span><strong>{query.data?.total ?? '—'}</strong></div><p>{kind === 'contas' ? 'Consulte os saldos datados em Posições de conta.' : kind === 'bandeiras' ? 'Alterações aqui se aplicam somente a este espaço.' : 'Cadastros ativos e histórico, sem misturar espaços.'}</p></div>
    <div className="data-card"><div className="table-toolbar"><form onSubmit={event => { event.preventDefault(); const values = new FormData(event.currentTarget); updateFilter({ q: String(values.get('q') || ''), page: '1' }); }}><label className="search-box"><span aria-hidden>⌕</span><input key={search} name="q" type="search" defaultValue={search} maxLength={200} aria-label={`Buscar ${config.title.toLowerCase()}`} placeholder={kind === 'contas' ? 'Buscar por agência ou número' : `Buscar ${config.title.toLowerCase()}`} /></label><button className="secondary" type="submit">Buscar</button></form><label className="inline-check" htmlFor="show-inactive"><input id="show-inactive" name="inactive" autoComplete="off" type="checkbox" checked={inactive} onChange={event => updateFilter({ inactive: String(event.target.checked), page: '1' })} /> Mostrar inativos</label></div>
      {query.isPending ? <Loading /> : query.isError ? <QueryError retry={() => void query.refetch()} message={query.error.message} /> : !query.data.items.length ? <div className="empty-state"><span className="empty-symbol" aria-hidden>◇</span><h2>{search ? 'Nenhum resultado encontrado' : config.empty}</h2><p>{search ? 'Experimente buscar por outro termo.' : 'Adicione seu primeiro cadastro para começar a organizar este espaço.'}</p>{canEdit && !search && <button className="secondary" onClick={() => setEditing('new')}>{config.action}</button>}</div> : <>
        <div className="table-scroll" tabIndex={0} aria-label={`Lista de ${config.title.toLowerCase()}`}><table><thead><tr><th>{kind === 'contas' ? 'Conta' : 'Nome'}</th>{kind === 'contas' ? <><th>Agência</th><th>Moeda</th><th>Limite de cheque especial</th></> : kind === 'cartoes' ? <><th>Vencimento</th><th>Limite informado</th><th>Disponível</th></> : kind === 'categorias' ? <th>Natureza</th> : kind === 'instituicoes' ? <th>Código</th> : null}<th>Situação</th><th className="actions-heading">Ações</th></tr></thead><tbody>{query.data.items.map(row => <tr key={row.id}><td><strong>{label(row)}</strong>{kind === 'contas' && <small className="cell-subtitle">{row.type || 'Tipo não informado'}{row.checkDigit ? ` · Dígito ${row.checkDigit}` : ''}</small>}</td>{kind === 'contas' ? <><td className="literal">{row.agency ?? 'Não informado'}</td><td>{row.currency}</td><td className="amount">{money(row.overdraftLimit, row.currency)}</td></> : kind === 'cartoes' ? <><td>{row.dueDay ? `Dia ${row.dueDay}` : 'Não informado'}</td><td className="amount">{money(row.creditLimit)}</td><td><span className="unknown-badge">{row.availableCredit === null ? 'Não confirmado' : money(row.availableCredit)}</span></td></> : kind === 'categorias' ? <td>{{ Fixed: 'Fixa', Variable: 'Variável', Unclassified: 'Não classificada' }[row.nature || 'Unclassified'] || 'Não classificada'}</td> : kind === 'instituicoes' ? <td className="literal">{row.code ?? 'Não informado'}</td> : null}<td><span className={`status-badge ${row.active ? 'active' : 'inactive'}`}><span />{row.active ? 'Ativo' : 'Inativo'}</span></td><td><div className="row-actions">{kind === 'contas' && <Link to={`/contas/${row.id}/posicoes`} className="text-button">Posições</Link>}{canEdit && <><button className="text-button" onClick={() => setEditing(row)} aria-label={`Editar ${config.singular} ${label(row)}`}>Editar</button><button className="text-button destructive" onClick={() => { setError(''); setDeleting(row); }} aria-label={`Excluir ${config.singular} ${label(row)}`}>Excluir</button></>}</div></td></tr>)}</tbody></table></div>
      </>}
      {query.data && <Pagination page={page} total={query.data.total} pageSize={20} onPage={next => updateFilter({ page: String(next) })} />}
    </div>
    {editing && <CatalogForm kind={kind} row={editing === 'new' ? undefined : editing} onClose={() => setEditing(null)} />}
    {deleting && <Dialog title={`Excluir ${config.singular}?`} onClose={() => { if (!remove.isPending) setDeleting(null); }}><p>Você está excluindo <strong>{label(deleting)}</strong>. Cadastros utilizados em registros financeiros devem ser desativados para manter o histórico.</p>{error && <p role="alert" className="error-banner">{error}</p>}<div className="dialog-footer"><button className="secondary" disabled={remove.isPending} onClick={() => setDeleting(null)}>Cancelar</button><button className="danger" disabled={remove.isPending} onClick={() => remove.mutate(deleting)}>{remove.isPending ? 'Excluindo…' : 'Excluir cadastro'}</button></div></Dialog>}
  </section>;
}
