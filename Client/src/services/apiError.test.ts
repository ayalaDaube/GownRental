import { getApiErrorMessage } from './apiError';

const failure = (status: number, data?: unknown) => ({ response: { status, data } });

test('uses the detail text the API sends with an error', () => {
  expect(getApiErrorMessage(failure(409, { detail: 'Gown 1 has rental history and cannot be deleted.' })))
    .toBe('Gown 1 has rental history and cannot be deleted.');
});

test('reports the first problem of an invalid form', () => {
  const error = failure(400, { errors: { Size: ['Size must be greater than zero.'], Price: ['Price is required.'] } });

  expect(getApiErrorMessage(error)).toBe('Size must be greater than zero.');
});

test('falls back to the title when there is no detail', () => {
  expect(getApiErrorMessage(failure(500, { title: 'Unexpected error' }))).toBe('Unexpected error');
});

test('explains an expired session and a missing permission', () => {
  expect(getApiErrorMessage(failure(401))).toMatch(/session has expired/i);
  expect(getApiErrorMessage(failure(403))).toMatch(/permission/i);
});

test('explains that the server cannot be reached when there is no response', () => {
  expect(getApiErrorMessage(new Error('Network Error'))).toMatch(/could not reach the server/i);
});

test('uses the given fallback for anything else', () => {
  expect(getApiErrorMessage(failure(418), 'Custom fallback')).toBe('Custom fallback');
});
