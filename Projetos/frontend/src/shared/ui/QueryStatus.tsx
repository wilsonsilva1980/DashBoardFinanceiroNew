export function Loading() { return <div className="loading-panel" role="status"><span className="spinner" /> Carregando seus registros…</div>; }
export function QueryError({ retry, message }: { retry(): void; message?: string }) { return <div className="error-panel" role="alert"><h2>Não foi possível carregar os registros</h2><p>{message || 'Verifique a conexão e tente novamente.'}</p><button className="secondary" onClick={retry}>Tentar novamente</button></div>; }
export function Pagination({ page, total, pageSize, onPage }: { page: number; total: number; pageSize: number; onPage(page: number): void }) {
  const pages = Math.max(1, Math.ceil(total / pageSize));
  return <div className="pagination"><span>{total} {total === 1 ? 'registro' : 'registros'} · Página {page} de {pages}</span><div><button className="secondary compact" disabled={page <= 1} onClick={() => onPage(page - 1)}>Anterior</button><button className="secondary compact" disabled={page >= pages} onClick={() => onPage(page + 1)}>Próxima</button></div></div>;
}
