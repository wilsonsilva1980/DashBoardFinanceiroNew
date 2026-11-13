import { useState } from 'react';
import { Link, Navigate, NavLink, Route, Routes } from 'react-router-dom';
import { useIsMutating, useQuery } from '@tanstack/react-query';
import { useAuth, useScopeKey } from './app/Auth';
import { WorkspaceContext } from './app/Workspace';
import { get } from './shared/api/client';
import type { FinancialSpace } from './shared/api/contracts';
import { Loading, QueryError } from './shared/ui/QueryStatus';
import { AuthPage } from './features/auth/AuthPage';
import { SetupPage } from './features/tenancy/SetupPage';
import { CatalogPage } from './features/cadastros/CatalogPage';
import { PositionsPage } from './features/accounts/PositionsPage';
import { ObligationsPage } from './features/obligations/ObligationsPage';
import { TitlesPage } from './features/obligations/TitlesPage';
import { PaymentsPage } from './features/settlements/PaymentsPage';
import { AgreementsPage } from './features/settlements/AgreementsPage';
import { RecurrencesPage } from './features/recurrences/RecurrencesPage';
import { FaturasPage } from './features/invoices/FaturasPage';
import { TravelPage } from './features/travel/TravelPage';
import { DashboardPage } from './features/dashboard/DashboardPage';
import { AuditPage } from './features/audit/AuditPage';

const navigation = [
  { path: '/dashboard', label: 'Dashboard', icon: 'M3 13h8V3H3v10Zm10 8h8V11h-8v10ZM3 21h8v-6H3v6Zm10-10h8V3h-8v8Z' },
  { path: '/contas', label: 'Contas', icon: 'M3 8h18M5 8v11m5-11v11m4-11v11m5-11v11M3 21h18M12 3l9 5H3l9-5Z' },
  { path: '/cartoes', label: 'Cartões', icon: 'M3 8h18M5 4h14a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2Zm2 11h4' },
  { path: '/posicoes', label: 'Posições de conta', icon: 'M4 20h16M6 16v-4m6 4V5m6 11V9' },
  { path: '/obrigacoes', label: 'Obrigações', icon: 'M6 3h12v18l-3-2-3 2-3-2-3 2V3Zm3 5h6m-6 4h6m-6 4h3' },
  { path: '/pagamentos', label: 'Pagamentos', icon: 'M12 2v20M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6' },
  { path: '/acordos', label: 'Acordos', icon: 'M7 4h10a1 1 0 0 1 1 1v16l-6-3-6 3V5a1 1 0 0 1 1-1Zm3 5h4m-4 4h4' },
  { path: '/recorrencias', label: 'Recorrências', icon: 'M17 4h1a4 4 0 0 1 4 4v13a4 4 0 0 01-4 4H7a4 4 0 0 01-4-4V8a4 4 0 0 01 4-4h1' },
  { path: '/faturas', label: 'Faturas', icon: 'M5 3h14l2 5v14a1 1 0 0 01-1 1H6a1 1 0 0 01-1-1V8l2-5z' },
  { path: '/cadastros/pessoas', label: 'Pessoas', icon: 'M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2m18 0v-2a4 4 0 0 0-3-3.87M13 3.13a4 4 0 0 1 0 7.75M13 7a4 4 0 1 1-8 0 4 4 0 0 1 8 0Z' },
  { path: '/cadastros/categorias', label: 'Categorias', icon: 'M3 3h7v7H3V3Zm11 0h7v7h-7V3ZM3 14h7v7H3v-7Zm11 0h7v7h-7v-7Z' },
  { path: '/cadastros/instituicoes', label: 'Instituições', icon: 'M4 21h16M6 21V5l6-3 6 3v16M9 8h1m4 0h1m-6 4h1m4 0h1m-6 4h1m4 0h1' },
  { path: '/cadastros/bandeiras', label: 'Bandeiras', icon: 'M5 21V3m0 1c5-4 9 4 14 0v11c-5 4-9-4-14 0' },
  { path: '/auditoria', label: 'Auditoria', permission: 'auditoria.visualizar', icon: 'M4 5h16M4 12h16M4 19h10' },
];
function Icon({ path }: { path: string }) { return <svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" strokeWidth="1.65" strokeLinecap="round" strokeLinejoin="round" aria-hidden><path d={path} /></svg>; }
export function App() {
  const auth = useAuth();
  if (auth.loading) return <main className="centered-state"><Loading /></main>;
  if (auth.blocked) return <main className="centered-state"><QueryError message={auth.notice} retry={() => void auth.refresh()} /></main>;
  if (!auth.user) return <AuthPage />;
  if (!auth.user.tenantId || !auth.user.spaceId) return <SetupPage />;
  if (auth.busy) return <main className="centered-state"><Loading /></main>;
  return <FinancialShell key={`${auth.user.id}:${auth.user.tenantId}:${auth.user.spaceId}`} />;
}
function FinancialShell() {
  const auth = useAuth();
  const scope = useScopeKey();
  const mutations = useIsMutating();
  const [navigationOpen, setNavigationOpen] = useState(false);
  const spaces = useQuery({ queryKey: ['session', auth.user!.id, 'spaces'], queryFn: ({ signal }) => get<FinancialSpace[]>('/api/v1/context/spaces', signal) });
  const permissions = useQuery({ queryKey: [...scope, 'permissions'], queryFn: ({ signal }) => get<string[]>('/api/v1/context/permissions', signal) });
  const active = spaces.data?.find(space => space.spaceId === auth.user!.spaceId && space.tenantId === auth.user!.tenantId);
  if (spaces.isPending) return <main className="centered-state"><Loading /></main>;
  if (spaces.isError) return <main className="centered-state"><QueryError message={spaces.error.message} retry={() => void spaces.refetch()} /></main>;
  if (!active) return <main className="centered-state"><QueryError message="Confirme novamente seu espaço financeiro para continuar." retry={() => void auth.refresh()} /></main>;
  if (permissions.isPending) return <main className="centered-state"><Loading /></main>;
  const canEdit = permissions.data?.includes('cadastros.editar') ?? false;
  const canAudit = permissions.data?.includes('auditoria.visualizar') ?? false;
  const visibleNavigation = navigation.filter(item => !item.permission || permissions.data?.includes(item.permission));
  return <WorkspaceContext.Provider value={{ currency: active.currency, spaceName: active.name, canEdit, canAudit }}>
    <div className="app-layout">
      {navigationOpen && <button className="nav-backdrop" aria-label="Fechar navegação" onClick={() => setNavigationOpen(false)} />}
      <aside id="main-navigation" className={`sidebar ${navigationOpen ? 'is-open' : ''}`}><Link to="/dashboard" className="brand"><span className="brand-mark">F</span> Financeiro<span className="brand-dot">.</span></Link><p className="sidebar-caption">Tudo no seu lugar</p>
        <nav aria-label="Navegação principal"><p className="nav-heading">Gestão</p>{visibleNavigation.map((item, index) => <div key={item.path}>{item.path.startsWith('/cadastros/') && (index === 0 || !visibleNavigation[index - 1].path.startsWith('/cadastros/')) && <p className="nav-heading">Cadastros</p>}<NavLink to={item.path} end={item.path !== '/contas' && item.path !== '/obrigacoes'} onClick={() => setNavigationOpen(false)}><Icon path={item.icon} /><span>{item.label}</span></NavLink></div>)}</nav>
        <div className="sidebar-note"><span className="sidebar-note-icon" aria-hidden>✓</span><strong>Um espaço de cada vez.</strong><p>Seus registros organizados no contexto que você escolheu.</p></div>
        <div className="sidebar-footer"><span className="status-dot" /> Entrada manual</div>
      </aside>
      <div className="main-column"><header className="topbar"><div className="header-context"><button className="icon-button menu-toggle" aria-controls="main-navigation" aria-expanded={navigationOpen} aria-label="Abrir navegação" onClick={() => setNavigationOpen(!navigationOpen)}>☰</button><label className="workspace-switch"><span>Espaço financeiro</span><select aria-label="Espaço financeiro" value={`${active.tenantId}/${active.spaceId}`} disabled={mutations > 0} onChange={event => { const selected = spaces.data.find(space => `${space.tenantId}/${space.spaceId}` === event.target.value); if (selected) void auth.select(selected.tenantId, selected.spaceId).catch(() => {}); }}>{spaces.data.map(space => <option key={space.spaceId} value={`${space.tenantId}/${space.spaceId}`}>{space.name} · {space.currency}</option>)}</select></label></div><div className="profile-area"><span className="avatar" aria-hidden>{auth.user!.displayName.slice(0, 1).toUpperCase()}</span><div className="profile-name"><strong>{auth.user!.displayName}</strong><span>Minha conta</span></div><button className="secondary compact" onClick={() => void auth.logout()} disabled={mutations > 0}>Sair</button></div></header>
        <main className="main-content">{auth.notice && <p className="notice" role="status">{auth.notice}</p>}{permissions.isError && <div className="error-banner" role="alert">Não foi possível consultar suas permissões. <button className="text-button" onClick={() => void permissions.refetch()}>Tentar novamente</button></div>}
          <Routes><Route path="/dashboard" element={<DashboardPage key="dashboard" />} /><Route path="/auditoria" element={canAudit ? <AuditPage key="audit" /> : <Navigate to="/dashboard" replace />} /><Route path="/contas" element={<CatalogPage kind="contas" key="contas" />} /><Route path="/cartoes" element={<CatalogPage kind="cartoes" key="cartoes" />} /><Route path="/faturas" element={<FaturasPage key="faturas" />} /><Route path="/viagens" element={<TravelPage key="viagens" />} /><Route path="/posicoes" element={<PositionsPage key="positions" />} /><Route path="/contas/:accountId/posicoes" element={<PositionsPage key="account-positions" />} /><Route path="/obrigacoes" element={<ObligationsPage key="obligations" />} /><Route path="/obrigacoes/:obligationId/titulos" element={<TitlesPage key="titles" />} /><Route path="/recorrencias" element={<RecurrencesPage key="recorrencias" />} /><Route path="/pagamentos" element={<PaymentsPage key="payments" />} /><Route path="/acordos" element={<AgreementsPage key="agreements" />} /><Route path="/cadastros/pessoas" element={<CatalogPage kind="pessoas" key="pessoas" />} /><Route path="/cadastros/categorias" element={<CatalogPage kind="categorias" key="categorias" />} /><Route path="/cadastros/instituicoes" element={<CatalogPage kind="instituicoes" key="instituicoes" />} /><Route path="/cadastros/bandeiras" element={<CatalogPage kind="bandeiras" key="bandeiras" />} /><Route path="*" element={<Navigate to="/dashboard" replace />} /></Routes>
          <footer className="content-footer"><span>Financeiro · Seus registros, com clareza.</span><span>{active.currency} · {active.name}</span></footer>
        </main>
      </div>
    </div>
  </WorkspaceContext.Provider>;
}
