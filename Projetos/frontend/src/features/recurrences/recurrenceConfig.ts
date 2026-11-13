import { z } from 'zod';
import { parseCivilDate, parseMoneyInput } from '../../shared/forms/financialInput';
import { currencies } from '../cadastros/catalogConfig';

const civilDate = z.string().transform((value, context) => {
  if (!value) return null;
  try { return parseCivilDate(value); } catch { context.addIssue({ code: 'custom', message: 'Informe uma data válida.' }); return z.NEVER; }
});
const requiredDate = civilDate.refine(value => value !== null, 'Informe a data.');
const optionalDate = civilDate;
const text = (max: number) => z.string().trim().max(max, `Use até ${max} caracteres.`).transform(value => value || null);
const requiredText = (max: number) => z.string().trim().min(1, 'Informe o texto.').max(max, `Use até ${max} caracteres.`);

const money = z.string().transform((value, context) => {
  try { return parseMoneyInput(value); } catch (error) { context.addIssue({ code: 'custom', message: error instanceof Error ? error.message : 'Valor inválido.' }); return z.NEVER; }
});
const positiveMoney = money.refine(value => value !== null && value !== '0.00', 'Informe um valor positivo.');

const day = z.string().transform((value, context) => {
  if (!value.trim()) { context.addIssue({ code: 'custom', message: 'Informe o dia.' }); return z.NEVER; }
  if (!/^\d{1,2}$/.test(value) || Number(value) < 1 || Number(value) > 31) { context.addIssue({ code: 'custom', message: 'Informe um dia entre 1 e 31.' }); return z.NEVER; }
  return Number(value);
});
const interval = z.string().transform((value, context) => {
  if (!/^\d{1,3}$/.test(value) || Number(value) < 1 || Number(value) > 120) { context.addIssue({ code: 'custom', message: 'Informe o intervalo entre 1 e 120 meses.' }); return z.NEVER; }
  return Number(value);
});

export const recurrenceSchema = z.object({
  obligationId: z.string().min(1, 'Selecione a obrigação.'),
  anchorDate: requiredDate,
  nominalDay: day,
  endDate: optionalDate,
  active: z.boolean(),
});
export const recurrencePriceSchema = z.object({
  startDate: requiredDate,
  endDate: optionalDate,
  cycleAmount: positiveMoney,
  currency: z.enum(currencies),
  intervalMonths: interval,
});
export const recurrenceGenerationSchema = z.object({
  from: requiredDate,
  to: requiredDate,
}).refine(values => values.to >= values.from, { message: 'A data final deve ser posterior ou igual à data inicial.', path: ['to'] });

export type RecurrenceFormValues = z.infer<typeof recurrenceSchema>;
export type RecurrencePriceFormValues = z.infer<typeof recurrencePriceSchema>;
export type RecurrenceGenerationFormValues = z.infer<typeof recurrenceGenerationSchema>;
