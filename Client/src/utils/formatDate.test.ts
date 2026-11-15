import { formatDate } from './formatDate';

describe('formatDate', () => {
  test.each([
    ['2026-12-31', '31/12/2026'],
    ['2026-01-05', '05/01/2026'],
    ['2026-11-10T10:00:00', '10/11/2026'],
    ['2026-11-14T23:28:18.7651916', '14/11/2026'], // the late evening must stay on the same day
    ['2026-11-14T23:59:59Z', '14/11/2026'],
    ['2028-02-29', '29/02/2028'],                  // a real leap day
  ])('%s is shown as %s', (input, expected) => {
    expect(formatDate(input)).toBe(expected);
  });

  test.each([
    ['2026-02-30'],      // there is no 30 February
    ['2027-02-29'],      // 2027 is not a leap year
    ['2026-13-01'],
    ['2026-00-10'],
    ['not a date'],
    ['31/12/2026'],      // already formatted
    ['12026-12-31'],
  ])('%s is returned unchanged instead of being shown as a wrong date', (input) => {
    expect(formatDate(input)).toBe(input);
  });

  test('a missing value is shown as nothing', () => {
    expect(formatDate(null)).toBe('');
    expect(formatDate(undefined)).toBe('');
    expect(formatDate('')).toBe('');
  });
});
