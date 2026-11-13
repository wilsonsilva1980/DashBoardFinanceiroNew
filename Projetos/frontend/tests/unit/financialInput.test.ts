import { describe, expect, it } from 'vitest';
import { parseCivilDate, parseMoneyInput } from '../../src/shared/forms/financialInput';

describe('entrada financeira', () => {
  it('preserva centavos e limites de decimal(19,2) sem arredondamento binário', () => {
    expect(parseMoneyInput('99999999999999999,99')).toBe('99999999999999999.99');
    expect(parseMoneyInput('0')).toBe('0.00');
    expect(parseMoneyInput('-12,5')).toBe('-12.50');
    expect(parseMoneyInput('')).toBeNull();
  });
  it.each(['1.234,56', '1e3', '1.001', 'NaN', '100000000000000000'])('rejeita valor ambíguo ou fora da precisão: %s', value => {
    expect(() => parseMoneyInput(value)).toThrow();
  });
  it('mantém datas desconhecidas e rejeita dias inexistentes', () => {
    expect(parseCivilDate('')).toBeNull();
    expect(parseCivilDate('2028-02-29')).toBe('2028-02-29');
    expect(() => parseCivilDate('2026-02-29')).toThrow();
    expect(() => parseCivilDate('2026-04-31')).toThrow();
  });
});
