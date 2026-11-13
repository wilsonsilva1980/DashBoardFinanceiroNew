import { useRef, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useAuth } from '../../app/Auth';
import { get } from '../../shared/api/client';
import type { FinancialSpace } from '../../shared/api/contracts';
import { currencies } from '../cadastros/catalogConfig';

export function SetupPage() {
  const auth = useAuth();
  const key = useRef(crypto.randomUUID());
  const [error, setError] = useState('');
  const spaces = useQuery({ queryKey: ['session', auth.user!.id, 'spaces'], queryFn: ({ signal }) => get<FinancialSpace[]>('/api/v1/context/spaces', signal) });
  return <main className="setup-page"><div className="setup-card"><span className="brand-mark">F</span><p className="eyebrow">Comece por aqui</p><h1>Seu primeiro espaço financeiro</h1><p className="muted">Um lugar para reunir as informações da família, da vida pessoal ou de um projeto.</p>
    {error && <p className="error-banner" role="alert">{error}</p>}
    {spaces.isPending ? <p role="status">Buscando seus espaços…</p> : spaces.isError ? <div className="notice"><p>Não foi possível consultar seus espaços.</p><button onClick={() => void spaces.refetch()}>Tentar novamente</button></div> : spaces.data.length ? <div className="existing-spaces"><h2>Continue em um espaço existente</h2>{spaces.data.map(space => <button className="space-card" key={space.spaceId} onClick={() => void auth.select(space.tenantId, space.spaceId).catch(cause => setError(cause.message))}><strong>{space.name}</strong><span>{space.currency} →</span></button>)}</div> :
      <form onSubmit={event => { event.preventDefault(); const values = new FormData(event.currentTarget); setError(''); void auth.onboard({ tenantName: String(values.get('tenantName')), spaceName: String(values.get('spaceName')), currency: String(values.get('currency')), requestKey: key.current }).catch(cause => setError(cause.message)); }}>
        <label>Nome da conta cliente<input name="tenantName" required maxLength={120} placeholder="Ex.: Família Silva" /></label>
        <label>Nome do espaço<input name="spaceName" required maxLength={120} placeholder="Ex.: Pessoal" /></label>
        <label>Moeda padrão<select name="currency" defaultValue="BRL">{currencies.map(currency => <option key={currency}>{currency}</option>)}</select></label>
        <button className="primary full" disabled={auth.busy}>Criar meu espaço</button>
      </form>}
    <button className="text-button setup-exit" onClick={() => void auth.logout()}>Sair da conta</button>
  </div></main>;
}
