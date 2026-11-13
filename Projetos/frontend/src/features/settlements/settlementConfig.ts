import { z } from 'zod';
import { parseCivilDate, parseMoneyInput } from '../../shared/forms/financialInput';
import { currencies } from '../cadastros/catalogConfig';

export const paymentStateLabels: Record<string, string> = {
  Draft: 'Rascunho',
  Confirmed: 'Confirmado',
  Reversed: 'Revertido',
};
export const agreementStateLabels: Record<string, string> = { Draft: 'Rascunho', Confirmed: 'Confirmado' };

export const money = z.string().transform((value, context) => {
  try { return parseMoneyInput(value); } catch (error) { context.addIssue({ code: 'custom', message: error instanceof Error ? error.message : 'Valor inválido.' }); return z.NEVER; }
});
export const requiredMoney = money.refine(value => value !== null, 'Informe o valor.');
export const positiveMoney = requiredMoney.refine(value => !value!.startsWith('-') && value !== '0.00', 'Informe um valor positivo.');
export const nonnegativeMoney = requiredMoney.refine(value => !value!.startsWith('-'), 'Informe um valor não negativo.');
export const civilDate = z.string().transform((value, context) => {
  if (!value) return null;
  try { return parseCivilDate(value); } catch { context.addIssue({ code: 'custom', message: 'Informe uma data válida.' }); return z.NEVER; }
});
export const requiredDate = civilDate.refine(value => value !== null, 'Informe a data.');
const text = (max: number) => z.string().trim().max(max, `Use até ${max} caracteres.`).transform(value => value || null);
const requiredText = (max: number) => z.string().trim().min(1, 'Informe o texto.').max(max, `Use até ${max} caracteres.`);

export const paymentSchema = z.object({
  accountId: z.string().min(1, 'Selecione a conta.'),
  payerId: z.string().min(1, 'Selecione o pagador.'),
  paymentDate: requiredDate,
  amount: positiveMoney,
  currency: z.enum(currencies),
  method: requiredText(50),
});
export const historicalSettlementSchema = z.object({
  amount: positiveMoney,
  effectiveDate: civilDate,
  justification: requiredText(1000),
  previousReference: text(200),
  sourceStatus: text(100),
});
export const agreementLineSchema = z.object({
  titleId: z.string().min(1, 'Título inválido.'),
  nominalAmount: nonnegativeMoney,
  discount: nonnegativeMoney,
}).refine(line => Number(line.discount) <= Number(line.nominalAmount), { message: 'O desconto não pode exceder o valor nominal.', path: ['discount'] });

export function allocationsSum(amounts: string[]): { sum: string; valid: boolean } {
  let cents = 0;
  for (const amount of amounts) {
    const parsed = parseMoneyInput(amount);
    if (parsed === null) return { sum: '0.00', valid: false };
    cents += Math.round(Number(parsed) * 100);
  }
  return { sum: `${Math.floor(cents / 100)}.${String(cents % 100).padStart(2, '0')}`, valid: true };
}

export function validAllocations(titleIds: string[], amounts: string[], total: string): string | null {
  if (!titleIds.length || titleIds.some((id, index) => !id || titleIds.indexOf(id) !== index)) return 'Cada alocação precisa de um título distinto.';
  if (amounts.some(amount => { try { return parseMoneyInput(amount) === null || parseMoneyInput(amount)!.startsWith('-') || parseMoneyInput(amount) === '0.00'; } catch { return true; } })) return 'Cada alocação deve ter valor positivo.';
  const { sum, valid } = allocationsSum(amounts);
  if (!valid) return 'Informe valores de alocação válidos.';
  try {
    const parsedTotal = parseMoneyInput(total);
    if (parsedTotal === null || parsedTotal !== sum) return 'A soma das alocações deve coincidir com o valor do pagamento.';
  } catch { return 'Informe um valor de pagamento válido.'; }
  return null;
}
