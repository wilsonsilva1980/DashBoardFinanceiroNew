/** Canonical decimal text for the API; never passes money through binary floats. */
export function parseMoneyInput(input: string): string | null {
  const value = input.trim();
  if (!value) return null;
  if (!/^-?\d{1,17}(?:[.,]\d{1,2})?$/.test(value)) {
    throw new Error('Informe um valor com até duas casas decimais, sem separadores de milhar.');
  }
  const [integer, fraction = ''] = value.replace(',', '.').split('.');
  const normalizedInteger = integer.replace(/^(-?)0+(?=\d)/, '$1');
  return `${normalizedInteger}.${fraction.padEnd(2, '0')}`;
}

/** Unknown dates stay null. Invalid dates never roll over to another month. */
export function parseCivilDate(input: string): string | null {
  if (!input.trim()) return null;
  if (!/^\d{4}-\d{2}-\d{2}$/.test(input)) throw new Error('Data inválida.');
  const date = new Date(`${input}T00:00:00Z`);
  if (Number.isNaN(date.valueOf()) || date.toISOString().slice(0, 10) !== input) {
    throw new Error('Data inválida.');
  }
  return input;
}
