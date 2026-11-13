import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useScopeKey } from '../../app/Auth';
import { get } from '../../shared/api/client';
import type { AuditEntry, Page } from '../../shared/api/contracts';
import { Dialog } from '../../shared/ui/Dialog';
import { Loading, Pagination, QueryError } from '../../shared/ui/QueryStatus';

type Filters = { from: string; until: string; action: string; entity: string; correlationId: string };
const emptyFilters: Filters = { from: '', until: '', action: '', entity: '', correlationId: '' };

export function AuditPage() {
  const scope = useScopeKey();
  const [filters, setFilters] = useState<Filters>(emptyFilters);
  const [applied, setApplied] = useState<Filters>(emptyFilters);
  const [page, setPage] = useState(1);
  const [selected, setSelected] = useState<AuditEntry | null>(null);
  const events = useQuery({ queryKey: [...scope, 'audit-events'], queryFn: ({ signal }) => get<Record<string, string>>('/api/v1/auditoria/eventos', signal) });
  const queryString = new URLSearchParams({ page: String(page), pageSize: '20' });
  for (const [key, value] of Object.entries(applied)) if (value) queryString.set(key, value);
  const audit = useQuery({ queryKey: [...scope, 'audit', page, applied], queryFn: ({ signal }) => get<Page<AuditEntry>>(`/api/v1/auditoria?${queryString}`, signal) });
  const submit = (event: React.FormEvent) => { event.preventDefault(); setPage(1); setApplied(filters); };
  const clear = () => { setFilters(emptyFilters); setApplied(emptyFilters); setPage(1); };
  return <section className="workspace-page"><div className="page-heading"><div><p className="eyebrow">Rastreabilidade do espaço</p><h1>Auditoria</h1><p className="muted">Consulte as operações relevantes registradas no espaço financeiro ativo.</p></div><span className="read-only">Somente leitura</span></div>
    <div className="data-card"><div className="audit-toolbar"><form onSubmit={submit}><div><label htmlFor="audit-from">De</label><input id="audit-from" type="date" value={filters.from} onChange={event => setFilters(current => ({ ...current, from: event.target.value }))} /></div><div><label htmlFor="audit-until">Até</label><input id="audit-until" type="date" value={filters.until} onChange={event => setFilters(current => ({ ...current, until: event.target.value }))} /></div><div><label htmlFor="audit-action">Ação</label><select id="audit-action" value={filters.action} onChange={event => setFilters(current => ({ ...current, action: event.target.value }))}><option value="">Todas</option>{Object.entries(events.data ?? {}).map(([key, label]) => <option key={key} value={key}>{label}</option>)}</select></div><div><label htmlFor="audit-entity">Entidade</label><input id="audit-entity" value={filters.entity} placeholder="Payment" onChange={event => setFilters(current => ({ ...current, entity: event.target.value }))} /></div><div><label htmlFor="audit-correlation">Correlação</label><input id="audit-correlation" value={filters.correlationId} onChange={event => setFilters(current => ({ ...current, correlationId: event.target.value }))} /></div><div className="audit-actions"><button className="primary compact">Filtrar</button><button type="button" className="secondary compact" onClick={clear}>Limpar</button></div></form></div>
      {audit.isPending ? <Loading /> : audit.isError ? <QueryError retry={() => void audit.refetch()} message={audit.error.message} /> : !audit.data.items.length ? <div className="empty-state"><span className="empty-symbol" aria-hidden>⌁</span><h2>Nenhum evento encontrado</h2><p>Ajuste os filtros ou aguarde novas operações no espaço.</p></div> : <div className="table-scroll" tabIndex={0} aria-label="Lista de auditoria"><table><thead><tr><th>Data e hora</th><th>Ação</th><th>Entidade</th><th>Usuário</th><th>Correlação</th><th className="actions-heading">Detalhes</th></tr></thead><tbody>{audit.data.items.map(row => <tr key={row.id}><td>{new Date(row.instant).toLocaleString('pt-BR')}</td><td><strong>{row.actionLabel}</strong><small className="cell-subtitle literal">{row.action}</small></td><td>{row.entity}<small className="cell-subtitle literal">{row.entityId}</small></td><td>{row.userDisplayName ?? 'Sistema'}</td><td className="literal">{row.correlationId}</td><td className="actions-heading"><button className="text-button" onClick={() => setSelected(row)}>Ver detalhes</button></td></tr>)}</tbody></table></div>}
      {audit.data && <Pagination page={page} total={audit.data.total} pageSize={20} onPage={setPage} />}
    </div>{selected && <AuditDetail entry={selected} onClose={() => setSelected(null)} />}</section>;
}

function AuditDetail({ entry, onClose }: { entry: AuditEntry; onClose(): void }) {
  return <Dialog title={entry.actionLabel} onClose={onClose}><div className="audit-detail"><p><strong>Ação:</strong> <span className="literal">{entry.action}</span></p><p><strong>Entidade:</strong> {entry.entity} · <span className="literal">{entry.entityId}</span></p><p><strong>Data:</strong> {new Date(entry.instant).toLocaleString('pt-BR')}</p><p><strong>Usuário:</strong> {entry.userDisplayName ?? 'Sistema'}</p><p><strong>Correlação:</strong> <span className="literal">{entry.correlationId}</span></p><pre>{JSON.stringify(entry.changes, null, 2)}</pre></div><div className="dialog-footer"><span className="field-hint">Registro somente leitura</span><button type="button" className="secondary" onClick={onClose}>Fechar</button></div></Dialog>;
}
