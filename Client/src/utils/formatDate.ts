/**
 * Shows a date coming from the API (2026-12-31 or 2026-12-31T10:00:00.1234567) as 31/12/2026.
 * The text is rearranged rather than turned into a Date, so the time zone of the browser can never move the day.
 * Anything that does not start with a valid yyyy-MM-dd date is returned unchanged rather than shown as nonsense.
 */
export function formatDate(value: string | null | undefined): string {
  if (!value) return '';
  const match = /^(\d{4})-(\d{2})-(\d{2})(?:$|T|\s)/.exec(value);
  if (!match) return value;

  const [, year, month, day] = match;
  const asDate = new Date(Date.UTC(Number(year), Number(month) - 1, Number(day)));
  const real = asDate.getUTCFullYear() === Number(year)
    && asDate.getUTCMonth() === Number(month) - 1
    && asDate.getUTCDate() === Number(day);
  return real ? `${day}/${month}/${year}` : value;
}
