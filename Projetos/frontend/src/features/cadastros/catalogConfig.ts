import { z } from 'zod';
import { parseCivilDate, parseMoneyInput } from '../../shared/forms/financialInput';

export const currencies = ['BRL', 'USD', 'EUR', 'CLP', 'ARS', 'GBP', 'JPY', 'CAD', 'AUD', 'CHF', 'CNY', 'UYU', 'PYG', 'BOB', 'PEN', 'COP', 'MXN'] as const;
export type CatalogKind = 'pessoas' | 'categorias' | 'instituicoes' | 'bandeiras' | 'contas' | 'cartoes';
export const catalogConfig: Record<CatalogKind, { title: string; singular: string; description: string; action: string; empty: string }> = {
  pessoas: { title: 'Pessoas', singular: 'pessoa', description: 'Organize os responsáveis e pagadores do seu espaço.', action: 'Adicionar pessoa', empty: 'Nenhuma pessoa cadastrada' },
  categorias: { title: 'Categorias', singular: 'categoria', description: 'Dê uma organização clara aos seus compromissos.', action: 'Adicionar categoria', empty: 'Nenhuma categoria cadastrada' },
  instituicoes: { title: 'Instituições', singular: 'instituição', description: 'Mantenha as instituições das suas contas e cartões.', action: 'Adicionar instituição', empty: 'Nenhuma instituição cadastrada' },
  bandeiras: { title: 'Bandeiras', singular: 'bandeira', description: 'Cadastre as bandeiras dos cartões deste espaço.', action: 'Adicionar bandeira', empty: 'Nenhuma bandeira cadastrada' },
  contas: { title: 'Contas', singular: 'conta', description: 'Suas contas organizadas, com identificadores preservados.', action: 'Adicionar conta', empty: 'Nenhuma conta cadastrada' },
  cartoes: { title: 'Cartões', singular: 'cartão', description: 'Consulte os cartões, vencimentos e limites informados.', action: 'Adicionar cartão', empty: 'Nenhum cartão cadastrado' },
};
const optionalText = (max: number) => z.string().max(max, `Use até ${max} caracteres.`).transform(value => value === '' ? null : value);
export const decimalField = z.string().transform((value, context) => {
  try { return parseMoneyInput(value); } catch (error) { context.addIssue({ code: 'custom', message: error instanceof Error ? error.message : 'Valor inválido.' }); return z.NEVER; }
});
const nonnegative = decimalField.refine(value => value === null || !value.startsWith('-'), 'Informe um valor não negativo.');
const day = z.string().transform((value, context) => {
  if (!value) return null;
  if (!/^\d{1,2}$/.test(value) || Number(value) < 1 || Number(value) > 31) { context.addIssue({ code: 'custom', message: 'Informe um dia entre 1 e 31.' }); return z.NEVER; }
  return Number(value);
});
const reference = z.string().min(1, 'Selecione um cadastro.');
const optionalReference = z.string().transform(value => value || null);
const name = z.string().trim().min(1, 'Informe o nome.').max(200, 'Use até 200 caracteres.');
export const namedSchema = z.object({ name, active: z.boolean(), nature: z.enum(['Fixed', 'Variable', 'Unclassified']).optional(), code: optionalText(20).optional() });
export const accountSchema = z.object({ institutionId: reference, holderId: optionalReference, type: optionalText(30), agency: optionalText(30), number: optionalText(60), checkDigit: optionalText(10), currency: z.enum(currencies), overdraftLimit: nonnegative.refine(value => value !== null, 'Informe o limite, ou zero se não houver.'), active: z.boolean() });
export const cardSchema = z.object({ institutionId: reference, holderId: optionalReference, brandId: optionalReference, name: name.refine(value => value.length <= 120, 'Use até 120 caracteres.'), dueDay: day, closingDay: day, creditLimit: nonnegative, cashLimit: nonnegative, foreignCashLimit: nonnegative, pixLimit: nonnegative, billLimit: nonnegative, active: z.boolean() });
export const positionSchema = z.object({ accountId: reference, positionDate: z.string().transform((value, context) => {
  try { return parseCivilDate(value); } catch { context.addIssue({ code: 'custom', message: 'Informe uma data válida.' }); return z.NEVER; }
}).refine(value => value !== null, 'Informe a data da posição.'), balance: decimalField, savingsBalance: decimalField, overdraftUsed: nonnegative });
