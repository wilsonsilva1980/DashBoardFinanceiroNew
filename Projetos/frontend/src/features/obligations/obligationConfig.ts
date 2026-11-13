import { z } from 'zod';
import { parseCivilDate, parseMoneyInput } from '../../shared/forms/financialInput';
import { currencies } from '../cadastros/catalogConfig';

export const obligationKinds = [
  { value: 'Single', label: 'Única' },
  { value: 'Installment', label: 'Parcelada' },
] as const;
export const titleStates = [
  { value: 'Draft', label: 'Rascunho' },
  { value: 'Forecast', label: 'Previsão' },
  { value: 'Effective', label: 'Efetivo' },
  { value: 'Cancelled', label: 'Cancelado' },
] as const;
export const settlementLabels: Record<string, string> = {
  Open: 'Em aberto',
  Partial: 'Parcial',
  Paid: 'Pago',
  Overdue: 'Em atraso',
};
export const stateLabels: Record<string, string> = {
  Draft: 'Rascunho',
  Forecast: 'Previsão',
  Effective: 'Efetivo',
  Cancelled: 'Cancelado',
};

const optionalReference = z.string().transform(value => value || null);
const text = (max: number) => z.string().trim().max(max, `Use até ${max} caracteres.`).transform(value => value || null);
const money = z.string().transform((value, context) => {
  try { return parseMoneyInput(value); } catch (error) { context.addIssue({ code: 'custom', message: error instanceof Error ? error.message : 'Valor inválido.' }); return z.NEVER; }
});
const requiredMoney = money.refine(value => value !== null, 'Informe o valor.');
const nonnegativeMoney = requiredMoney.refine(value => !value!.startsWith('-'), 'Informe um valor não negativo.');
const civilDate = z.string().transform((value, context) => {
  if (!value) return null;
  try { return parseCivilDate(value); } catch { context.addIssue({ code: 'custom', message: 'Informe uma data válida.' }); return z.NEVER; }
});
const requiredDate = civilDate.refine(value => value !== null, 'Informe a data.');
const optionalCount = z.string().transform((value, context) => {
  if (!value.trim()) return null;
  if (!/^\d{1,3}$/.test(value) || Number(value) < 1 || Number(value) > 999) { context.addIssue({ code: 'custom', message: 'Informe um número entre 1 e 999.' }); return z.NEVER; }
  return Number(value);
});
const day = z.string().transform((value, context) => {
  if (!value.trim()) return null;
  if (!/^\d{1,2}$/.test(value) || Number(value) < 1 || Number(value) > 31) { context.addIssue({ code: 'custom', message: 'Informe um dia entre 1 e 31.' }); return z.NEVER; }
  return Number(value);
});

export const obligationSchema = z.object({
  description: z.string().trim().min(1, 'Informe a descrição.').max(1000, 'Use até 1000 caracteres.'),
  kind: z.enum(['Single', 'Installment']),
  creditor: text(200),
  purchaseDate: civilDate,
  purchaseDateContext: text(200),
  dueDay: day,
  installmentCount: optionalCount,
  currency: z.enum(currencies),
  responsibleId: optionalReference,
  categoryId: optionalReference,
  active: z.boolean(),
});
export const titleSchema = z.object({
  obligationId: z.string().min(1, 'Selecione a obrigação.'),
  occurrenceKey: z.string().trim().min(1, 'Informe a chave de ocorrência.').max(200, 'Use até 200 caracteres.'),
  period: requiredDate,
  dueDate: civilDate,
  installmentNumber: optionalCount,
  originalAmount: nonnegativeMoney,
  state: z.enum(['Draft', 'Forecast', 'Effective', 'Cancelled']),
  responsibleId: optionalReference,
  cardId: optionalReference,
});
export const planningSchema = z.object({
  personId: z.string().min(1, 'Selecione o responsável.'),
  amount: nonnegativeMoney,
});
