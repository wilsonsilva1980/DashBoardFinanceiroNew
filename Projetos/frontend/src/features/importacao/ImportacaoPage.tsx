import { useEffect, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { get, request, requestUpload } from '../../shared/api/client';
import type { ImportAnalysisResult, ImportBatch, ImportConfirmation, ImportDecision, ImportPendingSummary, ImportReconciliation, ImportRow } from '../../shared/api/contracts';
import { QueryError, Loading } from '../../shared/ui/QueryStatus';
import { useScopeKey } from '../../app/Auth';

const MAX_FILE_BYTES = 10 * 1024 * 1024;
const statesInProgress = new Set(['Received']);

export function ImportacaoPage() {
  const scope = useScopeKey();
  const client = useQueryClient();
  const [batchId, setBatchId] = useState<string>();
  const [selected, setSelected] = useState<ImportRow>();
  const [selectedRows, setSelectedRows] = useState<Map<string, number>>(new Map());
  const [page, setPage] = useState(1);
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
    queryKey: [...scope, 'importacao', batchId, 'registros', page, severity, decision],
    queryFn: ({ signal }) => get<{ items: ImportRow[]; total: number; page: number; pageSize: number }>(`/api/v1/importacoes/${batchId}/registros?page=${page}&pageSize=25${severity ? `&severity=${severity}` : ''}${decision ? `&decision=${decision}` : ''}`, signal),
    enabled: Boolean(batchId && batch.data && !statesInProgress.has(batch.data.state)),
  });
  const pending = useQuery({
    queryKey: [...scope, 'importacao', batchId, 'pendencias'],
    queryFn: ({ signal }) => get<ImportPendingSummary>(`/api/v1/importacoes/${batchId}/pendencias`, signal),
    enabled: Boolean(batchId && batch.data && !statesInProgress.has(batch.data.state)),
  });
  const reconciliation = useQuery({
    queryKey: [...scope, 'importacao', batchId, 'reconciliacao'],
    queryFn: ({ signal }) => get<ImportReconciliation[]>(`/api/v1/importacoes/${batchId}/reconciliacao`, signal),
    enabled: Boolean(batchId && batch.data && !statesInProgress.has(batch.data.state)),
  });
  const decideReconciliation = useMutation({
    mutationFn: (input: { candidate: ImportReconciliation; decision: 'Linked' | 'Rejected' }) =>
      request<ImportReconciliation>(`/api/v1/importacoes/${batchId}/reconciliacao/${input.candidate.id}`, {
        method: 'POST', body: { decision: input.decision, version: input.candidate.version },
      }),
    onSuccess: async () => {
      await Promise.all([
        client.invalidateQueries({ queryKey: [...scope, 'importacao', batchId] }),
        client.invalidateQueries({ queryKey: [...scope, 'importacao', batchId, 'reconciliacao'] }),
        client.invalidateQueries({ queryKey: [...scope, 'importacao', batchId, 'pendencias'] }),
      ]);
    },
    onError: error => setNotice(error instanceof Error ? error.message : 'Não foi possível decidir a reconciliação.'),
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
  const decideBatch = useMutation({
    mutationFn: (nextDecision: ImportDecision) => request<ImportRow[]>(`/api/v1/importacoes/${batchId}/decisoes`, {
      method: 'POST',
      body: { items: [...selectedRows].map(([rowId, version]) => ({ rowId, decision: nextDecision, version })) },
    }),
    onSuccess: async () => { setSelectedRows(new Map()); await client.invalidateQueries({ queryKey: [...scope, 'importacao', batchId] }); },
    onError: error => setNotice(error instanceof Error ? error.message : 'Não foi possível registrar as decisões.'),
  });
  const correct = useMutation({
    mutationFn: (input: { fieldName: string; valueJson: string; reason: string }) =>
      request<ImportRow>(`/api/v1/importacoes/${batchId}/registros/${selected?.id}/correcao`, {
        method: 'POST', body: { ...input, version: selected?.version ?? 0 },
      }),
    onSuccess: async () => { setSelected(undefined); await client.invalidateQueries({ queryKey: [...scope, 'importacao', batchId] }); },
    onError: error => setNotice(error instanceof Error ? error.message : 'Não foi possível corrigir o registro.'),
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

  const totalPages = rows.data ? Math.max(1, Math.ceil(rows.data.total / rows.data.pageSize)) : 1;
  const allPageSelected = Boolean(rows.data?.items.length && rows.data.items.every(row => selectedRows.has(row.id)));
  return <section className="page-stack">
    <header className="page-heading"><div><p className="eyebrow">Migração controlada</p><h1>Importação de planilha</h1><p className="muted">Analise, revise e confirme dados do arquivo sem criar pagamentos bancários automaticamente.</p></div></header>
    <div className="data-card">
      <div className="table-toolbar">
        <div><strong>Planilha modelo</strong><p className="muted">Use este exemplo para preparar arquivos compatíveis com a importação.</p></div>
        <a className="secondary" href="/modelos/planilha-financeiro-modelo.xlsx" download>Baixar modelo XLSX</a>
      </div>
      <p className="field-hint">O modelo contém exemplos nas abas Dívidas, Parcelas, Viagens e Instruções. Substitua os exemplos pelos seus dados e mantenha os cabeçalhos.</p>
    </div>
    <div className="data-card">
      <label
        className="dropzone"
        aria-label="Selecionar planilha XLSX"
        onDragOver={event => event.preventDefault()}
        onDrop={event => { event.preventDefault(); selectFile(event.dataTransfer.files[0]); }}
      >
        <strong>{upload.isPending ? 'Enviando arquivo...' : 'Solte sua planilha aqui'}</strong>
        <span>ou selecione um arquivo .xlsx de até 10 MB</span>
        <input className="visually-hidden" type="file" accept=".xlsx,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" aria-label="Arquivo XLSX" aria-invalid={Boolean(notice && !batchId)} onChange={event => selectFile(event.target.files?.[0])} />
      </label>
      {notice && <p className={upload.isError || confirm.isError || decide.isError ? 'error-banner' : 'notice'} role="status">{notice}</p>}
    </div>
    {batchId && batch.isPending && <Loading />}
    {batch.isError && <QueryError message={batch.error.message} retry={() => void batch.refetch()} />}
    {batch.data && <div className="data-card">
      <div className="table-toolbar"><div><strong>{batch.data.originalFileName}</strong><p className="muted">Estado: {batch.data.state}</p></div>
        <div className="inline-actions"><label>Severidade<select value={severity} onChange={event => { setSeverity(event.target.value); setPage(1); }}><option value="">Todas</option><option value="Error">Erro</option><option value="Warning">Aviso</option><option value="Info">Informação</option></select></label><label>Decisão<select value={decision} onChange={event => { setDecision(event.target.value); setPage(1); }}><option value="">Todas</option><option value="Pending">Pendente</option><option value="Approved">Aprovado</option><option value="Rejected">Rejeitado</option><option value="Ignored">Ignorado</option></select></label><a className="secondary" href={`/api/v1/importacoes/${batchId}/relatorio`}>Baixar CSV</a></div>
        {batch.data.state !== 'Confirmed' && batch.data.state !== 'Cancelled' && <button className="primary" type="button" disabled={confirm.isPending || pending.data?.pending !== 0 || pending.data?.errors !== 0} onClick={() => confirm.mutate()}>Confirmar aprovados</button>}
      </div>
      {pending.data && <p className="field-hint">Pendências globais: {pending.data.pending} · erros: {pending.data.errors} · avisos: {pending.data.warnings}</p>}
      {batch.data.statisticsJson !== '{}' && <details className="field-hint"><summary>Resumo do lote</summary><pre>{batch.data.statisticsJson}</pre></details>}
      {reconciliation.data && reconciliation.data.length > 0 && <div className="field-hint"><strong>Reconciliação</strong>{reconciliation.data.map(candidate => <div key={candidate.id}>{candidate.reason} · {candidate.decision}<button className="text-button" type="button" disabled={candidate.decision !== 'Pending'} onClick={() => decideReconciliation.mutate({ candidate, decision: 'Linked' })}>Vincular</button><button className="text-button" type="button" disabled={candidate.decision !== 'Pending'} onClick={() => decideReconciliation.mutate({ candidate, decision: 'Rejected' })}>Manter separado</button></div>)}</div>}
      {rows.isPending && <Loading />}
      {rows.isError && <QueryError message={rows.error.message} retry={() => void rows.refetch()} />}
      {rows.data && <><div className="table-scroll"><table><caption className="visually-hidden">Registros da importação</caption><thead><tr><th><input type="checkbox" aria-label="Selecionar página" checked={allPageSelected} onChange={event => setSelectedRows(current => { const next = new Map(current); rows.data?.items.forEach(row => event.target.checked ? next.set(row.id, row.version) : next.delete(row.id)); return next; })} /></th><th>Aba</th><th>Linha</th><th>Mês</th><th>Tipo</th><th>Issue</th><th>Decisão</th><th>Ações</th></tr></thead><tbody>
        {rows.data.items.map(row => <tr key={row.id}><td><input type="checkbox" aria-label={`Selecionar ${row.sheetName}, linha ${row.rowNumber}`} checked={selectedRows.has(row.id)} onChange={event => setSelectedRows(current => { const next = new Map(current); event.target.checked ? next.set(row.id, row.version) : next.delete(row.id); return next; })} /></td><td>{row.sheetName}</td><td>{row.rowNumber}</td><td>{row.month ?? '—'}</td><td>{row.sourceType}</td><td><span className={`status-${row.severity.toLowerCase()}`}>{row.issueMessage ?? '—'}</span></td><td>{row.decision}</td><td><button className="text-button" type="button" onClick={() => setSelected(row)}>Revisar</button></td></tr>)}
      </tbody></table></div>
      {selectedRows.size > 0 && <div className="inline-actions"><span>{selectedRows.size} selecionados</span><button className="secondary" type="button" onClick={() => decideBatch.mutate('Rejected')}>Rejeitar selecionados</button><button className="primary" type="button" onClick={() => decideBatch.mutate('Approved')}>Aprovar selecionados</button></div>}
      <div className="inline-actions"><button className="secondary" type="button" disabled={page <= 1} onClick={() => setPage(current => current - 1)}>Anterior</button><span>Página {page} de {totalPages}</span><button className="secondary" type="button" disabled={page >= totalPages} onClick={() => setPage(current => current + 1)}>Próxima</button></div></>}
      {rows.data?.items.length === 0 && <p className="empty-state">Nenhum registro elegível foi encontrado.</p>}
    </div>}
    {selected && <div className="dialog-backdrop" role="presentation" onClick={() => setSelected(undefined)}><div className="dialog" role="dialog" aria-modal="true" aria-labelledby="import-row-title" onClick={event => event.stopPropagation()}>
      <h2 id="import-row-title">Revisar registro</h2><p>{selected.sheetName}, linha {selected.rowNumber} · {selected.cellReference}</p>{selected.issueMessage && <p className="notice">{selected.issueMessage}</p>}<pre className="field-hint">{selected.derivedValuesJson}</pre>
      <form onSubmit={event => { event.preventDefault(); const data = new FormData(event.currentTarget); correct.mutate({ fieldName: String(data.get('fieldName') ?? ''), valueJson: String(data.get('valueJson') ?? ''), reason: String(data.get('reason') ?? '') }); }}><label>Campo<input name="fieldName" required /></label><label>Valor corrigido<input name="valueJson" /></label><label>Motivo<textarea name="reason" required /></label><button className="secondary" type="submit" disabled={correct.isPending}>Salvar correção</button></form>
      <div className="dialog-actions"><button className="secondary" type="button" disabled={decide.isPending} onClick={() => decide.mutate({ row: selected, decision: 'Ignored' })}>Ignorar</button><button className="secondary" type="button" disabled={decide.isPending} onClick={() => decide.mutate({ row: selected, decision: 'Rejected' })}>Rejeitar</button><button className="primary" type="button" disabled={decide.isPending} onClick={() => decide.mutate({ row: selected, decision: 'Approved' })}>Aprovar</button></div>
    </div></div>}
  </section>;
}
