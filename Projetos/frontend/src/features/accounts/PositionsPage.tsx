import { useState } from 'react';
import { useParams, useSearchParams } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { useScopeKey } from '../../app/Auth';
import { useWorkspace } from '../../app/Workspace';
import { get } from '../../shared/api/client';
import type { AccountPosition, Page } from '../../shared/api/contracts';
import { money, date } from '../../shared/forms/format';
import { Loading, Pagination, QueryError } from '../../shared/ui/QueryStatus';
import { Field, FinancialForm, ReferenceField } from '../cadastros/CatalogForm';
import { positionSchema } from '../cadastros/catalogConfig';

export function PositionsPage() {
  const scope = useScopeKey();
  const { canEdit } = useWorkspace();
  const { accountId } = useParams();
  const [params, setParams] = useSearchParams();
  const parsed = Number(params.get('page') || 1);
  const page = Number.isSafeInteger(parsed) && parsed > 0 && parsed <= 1000000 ? parsed : 1;
  const [creating, setCreating] = useState(false);
  const query = useQuery({ queryKey: [...scope, 'positions', accountId, page], queryFn: ({ signal }) => get<Page<AccountPosition>>(`/api/v1/posicoes-conta?${new URLSearchParams({ page: String(page), pageSize: '20', ...(accountId ? { accountId } : {}) })}`, signal) });
  return <section className="workspace-page"><div className="page-heading"><div><p className="eyebrow">Uma observação, na data certa</p><h1>Posições de conta</h1><p className="muted">Saldo, poupança e cheque especial registrados separadamente.</p></div>{canEdit && <button className="primary" onClick={() => setCreating(true)}>＋ Registrar posição</button>}</div>
    <div className="notice position-note"><strong>Posições não são movimentações.</strong> Os valores mostram uma observação da conta e não alteram pagamentos ou despesas.</div>
    <div className="data-card">{query.isPending ? <Loading /> : query.isError ? <QueryError retry={() => void query.refetch()} message={query.error.message} /> : !query.data.items.length ? <div className="empty-state"><span className="empty-symbol" aria-hidden>◷</span><h2>Nenhuma posição registrada</h2><p>Informe a data e os valores conhecidos para acompanhar a sua conta.</p>{canEdit && <button className="secondary" onClick={() => setCreating(true)}>Registrar posição</button>}</div> : <div className="table-scroll" tabIndex={0} aria-label="Histórico de posições"><table><thead><tr><th>Data</th><th>Conta</th><th>Saldo</th><th>Poupança</th><th>Cheque utilizado</th><th>Cheque disponível</th></tr></thead><tbody>{query.data.items.map(row => <tr key={row.id}><td>{date(row.positionDate)}</td><td><strong>{row.institutionName}</strong><small className="cell-subtitle literal">{row.accountNumber ?? 'Número não informado'}</small></td><td className="amount">{money(row.balance, row.currency)}</td><td className="amount">{money(row.savingsBalance, row.currency)}</td><td className="amount">{money(row.overdraftUsed, row.currency)}</td><td className={`amount ${row.availableOverdraft?.startsWith('-') ? 'negative' : ''}`}>{money(row.availableOverdraft, row.currency)}<small className="cell-subtitle">Limite atual: {money(row.configuredOverdraftLimit, row.currency)}</small></td></tr>)}</tbody></table></div>}
      {query.data && <Pagination page={page} total={query.data.total} pageSize={20} onPage={next => setParams({ page: String(next) })} />}
    </div>
    {creating && <FinancialForm title="Registrar posição" endpoint="/api/v1/posicoes-conta" defaults={{ accountId: accountId ?? '', positionDate: '', balance: '', savingsBalance: '', overdraftUsed: '' }} schema={positionSchema} onClose={() => setCreating(false)}>{form => <><ReferenceField form={form} name="accountId" label="Conta" path="contas" /><Field form={form} name="positionDate" label="Data da posição" type="date" required /><Field form={form} name="balance" label="Saldo da conta" hint="Pode ser negativo. Vazio significa não informado." /><Field form={form} name="savingsBalance" label="Saldo da poupança" /><Field form={form} name="overdraftUsed" label="Cheque especial utilizado" hint="Deixe vazio quando não houver uma informação confirmada." /></>}</FinancialForm>}
  </section>;
}
