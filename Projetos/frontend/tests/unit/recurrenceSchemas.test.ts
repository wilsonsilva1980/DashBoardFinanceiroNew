import { describe, expect, it } from 'vitest';
import { recurrenceGenerationSchema, recurrencePriceSchema, recurrenceSchema } from '../../src/features/recurrences/recurrenceConfig';

const recurrence = { obligationId: 'obligation-1', anchorDate: '2026-12-01', nominalDay: '15', endDate: '', active: true };
const price = { recurrenceId: 'recurrence-1', startDate: '2026-12-01', endDate: '', cycleAmount: '120,50', currency: 'BRL', intervalMonths: '3' };

describe('formulários de recorrências', () => {
  it('converte valores e datas informados sem inventar dados desconhecidos', () => {
    const parsed = recurrenceSchema.parse(recurrence);
    expect(parsed.obligationId).toBe('obligation-1');
    expect(parsed.anchorDate).toBe('2026-12-01');
    expect(parsed.nominalDay).toBe(15);
    expect(parsed.endDate).toBeNull();
    expect(parsed.active).toBe(true);
  });

  it('rejeita recorrências sem obrigação, data ou dia nominal válido', () => {
    expect(recurrenceSchema.safeParse({ ...recurrence, obligationId: '' }).success).toBe(false);
    expect(recurrenceSchema.safeParse({ ...recurrence, anchorDate: '' }).success).toBe(false);
    expect(recurrenceSchema.safeParse({ ...recurrence, anchorDate: '2026-02-30' }).success).toBe(false);
    expect(recurrenceSchema.safeParse({ ...recurrence, nominalDay: '32' }).success).toBe(false);
    expect(recurrenceSchema.safeParse({ ...recurrence, endDate: '2026-13-01' }).success).toBe(false);
  });

  it('valida geração por período e vigência de ciclo com valor positivo', () => {
    expect(recurrenceGenerationSchema.safeParse({ from: '2026-12-01', to: '2027-12-01' }).success).toBe(true);
    expect(recurrenceGenerationSchema.safeParse({ from: '2027-12-01', to: '2026-12-01' }).success).toBe(false);
    const parsedPrice = recurrencePriceSchema.parse(price);
    expect(parsedPrice.cycleAmount).toBe('120.50');
    expect(parsedPrice.intervalMonths).toBe(3);
    expect(parsedPrice.endDate).toBeNull();
    expect(recurrencePriceSchema.safeParse({ ...price, cycleAmount: '0,00' }).success).toBe(false);
    expect(recurrencePriceSchema.safeParse({ ...price, intervalMonths: '0' }).success).toBe(false);
    expect(recurrencePriceSchema.safeParse({ ...price, currency: 'XYZ' }).success).toBe(false);
  });
});
