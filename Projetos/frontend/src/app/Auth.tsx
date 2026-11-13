import { createContext, useContext, useEffect, useRef, useState, type ReactNode } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { ApiError, get, onUnauthorized, refreshCsrf, request, resetCsrf } from '../shared/api/client';
import type { SessionUser } from '../shared/api/contracts';
import { clearSessionCache } from './queryClient';

interface AuthState {
  user: SessionUser | null; loading: boolean; busy: boolean; blocked: boolean; notice: string;
  refresh(): Promise<void>; login(email: string, password: string, twoFactorCode?: string): Promise<void>;
  register(email: string, password: string, displayName: string): Promise<void>;
  logout(): Promise<void>; select(tenantId: string, spaceId: string): Promise<void>;
  onboard(body: { tenantName: string; spaceName: string; currency: string; requestKey: string }): Promise<void>;
}
const AuthContext = createContext<AuthState | null>(null);
export const useAuth = () => { const value = useContext(AuthContext); if (!value) throw new Error('AuthProvider ausente.'); return value; };
export function useScopeKey() {
  const { user } = useAuth();
  if (!user?.tenantId || !user.spaceId) throw new Error('Espaço não selecionado.');
  return ['finance', user.id, user.tenantId, user.spaceId] as const;
}
export function AuthProvider({ children }: { children: ReactNode }) {
  const client = useQueryClient();
  const [user, setUser] = useState<SessionUser | null>(null);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [blocked, setBlocked] = useState(false);
  const [notice, setNotice] = useState('');
  const knownUser = useRef(false);
  useEffect(() => { knownUser.current = user !== null; }, [user]);
  useEffect(() => onUnauthorized(() => {
    void client.cancelQueries(); client.clear(); setUser(null); setBlocked(false);
    if (knownUser.current) setNotice('Sua sessão terminou. Entre novamente para continuar.');
  }), [client]);
  useEffect(() => {
    const controller = new AbortController();
    get<SessionUser>('/api/v1/auth/me', controller.signal).then(setUser).catch(error => {
      if (controller.signal.aborted) return;
      if (!(error instanceof ApiError && error.status === 401)) { setBlocked(true); setNotice('Não foi possível verificar sua sessão. Verifique a conexão.'); }
    }).finally(() => { if (!controller.signal.aborted) setLoading(false); });
    return () => controller.abort();
  }, []);
  async function refresh() {
    setBusy(true);
    await clearSessionCache(client);
    try { setUser(await get<SessionUser>('/api/v1/auth/me')); setBlocked(false); setNotice(''); }
    catch (error) { if (!(error instanceof ApiError && error.status === 401)) { setBlocked(true); setNotice('Não foi possível confirmar o espaço ativo. Tente novamente.'); } }
    finally { setBusy(false); }
  }
  async function login(email: string, password: string, twoFactorCode?: string) {
    setBusy(true); setNotice('');
    try {
      await clearSessionCache(client); await refreshCsrf();
      await request('/api/v1/auth/login', { method: 'POST', body: { email, password, twoFactorCode } });
      await refreshCsrf(); setUser(await get<SessionUser>('/api/v1/auth/me')); setBlocked(false);
    } finally { setBusy(false); }
  }
  async function register(email: string, password: string, displayName: string) {
    setBusy(true);
    try {
      await refreshCsrf();
      await request('/api/v1/auth/register', { method: 'POST', body: { email, password, displayName } });
      await login(email, password);
    } finally { setBusy(false); }
  }
  async function logout() {
    setBusy(true); await clearSessionCache(client);
    try {
      await request('/api/v1/auth/logout', { method: 'POST', body: {} });
      resetCsrf(); setUser(null); setBlocked(false); setNotice('Você saiu da sua conta.');
    } catch (error) {
      if (!(error instanceof ApiError && error.status === 401)) setNotice('Não foi possível confirmar a saída. Verifique a conexão e tente sair novamente.');
    } finally { setBusy(false); }
  }
  async function transition(path: string, body: unknown) {
    setBusy(true); setNotice(''); await clearSessionCache(client);
    try {
      await request(path, { method: 'POST', body });
      setUser(await get<SessionUser>('/api/v1/auth/me')); setBlocked(false);
    } catch (error) {
      // A lost response may hide a committed selection: never resume with an assumed context.
      try { setUser(await get<SessionUser>('/api/v1/auth/me')); }
      catch (refreshError) { if (!(refreshError instanceof ApiError && refreshError.status === 401)) setBlocked(true); }
      setNotice(error instanceof Error ? error.message : 'Não foi possível confirmar a seleção.');
      throw error;
    } finally { setBusy(false); }
  }
  return <AuthContext.Provider value={{ user, loading, busy, blocked, notice, refresh, login, register, logout,
    select: (tenantId, spaceId) => transition('/api/v1/context/select', { tenantId, spaceId }),
    onboard: body => transition('/api/v1/context/onboarding', body),
  }}>{children}</AuthContext.Provider>;
}
