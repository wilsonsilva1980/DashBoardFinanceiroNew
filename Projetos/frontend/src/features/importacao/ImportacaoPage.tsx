import { useEffect, useRef, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { get, request, requestUpload } from '../../shared/api/client';
import type { ImportAnalysisResult, ImportBatch, ImportConfirmation, ImportDecision, ImportRow } from '../../shared/api/contracts';
import { QueryError, Loading } from '../../shared/ui/QueryStatus';
import { useScopeKey } from '../../app/Auth';

const MAX_FILE_BYTES = 10 * 1024 * 1024;
const statesInProgress = new Set(['Received']);

export function ImportacaoPage() {
  const scope = useScopeKey();
  const client = useQueryClient();
  const inputRef = useRef<HTMLInputElement>(null);
  const [batchId, setBatchId] = useState<string>();
  const [selected, setSelected] = useState<ImportRow>();
  const [notice, setNotice] = useState('');
  const [severity, setSeverity] = useState('');
  const [decision, setDecision] = useState('');
  const batch = useQuery({
    queryKey: [...scope, 'importacao', batchId],
    queryFn: ({ signal }) => get<ImportBatch>(`/api/v1/importacoes/${batchId}`, signal),
    enabled: Boolean(batchId),
    refetchInterval: query => query.state.data && statesInProgress.has(query.state.data.state) ? 1000 : false,
  });
  const rows = useQuery({
    queryKey: [...scope, 'importacao', batchId, 'registros', severity, decision],
    queryFn: ({ signal }) => get<{ items: ImportRow[]; total: number; page: number; pageSize: number }>(`/api/v1/importacoes/${batchId}/registros?page=1&pageSize=100${severity ? `&severity=${severity}` : ''}${decision ? `&decision=${decision}` : ''}`, signal),
    enabled: Boolean(batchId && batch.data && !statesInProgress.has(batch.data.state)),
  });
  const upload = useMutation({
    mutationFn: (file: File) => requestUpload<ImportAnalysisResult>('/api/v1/importacoes', file),
    onSuccess: result => { setBatchId(result.batchId); setNotice('Arquivo recebido. A análise está sendo preparada.'); },
    onError: error => setNotice(error instanceof Error ? error.message : 'Não foi possível enviar o arquivo.'),
  });
  const decide = useMutation({
    mutationFn: (input: { row: ImportRow; decision: ImportDecision }) =>
      request<ImportRow>(`/api/v1/importacoes/${batchId}/registros/${input.row.id}/decisao`, {
        method: 'POST', body: { decision: input.decision, version: input.row.version },
      }),
    onSuccess: async () => { setSelected(undefined); await client.invalidateQueries({ queryKey: [...scope, 'importacao', batchId] }); },
    onError: error => setNotice(error instanceof Error ? error.message : 'Não foi possível registrar a decisão.'),
  });
  const confirm = useMutation({
    mutationFn: () => request<ImportConfirmation>(`/api/v1/importacoes/${batchId}/confirmar?version=${batch?.data?.version ?? 0}`, { method: 'POST' }),
    onSuccess: async result => {
      setNotice(`Importação confirmada: ${result.created} criados, ${result.reused} reaproveitados.`);
      await client.invalidateQueries({ queryKey: scope });
    },
    onError: error => setNotice(error instanceof Error ? error.message : 'Não foi possível confirmar a importação.'),
  });

  useEffect(() => {
    if (batch.data?.state === 'Confirmed') void client.invalidateQueries({ queryKey: scope });
  }, [batch.data?.state, client, scope]);

  const selectFile = (file?: File) => {
    if (!file) return;
    if (!file.name.toLowerCase().endsWith('.xlsx')) { setNotice('Selecione um arquivo .xlsx.'); return; }
    if (file.size > MAX_FILE_BYTES) { setNotice('O arquivo excede o limite de 10 MB.'); return; }
    setNotice('');
    upload.mutate(file);
  };

  return <section className="page-stack">
    <header className="page-heading"><div><p className="eyebrow">Migração controlada</p><h1>Importação de planilha</h1><p className="muted">Analise, revise e confirme dados do arquivo sem criar pagamentos bancários automaticamente.</p></div></header>
    <div className="data-card">
      <div
        className="dropzone"
        role="button"
        tabIndex={0}
        aria-label="Selecionar planilha XLSX"
        onClick={() => inputRef.current?.click()}
        onKeyDown={event => { if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); inputRef.current?.click(); } }}
        onDragOver={event => event.preventDefault()}
        onDrop={event => { event.preventDefault(); selectFile(event.dataTransfer.files[0]); }}
      >
        <strong>{upload.isPending ? 'Enviando arquivo...' : 'Solte sua planilha aqui'}</strong>
        <span>ou selecione um arquivo .xlsx de até 10 MB</span>
        <input ref={inputRef} className="visually-hidden" type="file" accept=".xlsx,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" aria-label="Arquivo XLSX" aria-invalid={Boolean(notice && !batchId)} onChange={event => selectFile(event.target.files?.[0])} />
      </div>
      {notice && <p className={upload.isError || confirm.isError || decide.isError ? 'error-banner' : 'notice'} role="status">{notice}</p>}
    </div>
    {batchId && batch.isPending && <Loading />}
    {batch.isError && <QueryError message={batch.error.message} retry={() => void batch.refetch()} />}
    {batch.data && <div className="data-card">
      <div className="table-toolbar"><div><strong>{batch.data.originalFileName}</strong><p className="muted">Estado: {batch.data.state}</p></div>
        <div className="inline-actions"><label>Severidade<select value={severity} onChange={event => setSeverity(event.target.value)}><option value="">Todas</option><option value="Error">Erro</option><option value="Warning">Aviso</option><option value="Info">Informação</option></select></label><label>Decisão<select value={decision} onChange={event => setDecision(event.target.value)}><option value="">Todas</option><option value="Pending">Pendente</option><option value="Approved">Aprovado</option><option value="Rejected">Rejeitado</option><option value="Ignored">Ignorado</option></select></label><a className="secondary" href={`/api/v1/importacoes/${batchId}/relatorio`}>Baixar CSV</a></div>
        {batch.data.state !== 'Confirmed' && batch.data.state !== 'Cancelled' && <button className="primary" type="button" disabled={confirm.isPending || rows.data?.items.some(row => row.severity === 'Error' && row.decision === 'Approved')} onClick={() => confirm.mutate()}>Confirmar aprovados</button>}
      </div>
      {rows.isPending && <Loading />}
      {rows.isError && <QueryError message={rows.error.message} retry={() => void rows.refetch()} />}
      {rows.data && <div className="table-scroll"><table><caption className="visually-hidden">Registros da importação</caption><thead><tr><th>Aba</th><th>Linha</th><th>Tipo</th><th>Issue</th><th>Decisão</th><th>Ações</th></tr></thead><tbody>
        {rows.data.items.map(row => <tr key={row.id}><td>{row.sheetName}</td><td>{row.rowNumber}</td><td>{row.sourceType}</td><td><span className={`status-${row.severity.toLowerCase()}`}>{row.issueMessage ?? '—'}</span></td><td>{row.decision}</td><td><button className="text-button" type="button" onClick={() => setSelected(row)}>Revisar</button></td></tr>)}
      </tbody></table></div>}
      {rows.data?.items.length === 0 && <p className="empty-state">Nenhum registro elegível foi encontrado.</p>}
    </div>}
    {selected && <div className="dialog-backdrop" role="presentation" onClick={() => setSelected(undefined)}><div className="dialog" role="dialog" aria-modal="true" aria-labelledby="import-row-title" onClick={event => event.stopPropagation()}>
      <h2 id="import-row-title">Revisar registro</h2><p>{selected.sheetName}, linha {selected.rowNumber} · {selected.cellReference}</p>{selected.issueMessage && <p className="notice">{selected.issueMessage}</p>}
      <div className="dialog-actions"><button className="secondary" type="button" disabled={decide.isPending} onClick={() => decide.mutate({ row: selected, decision: 'Ignored' })}>Ignorar</button><button className="secondary" type="button" disabled={decide.isPending} onClick={() => decide.mutate({ row: selected, decision: 'Rejected' })}>Rejeitar</button><button className="primary" type="button" disabled={decide.isPending} onClick={() => decide.mutate({ row: selected, decision: 'Approved' })}>Aprovar</button></div>
    </div></div>}
  </section>;
}
