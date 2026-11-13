import { z } from 'zod';
import { parseCivilDate, parseMoneyInput } from '../../shared/forms/financialInput';

const civilDate = z.string().transform((value, context) => {
  if (!value) return null;
  try { return parseCivilDate(value); } catch { context.addIssue({ code: 'custom', message: 'Informe uma data válida.' }); return z.NEVER; }
});
const requiredDate = civilDate.refine(value => value !== null, 'Informe a data.');
const text = (max: number) => z.string().trim().max(max, `Use até ${max} caracteres.`).transform(value => value || null);
const requiredText = (max: number) => z.string().trim().min(1, 'Informe o texto.').max(max, `Use até ${max} caracteres.`);
const money = z.string().transform((value, context) => {
  try { return parseMoneyInput(value); } catch (error) { context.addIssue({ code: 'custom', message: error instanceof Error ? error.message : 'Valor inválido.' }); return z.NEVER; }
});
const nonnegativeMoney = money.refine(value => value === null || !value.startsWith('-'), 'Informe um valor não negativo.');
const optionalCount = z.string().transform((value, context) => {
  if (!value.trim()) return null;
  if (!/^\d{1,3}$/.test(value) || Number(value) < 1 || Number(value) > 999) { context.addIssue({ code: 'custom', message: 'Informe um número entre 1 e 999.' }); return z.NEVER; }
  return Number(value);
});

export const invoiceSchema = z.object({
  cardId: z.string().min(1, 'Selecione o cartão.'),
  titleId: z.string().min(1, 'Selecione o título da fatura.'),
  period: requiredDate,
  mode: requiredText(50),
  reportedAmount: nonnegativeMoney.optional(),
  notes: text(200),
});

export const invoicePurchaseSchema = z.object({
  purchaseObligationId: text(200).optional(),
  description: requiredText(1000),
  amount: nonnegativeMoney.refine(value => value !== null, 'Informe o valor.'),
  installmentNumber: optionalCount,
  occurrenceKey: requiredText(200),
});

export const invoiceModes = ['Consolidado', 'Analítico'] as const;
export type InvoiceFormValues = z.infer<typeof invoiceSchema>;
export type InvoicePurchaseFormValues = z.infer<typeof invoicePurchaseSchema>;
