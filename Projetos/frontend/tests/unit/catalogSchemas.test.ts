import { describe, expect, it } from 'vitest';
import { accountSchema, cardSchema, positionSchema } from '../../src/features/cadastros/catalogConfig';
import { money } from '../../src/shared/forms/format';

describe('formulários financeiros', () => {
  it('preserva identificadores e centavos sem passagem por float', () => {
    const input = { institutionId: 'bank', holderId: '', type: '', agency: '0001-A', number: '0000123', checkDigit: 'X', currency: 'BRL', overdraftLimit: '99999999999999999,99', active: true };
    const parsed = accountSchema.parse(input);
    expect(parsed.number).toBe('0000123'); expect(parsed.agency).toBe('0001-A');
    expect(parsed.overdraftLimit).toBe('99999999999999999.99');
    expect(money(parsed.overdraftLimit, 'BRL')).toBe('R$ 99.999.999.999.999.999,99');
    expect(accountSchema.safeParse({ ...input, overdraftLimit: '-1,00' }).success).toBe(false);
  });
  it('mantém saldos desconhecidos e impede data impossível', () => {
    const base = { accountId: 'account', positionDate: '2026-11-09', balance: '', savingsBalance: '', overdraftUsed: '' };
    expect(positionSchema.parse(base).balance).toBeNull();
    expect(positionSchema.safeParse({ ...base, positionDate: '2026-02-29' }).success).toBe(false);
    expect(positionSchema.safeParse({ ...base, overdraftUsed: '-1' }).success).toBe(false);
    expect(cardSchema.safeParse({ institutionId: 'bank', holderId: '', brandId: '', name: 'Card', dueDay: '32', closingDay: '', creditLimit: '', cashLimit: '', foreignCashLimit: '', pixLimit: '', billLimit: '', active: true }).success).toBe(false);
  });
});
