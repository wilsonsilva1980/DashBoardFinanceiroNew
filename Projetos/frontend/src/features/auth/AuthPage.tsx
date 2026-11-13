import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../../app/Auth';

export function AuthPage() {
  const auth = useAuth();
  const navigate = useNavigate();
  const [creating, setCreating] = useState(false);
  const [error, setError] = useState('');
  const { register, handleSubmit, reset } = useForm<{ email: string; password: string; displayName: string; twoFactorCode: string }>();
  const submit = handleSubmit(async values => {
    setError('');
    try {
      if (creating) await auth.register(values.email, values.password, values.displayName);
      else await auth.login(values.email, values.password, values.twoFactorCode || undefined);
      navigate('/contas');
    } catch (cause) { setError(cause instanceof Error ? cause.message : 'Não foi possível entrar. Tente novamente.'); }
  });
  return <div className="auth-layout">
    <section className="auth-story"><a className="brand brand-light" href="/"> <span className="brand-mark">F</span> Financeiro</a>
      <div><p className="eyebrow">Clareza para decidir</p><h1>Um espaço para cuidar<br />das suas finanças.</h1><p>Organize suas contas, acompanhe posições e mantenha seus cadastros em dia.</p><div className="story-rule" /></div>
      <p className="auth-footnote">Seus espaços. Seus registros. Uma visão organizada.</p>
    </section>
    <main className="auth-main"><div className="auth-card"><p className="eyebrow">Bem-vindo ao seu espaço</p><h2>{creating ? 'Crie sua conta' : 'Entre na sua conta'}</h2><p className="muted">{creating ? 'Comece a organizar suas informações financeiras.' : 'Continue de onde você parou.'}</p>
      {auth.notice && <p className="notice" role="status">{auth.notice}</p>}
      {error && <p className="error-banner" role="alert">{error}</p>}
      <form onSubmit={submit}>
        {creating && <label>Seu nome<input {...register('displayName')} required maxLength={120} autoComplete="name" /></label>}
        <label>Email<input {...register('email')} type="email" required autoComplete="username" placeholder="voce@exemplo.com" /></label>
        <label>Senha<input {...register('password')} type="password" required minLength={creating ? 10 : undefined} autoComplete={creating ? 'new-password' : 'current-password'} /></label>
        {creating ? <p className="field-hint">Use ao menos 10 caracteres, com letras maiúsculas e minúsculas, número e símbolo.</p> : <details className="mfa-option"><summary>Uso autenticação em duas etapas</summary><label>Código do autenticador<input {...register('twoFactorCode')} inputMode="numeric" autoComplete="one-time-code" maxLength={6} /></label></details>}
        <button className="primary full" disabled={auth.busy}>{auth.busy ? 'Aguarde…' : creating ? 'Criar conta' : 'Entrar'}</button>
      </form>
      <p className="auth-switch">{creating ? 'Já tem uma conta?' : 'É sua primeira vez?'} <button className="text-button" onClick={() => { setCreating(!creating); setError(''); reset(); }}>{creating ? 'Entrar' : 'Criar minha conta'}</button></p>
    </div></main>
  </div>;
}
