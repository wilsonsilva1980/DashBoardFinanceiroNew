import { describe, expect, it } from 'vitest';
import { invoiceModes, invoicePurchaseSchema, invoiceSchema } from '../../src/features/invoices/invoiceConfig';

const invoice = { cardId: 'card-1', titleId: 'title-1', period: '2026-12-01', mode: 'Consolidado', reportedAmount: '', notes: '' };
const purchase = { purchaseObligationId: '', description: 'Restaurante', amount: '90,00', installmentNumber: '', occurrenceKey: 'cartao-2026-12-1' };

describe('formulários de faturas e compras analíticas', () => {
  it('converte faturas sem datas ou valores desconhecidos inventados', () => {
    const parsed = invoiceSchema.parse(invoice);
    expect(parsed.cardId).toBe('card-1');
    expect(parsed.titleId).toBe('title-1');
    expect(parsed.period).toBe('2026-12-01');
    expect(parsed.mode).toBe('Consolidado');
    expect(parsed.reportedAmount).toBeNull();
    expect(parsed.notes).toBeNull();
  });

  it('rejeita faturas sem cartão, título, período ou modo informados', () => {
    expect(invoiceSchema.safeParse({ ...invoice, cardId: '' }).success).toBe(false);
    expect(invoiceSchema.safeParse({ ...invoice, titleId: '' }).success).toBe(false);
    expect(invoiceSchema.safeParse({ ...invoice, period: '2026-02-30' }).success).toBe(false);
    expect(invoiceSchema.safeParse({ ...invoice, mode: '' }).success).toBe(false);
  });

  it('valida compras analíticas vinculadas à fatura', () => {
    const parsed = invoicePurchaseSchema.parse(purchase);
    expect(parsed.description).toBe('Restaurante');
    expect(parsed.amount).toBe('90.00');
    expect(parsed.purchaseObligationId).toBeNull();
    expect(parsed.installmentNumber).toBeNull();
    expect(invoicePurchaseSchema.safeParse({ ...purchase, description: '' }).success).toBe(false);
    expect(invoicePurchaseSchema.safeParse({ ...purchase, amount: '-90,00' }).success).toBe(false);
    expect(invoicePurchaseSchema.safeParse({ ...purchase, occurrenceKey: '' }).success).toBe(false);
  });

  it('expõe os modos de faturamento suportados', () => {
    expect(invoiceModes).toEqual(['Consolidado', 'Analítico']);
  });
});
