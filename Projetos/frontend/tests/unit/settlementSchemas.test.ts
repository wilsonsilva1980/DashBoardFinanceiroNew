import { describe, expect, it } from 'vitest';
import { agreementLineSchema, historicalSettlementSchema, paymentSchema, validAllocations } from '../../src/features/settlements/settlementConfig';

const payment = { accountId: 'account', payerId: 'person', paymentDate: '2026-11-12', amount: '150,00', currency: 'BRL', method: 'Pix' };

describe('formulários de liquidação', () => {
  it('valida pagamento e converte centavos sem float', () => {
    const parsed = paymentSchema.parse(payment);
    expect(parsed.amount).toBe('150.00');
    expect(parsed.paymentDate).toBe('2026-11-12');
    expect(paymentSchema.safeParse({ ...payment, paymentDate: '2026-02-30' }).success).toBe(false);
    expect(paymentSchema.safeParse({ ...payment, amount: '0,00' }).success).toBe(false);
    expect(paymentSchema.safeParse({ ...payment, method: '' }).success).toBe(false);
    expect(paymentSchema.safeParse({ ...payment, currency: 'XYZ' }).success).toBe(false);
  });

  it('recusa alocações inválidas, duplicadas ou com soma divergente', () => {
    expect(validAllocations(['a', 'b'], ['100,00', '50,00'], '150,00')).toBeNull();
    expect(validAllocations(['a', 'b'], ['100,00', '20,00'], '150,00')).toBe('A soma das alocações deve coincidir com o valor do pagamento.');
    expect(validAllocations(['a', 'a'], ['100,00', '50,00'], '150,00')).toBe('Cada alocação precisa de um título distinto.');
    expect(validAllocations(['a'], ['0,00'], '0,00')).toBe('Cada alocação deve ter valor positivo.');
    expect(validAllocations(['a'], ['1.234,56'], '1.234,56')).toBe('Cada alocação deve ter valor positivo.');
  });

  it('mantém datas desconhecidas em baixas históricas e exige justificativa', () => {
    const parsed = historicalSettlementSchema.parse({ amount: '40,00', effectiveDate: '', justification: 'Comprovante sem data', previousReference: '', sourceStatus: '' });
    expect(parsed.effectiveDate).toBeNull();
    expect(parsed.amount).toBe('40.00');
    expect(historicalSettlementSchema.safeParse({ amount: '40,00', effectiveDate: '', justification: '' }).success).toBe(false);
    expect(historicalSettlementSchema.safeParse({ amount: '-40,00', effectiveDate: '', justification: 'x' }).success).toBe(false);
  });

  it('limita o desconto ao valor nominal do título', () => {
    expect(agreementLineSchema.safeParse({ titleId: 'title', nominalAmount: '100,00', discount: '20,00' }).success).toBe(true);
    expect(agreementLineSchema.safeParse({ titleId: 'title', nominalAmount: '100,00', discount: '120,00' }).success).toBe(false);
    expect(agreementLineSchema.safeParse({ titleId: 'title', nominalAmount: '100,00', discount: '-1,00' }).success).toBe(false);
  });
});
