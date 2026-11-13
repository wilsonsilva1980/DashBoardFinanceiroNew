import { useEffect, useId, useState, type ReactNode } from 'react';
import { useForm, type UseFormReturn } from 'react-hook-form';
import { useBlocker } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { z } from 'zod';
import { useScopeKey } from '../../app/Auth';
import { useWorkspace } from '../../app/Workspace';
import { ApiError, get, request } from '../../shared/api/client';
import type { CatalogEntry, Page } from '../../shared/api/contracts';
import { Dialog } from '../../shared/ui/Dialog';
import { accountSchema, cardSchema, catalogConfig, currencies, namedSchema, type CatalogKind } from './catalogConfig';

type Values = Record<string, string | boolean>;
export function Field({ form, name, label, type = 'text', hint, required = false, maxLength }: { form: UseFormReturn<Values>; name: string; label: string; type?: string; hint?: string; required?: boolean; maxLength?: number }) {
  const id = useId();
  const error = form.formState.errors[name];
  return <div className="field"><label htmlFor={id}>{label}{required && <span className="required-mark"> *</span>}</label><input id={id} {...form.register(name)} type={type} required={required} maxLength={maxLength} inputMode={type === 'date' ? undefined : /[Ll]imite|[Ss]aldo|[Uu]tilizado/.test(label) ? 'decimal' : undefined} aria-invalid={!!error} aria-describedby={error || hint ? `${id}-hint` : undefined} /><small id={`${id}-hint`} className={error ? 'field-error' : 'field-hint'}>{error ? String(error.message) : hint}</small></div>;
}
export function SelectField({ form, name, label, options }: { form: UseFormReturn<Values>; name: string; label: string; options: { value: string; label: string }[] }) {
  const id = useId();
  const error = form.formState.errors[name];
  return <div className="field"><label htmlFor={id}>{label}</label><select id={id} {...form.register(name)} value={String(form.watch(name) ?? '')} aria-invalid={!!error}>{options.map(option => <option key={option.value} value={option.value}>{option.label}</option>)}</select>{error && <small className="field-error">{String(error.message)}</small>}</div>;
}
export function ReferenceField({ form, name, label, path, optional = false }: { form: UseFormReturn<Values>; name: string; label: string; path: string; optional?: boolean }) {
  const scope = useScopeKey();
  const [search, setSearch] = useState('');
  const [query, setQuery] = useState('');
  useEffect(() => { const timer = setTimeout(() => setQuery(search), 200); return () => clearTimeout(timer); }, [search]);
  const selectedId = String(form.watch(name) ?? '');
  const list = useQuery({ queryKey: [...scope, 'lookup', path, query], queryFn: ({ signal }) => get<Page<CatalogEntry>>(`/api/v1/${path}?pageSize=100&search=${encodeURIComponent(query)}`, signal) });
  const selected = useQuery({ queryKey: [...scope, 'lookup-item', path, selectedId], enabled: !!selectedId && !list.data?.items.some(item => item.id === selectedId), queryFn: ({ signal }) => get<CatalogEntry>(`/api/v1/${path}/${selectedId}`, signal) });
  const rows = [...(list.data?.items ?? [])];
  if (selected.data && !rows.some(row => row.id === selected.data.id)) rows.unshift(selected.data);
  const options = [{ value: '', label: optional ? 'Não informado' : 'Selecione…' }, ...rows.map(row => ({ value: row.id, label: (row.name || `Conta ${row.number || 'sem número'}`) + (row.active ? '' : ' (inativo)') }))];
  return <div className="reference-field"><label className="search-reference">Buscar {label.toLowerCase()}<input type="search" value={search} maxLength={200} placeholder="Digite para buscar" onChange={event => setSearch(event.target.value)} /></label><SelectField form={form} name={name} label={label} options={options} />{list.isError && <small className="field-error">Não foi possível buscar os cadastros. <button className="text-button" type="button" onClick={() => void list.refetch()}>Tentar novamente</button></small>}</div>;
}

export function FinancialForm({ title, defaults, schema, endpoint, method = 'POST', version, children, onClose }: { title: string; defaults: Values; schema: z.ZodType; endpoint: string; method?: string; version?: number; children(form: UseFormReturn<Values>): ReactNode; onClose(): void }) {
  const scope = useScopeKey();
  const workspace = useWorkspace();
  const client = useQueryClient();
  const form = useForm<Values>({ defaultValues: defaults });
  const [error, setError] = useState('');
  const [discard, setDiscard] = useState(false);
  const blocker = useBlocker(form.formState.isDirty);
  useEffect(() => { if (!form.formState.isDirty) return; const handler = (event: BeforeUnloadEvent) => { event.preventDefault(); event.returnValue = ''; }; window.addEventListener('beforeunload', handler); return () => window.removeEventListener('beforeunload', handler); }, [form.formState.isDirty]);
  const save = useMutation({ mutationFn: (body: unknown) => request(endpoint, { method, body }), onSuccess: async () => { await client.invalidateQueries({ queryKey: scope }); onClose(); }, onError: cause => { setError(cause instanceof Error ? cause.message : 'Não foi possível salvar.'); if (cause instanceof ApiError && cause.status === 409) void client.invalidateQueries({ queryKey: scope }); } });
  const close = () => { if (save.isPending) return; if (form.formState.isDirty) setDiscard(true); else onClose(); };
  return <Dialog title={title} onClose={close}>
    <p className="context-line">Espaço: <strong>{workspace.spaceName}</strong></p>
    {(discard || blocker.state === 'blocked') && <div className="notice" role="alert"><p>Descartar as alterações que ainda não foram salvas?</p><div className="button-row"><button className="danger" type="button" onClick={() => { if (blocker.state === 'blocked') blocker.proceed(); else onClose(); }}>Descartar alterações</button><button className="secondary" type="button" onClick={() => { setDiscard(false); if (blocker.state === 'blocked') blocker.reset(); }}>Continuar editando</button></div></div>}
    {error && <p className="error-banner" role="alert">{error}</p>}
    <form onSubmit={form.handleSubmit(values => { form.clearErrors(); setError(''); const parsed = schema.safeParse(values); if (!parsed.success) { for (const issue of parsed.error.issues) form.setError(String(issue.path[0]), { message: issue.message }); return; } const payload = { ...(parsed.data as Record<string, unknown>), ...(version === undefined ? {} : { version }) }; save.mutate(payload); })}>
      <fieldset disabled={save.isPending}><div className="form-grid">{children(form)}</div>
      <div className="dialog-footer"><span className="field-hint">* Campo obrigatório</span><div className="button-row"><button type="button" className="secondary" onClick={close} disabled={save.isPending}>Cancelar</button><button className="primary" disabled={save.isPending}>{save.isPending ? 'Salvando…' : 'Salvar'}</button></div></div></fieldset>
    </form>
  </Dialog>;
}
export function CatalogForm({ kind, row, onClose }: { kind: CatalogKind; row?: CatalogEntry; onClose(): void }) {
  const { currency } = useWorkspace();
  const config = catalogConfig[kind];
  const defaults: Values = { name: row?.name ?? '', active: row?.active ?? true, nature: row?.nature ?? 'Unclassified', code: row?.code ?? '', institutionId: row?.institutionId ?? '', holderId: row?.holderId ?? '', brandId: row?.brandId ?? '', type: row?.type ?? '', agency: row?.agency ?? '', number: row?.number ?? '', checkDigit: row?.checkDigit ?? '', currency: row?.currency ?? currency, overdraftLimit: (row?.overdraftLimit ?? '0.00').replace('.', ','), dueDay: row?.dueDay?.toString() ?? '', closingDay: row?.closingDay?.toString() ?? '' };
  for (const field of ['creditLimit', 'cashLimit', 'foreignCashLimit', 'pixLimit', 'billLimit'] as const) defaults[field] = (row?.[field] ?? '').replace('.', ',');
  const schema = kind === 'contas' ? accountSchema : kind === 'cartoes' ? cardSchema : namedSchema;
  return <FinancialForm title={`${row ? 'Editar' : 'Adicionar'} ${config.singular}`} defaults={defaults} schema={schema} endpoint={`/api/v1/${kind}${row ? '/' + row.id : ''}`} method={row ? 'PUT' : 'POST'} version={row?.version} onClose={onClose}>{form => <>
    {kind !== 'contas' && <Field form={form} name="name" label="Nome" required maxLength={kind === 'cartoes' ? 120 : 200} />}
    {kind === 'instituicoes' && <Field form={form} name="code" label="Código da instituição" maxLength={20} hint="Preserve os zeros à esquerda, se houver." />}
    {kind === 'categorias' && <SelectField form={form} name="nature" label="Natureza" options={[{ value: 'Unclassified', label: 'Não classificada' }, { value: 'Fixed', label: 'Fixa' }, { value: 'Variable', label: 'Variável' }]} />}
    {(kind === 'contas' || kind === 'cartoes') && <><ReferenceField form={form} name="institutionId" label="Instituição" path="instituicoes" /><ReferenceField form={form} name="holderId" label="Titular" path="pessoas" optional /></>}
    {kind === 'contas' && <><Field form={form} name="type" label="Tipo de conta" maxLength={30} /><Field form={form} name="agency" label="Agência" maxLength={30} /><Field form={form} name="number" label="Número da conta" maxLength={60} hint="Zeros e letras são mantidos como informados." /><Field form={form} name="checkDigit" label="Dígito" maxLength={10} /><SelectField form={form} name="currency" label="Moeda" options={currencies.map(value => ({ value, label: value }))} /><Field form={form} name="overdraftLimit" label="Limite do cheque especial" required /></>}
    {kind === 'cartoes' && <><ReferenceField form={form} name="brandId" label="Bandeira" path="bandeiras" optional /><Field form={form} name="dueDay" label="Dia do vencimento" /><Field form={form} name="closingDay" label="Dia do fechamento" hint="Deixe vazio quando não souber." /><Field form={form} name="creditLimit" label="Limite principal" hint="Deixe vazio quando não informado." /><Field form={form} name="cashLimit" label="Limite de saque nacional" /><Field form={form} name="foreignCashLimit" label="Limite de saque exterior" /><Field form={form} name="pixLimit" label="Limite de Pix no crédito" /><Field form={form} name="billLimit" label="Limite de boletos" /></>}
    <label className="checkbox-field"><input type="checkbox" {...form.register('active')} /> Cadastro ativo</label>
  </>}</FinancialForm>;
}
