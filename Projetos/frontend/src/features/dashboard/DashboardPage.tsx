import { useMemo, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useScopeKey } from '../../app/Auth';
import { useWorkspace } from '../../app/Workspace';
import { get } from '../../shared/api/client';
import type { DashboardAmountState, DashboardSummary } from '../../shared/api/contracts';
import { date, money } from '../../shared/forms/format';
import { Loading, QueryError } from '../../shared/ui/QueryStatus';

function monthRange(offset = 0): { from: string; until: string } {
  const now = new Date();
  const start = new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth() + offset, 1));
  const end = new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth() + offset + 1, 1));
  return { from: start.toISOString().slice(0, 10), until: end.toISOString().slice(0, 10) };
}

function AmountGrid({ label, amounts, currency }: { label: string; amounts: DashboardAmountState; currency: string }) {
  const items: [string, string][] = [
    ['Efetivo', amounts.effective], ['Previsão', amounts.forecast], ['Em aberto', amounts.open],
    ['Parcial', amounts.partial], ['Pago', amounts.paid], ['Vencido', amounts.overdue],
  ];
  return <section className="data-card"><h2>{label}</h2><div className="dashboard-stats">{items.map(([name, value]) => <div key={name} className="stat"><span>{name}</span><strong className="amount">{money(value, currency)}</strong></div>)}</div></section>;
}

export function DashboardPage() {
  const scope = useScopeKey();
  const { currency } = useWorkspace();
  const initial = useMemo(() => monthRange(0), []);
  const [from, setFrom] = useState(initial.from);
  const [until, setUntil] = useState(initial.until);
  const query = useQuery({
    queryKey: [...scope, 'dashboard', 'summary', from, until, currency],
    queryFn: ({ signal }) => get<DashboardSummary>(`/api/v1/dashboard/resumo?${new URLSearchParams({ from, until, currency })}`, signal),
    enabled: Boolean(currency),
  });

  return <section className="workspace-page">
    <div className="page-heading"><div><p className="eyebrow">Visão do espaço ativo</p><h1>Dashboard financeiro</h1><p className="muted">Totais efetivos, previsão e reconciliação separados para revisão.</p></div>
      <div className="inline-filters">
        <label>De <input type="date" value={from} onChange={e => setFrom(e.target.value)} /></label>
        <label>Até <input type="date" value={until} onChange={e => setUntil(e.target.value)} /></label>
      </div>
    </div>

    {query.isPending ? <Loading /> : query.isError ? <QueryError retry={() => void query.refetch()} message={query.error.message} /> : <>
      <AmountGrid label="Resumo do período" amounts={query.data.totals} currency={currency} />
      <div className="notice position-note"><strong>Renda não informada.</strong> A primeira versão não possui livro de receitas; valores de renda permanecem desconhecidos quando não há evidência.</div>

      {query.data.reconciliationIndicators.length > 0 && <section className="data-card" aria-label="Indicadores de reconciliação">
        <h2>Reconciliação e divergências</h2>
        <p className="muted">Itens abaixo não são somados aos totais normais de contas a pagar.</p>
        <div className="table-scroll"><table><thead><tr><th>Tipo</th><th>Escopo</th><th>Mensagem</th><th>Valor</th></tr></thead><tbody>
          {query.data.reconciliationIndicators.map((item, i) => <tr key={`${item.kind}-${i}`}><td>{item.kind}</td><td>{item.scope}</td><td>{item.message}</td><td className="amount">{item.amount ? money(item.amount, currency) : '—'}</td></tr>)}
        </tbody></table></div>
      </section>}

      <section className="data-card"><h2>Evolução mensal</h2>
        {!query.data.monthlyEvolution.length ? <p className="muted">Sem títulos com vencimento conhecido no período.</p> : <div className="table-scroll"><table><thead><tr><th>Mês</th><th>Efetivo</th><th>Previsão</th><th>Em aberto</th><th>Pago</th><th>Vencido</th></tr></thead><tbody>
          {query.data.monthlyEvolution.map(row => <tr key={row.month}><td>{row.month.slice(0, 7)}</td><td className="amount">{money(row.effective, currency)}</td><td className="amount">{money(row.forecast, currency)}</td><td className="amount">{money(row.open, currency)}</td><td className="amount">{money(row.paid, currency)}</td><td className="amount">{money(row.overdue, currency)}</td></tr>)}
        </tbody></table></div>}
      </section>

      <section className="data-card"><h2>Por categoria</h2>{!query.data.byCategory.length ? <p className="muted">Sem dados no período.</p> : <div className="table-scroll"><table><thead><tr><th>Categoria</th><th>Efetivo</th><th>Previsão</th><th>Em aberto</th><th>Parcial</th><th>Pago</th><th>Vencido</th></tr></thead><tbody>
        {query.data.byCategory.map(row => <tr key={row.categoryId ?? row.categoryName}><td>{row.categoryName}</td><td className="amount">{money(row.amounts.effective, currency)}</td><td className="amount">{money(row.amounts.forecast, currency)}</td><td className="amount">{money(row.amounts.open, currency)}</td><td className="amount">{money(row.amounts.partial, currency)}</td><td className="amount">{money(row.amounts.paid, currency)}</td><td className="amount">{money(row.amounts.overdue, currency)}</td></tr>)}
      </tbody></table></div>}</section>

      <section className="data-card"><h2>Por responsável</h2>{!query.data.byResponsible.length ? <p className="muted">Sem dados no período.</p> : <div className="table-scroll"><table><thead><tr><th>Responsável</th><th>Efetivo</th><th>Previsão</th><th>Em aberto</th><th>Parcial</th><th>Pago</th><th>Vencido</th></tr></thead><tbody>
        {query.data.byResponsible.map(row => <tr key={row.responsibleId ?? row.responsibleName}><td>{row.responsibleName}</td><td className="amount">{money(row.amounts.effective, currency)}</td><td className="amount">{money(row.amounts.forecast, currency)}</td><td className="amount">{money(row.amounts.open, currency)}</td><td className="amount">{money(row.amounts.partial, currency)}</td><td className="amount">{money(row.amounts.paid, currency)}</td><td className="amount">{money(row.amounts.overdue, currency)}</td></tr>)}
      </tbody></table></div>}</section>

      <section className="data-card"><h2>Por vencimento</h2>{!query.data.byDueDate.length ? <p className="muted">Sem vencimentos conhecidos no período.</p> : <div className="table-scroll"><table><thead><tr><th>Vencimento</th><th>Efetivo</th><th>Previsão</th><th>Em aberto</th><th>Parcial</th><th>Pago</th><th>Vencido</th></tr></thead><tbody>
        {query.data.byDueDate.map(row => <tr key={row.label}><td>{row.dueDate ? date(row.dueDate) : row.label}</td><td className="amount">{money(row.amounts.effective, currency)}</td><td className="amount">{money(row.amounts.forecast, currency)}</td><td className="amount">{money(row.amounts.open, currency)}</td><td className="amount">{money(row.amounts.partial, currency)}</td><td className="amount">{money(row.amounts.paid, currency)}</td><td className="amount">{money(row.amounts.overdue, currency)}</td></tr>)}
      </tbody></table></div>}</section>

      <section className="data-card"><h2>Posições de conta</h2><p className="muted">Observações de saldo não são renda nem movimentação de caixa.</p>
        {!query.data.positions.length ? <p className="muted">Sem posições registradas para a moeda do espaço.</p> : <div className="table-scroll"><table><thead><tr><th>Conta</th><th>Data</th><th>Saldo</th><th>Poupança</th><th>Cheque usado</th><th>Cheque disponível</th></tr></thead><tbody>
          {query.data.positions.map(row => <tr key={row.accountId}><td>{row.accountLabel}</td><td>{row.positionDate ? date(row.positionDate) : '—'}</td><td className="amount">{money(row.balance, currency)}</td><td className="amount">{money(row.savingsBalance, currency)}</td><td className="amount">{money(row.overdraftUsed, currency)}</td><td className="amount">{money(row.availableOverdraft, currency)}</td></tr>)}
        </tbody></table></div>}
      </section>
    </>}
  </section>;
}
