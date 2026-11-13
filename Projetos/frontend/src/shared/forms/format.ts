export function money(value: string | null | undefined, currency?: string): string {
  if (value === null || value === undefined) return 'Não informado';
  const sign = value.startsWith('-') ? '−' : '';
  const [whole, cents = '00'] = value.replace(/^-/, '').split('.');
  const grouped = whole.replace(/\B(?=(\d{3})+(?!\d))/g, '.');
  return `${currency === 'BRL' ? 'R$ ' : currency ? currency + ' ' : ''}${sign}${grouped},${cents.padEnd(2, '0')}`;
}
export function date(value: string) { const [year, month, day] = value.split('-'); return `${day}/${month}/${year}`; }
