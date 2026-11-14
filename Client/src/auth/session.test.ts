import { clearSession, getToken, loadSession, saveSession } from './session';

const inOneHour = () => new Date(Date.now() + 60 * 60 * 1000).toISOString();
const oneHourAgo = () => new Date(Date.now() - 60 * 60 * 1000).toISOString();

beforeEach(() => sessionStorage.clear());

test('a saved session can be loaded back', () => {
  saveSession({ token: 'abc.def.ghi', username: 'secretary', role: 'Secretary', expiresAtUtc: inOneHour() });

  expect(loadSession()?.username).toBe('secretary');
  expect(getToken()).toBe('abc.def.ghi');
});

test('there is no session before anyone signs in', () => {
  expect(loadSession()).toBeNull();
  expect(getToken()).toBeNull();
});

test('an expired session is ignored and removed', () => {
  saveSession({ token: 'abc.def.ghi', username: 'secretary', role: 'Secretary', expiresAtUtc: oneHourAgo() });

  expect(loadSession()).toBeNull();
  expect(sessionStorage.length).toBe(0);
});

test('clearing the session removes the token', () => {
  saveSession({ token: 'abc.def.ghi', username: 'secretary', role: 'Secretary', expiresAtUtc: inOneHour() });

  clearSession();

  expect(getToken()).toBeNull();
});

test('a damaged stored value is treated as no session', () => {
  sessionStorage.setItem('gownrental.session', '{not valid json');

  expect(loadSession()).toBeNull();
});

test('the session is kept in sessionStorage only, not in localStorage', () => {
  saveSession({ token: 'abc.def.ghi', username: 'secretary', role: 'Secretary', expiresAtUtc: inOneHour() });

  expect(localStorage.length).toBe(0);
});
