import { describe, expect, it } from 'vitest';
import { obligationSchema, planningSchema, titleSchema } from '../../src/features/obligations/obligationConfig';

const obligation = {
  description: 'Seguro', kind: 'Installment', creditor: '', purchaseDate: '', purchaseDateContext: '', dueDay: '15',
  installmentCount: '2', currency: 'BRL', responsibleId: '', categoryId: '', active: true,
};
const title = {
  obligationId: 'obligation', occurrenceKey: 'seguro-2026-12', period: '2026-12-01', dueDate: '', installmentNumber: '1',
  originalAmount: '120,50', state: 'Draft', responsibleId: '', cardId: '',
};

describe('formulários de obrigações e títulos', () => {
  it('converte valores e datas informados sem inventar dados desconhecidos', () => {
    const parsedTitle = titleSchema.parse(title);
    expect(parsedTitle.originalAmount).toBe('120.50');
    expect(parsedTitle.dueDate).toBeNull();
    expect(parsedTitle.obligationId).toBe('obligation');
    const parsedObligation = obligationSchema.parse(obligation);
    expect(parsedObligation.purchaseDate).toBeNull();
    expect(parsedObligation.dueDay).toBe(15);
    expect(parsedObligation.installmentCount).toBe(2);
    expect(parsedObligation.responsibleId).toBeNull();
  });

  it('rejeita valores e datas inválidos', () => {
    expect(titleSchema.safeParse({ ...title, originalAmount: '-1,00' }).success).toBe(false);
    expect(titleSchema.safeParse({ ...title, originalAmount: '1.234,56' }).success).toBe(false);
    expect(titleSchema.safeParse({ ...title, period: '2026-02-29' }).success).toBe(false);
    expect(titleSchema.safeParse({ ...title, occurrenceKey: '' }).success).toBe(false);
    expect(obligationSchema.safeParse({ ...obligation, dueDay: '31' }).success).toBe(true);
    expect(obligationSchema.safeParse({ ...obligation, dueDay: '32' }).success).toBe(false);
  });

  it('valida o planejamento por responsável', () => {
    expect(planningSchema.parse({ personId: 'person', amount: '60' }).amount).toBe('60.00');
    expect(planningSchema.safeParse({ personId: '', amount: '60' }).success).toBe(false);
  });
});
