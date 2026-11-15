import http, { AUTH_EXPIRED_EVENT } from './http';
import { clearSession, loadSession, saveSession } from '../auth/session';

/** Replaces the network: answers with the given result and remembers what was sent. */
function fakeNetwork(result: { status: number } | 'ok') {
  const sent: { headers: Record<string, unknown>; url?: string; baseURL?: string }[] = [];
  http.defaults.adapter = async (config) => {
    sent.push({ headers: { ...config.headers.toJSON() }, url: config.url, baseURL: config.baseURL });
    if (result === 'ok') {
      return { data: {}, status: 200, statusText: 'OK', headers: {}, config };
    }
    return Promise.reject({ response: { status: result.status, data: {} }, config });
  };
  return sent;
}

const session = (minutesFromNow: number) => ({
  token: 'abc.def.ghi',
  username: 'secretary',
  role: 'Secretary',
  expiresAtUtc: new Date(Date.now() + minutesFromNow * 60_000).toISOString(),
});

beforeEach(() => clearSession());

test('requests go to the API under /api', async () => {
  const sent = fakeNetwork('ok');

  await http.get('/Gowns');

  expect(sent[0].baseURL).toMatch(/\/api$/);
});

test('a signed-in user sends the token with every request', async () => {
  const sent = fakeNetwork('ok');
  saveSession(session(60));

  await http.get('/Gowns');
  await http.post('/Orders', {});

  expect(sent.map(s => s.headers.Authorization)).toEqual(['Bearer abc.def.ghi', 'Bearer abc.def.ghi']);
});

test('nothing is sent as a token when nobody is signed in', async () => {
  const sent = fakeNetwork('ok');

  await http.get('/Appointment/getAvailableAppointments');

  expect(sent[0].headers.Authorization).toBeUndefined();
});

test('an expired session is not sent as a token', async () => {
  const sent = fakeNetwork('ok');
  saveSession(session(-5));

  await http.get('/Gowns');

  expect(sent[0].headers.Authorization).toBeUndefined();
});

test('when the server rejects the token the session is cleared and the app is told once', async () => {
  fakeNetwork({ status: 401 });
  saveSession(session(60));
  const listener = jest.fn();
  window.addEventListener(AUTH_EXPIRED_EVENT, listener);

  await expect(http.get('/Gowns')).rejects.toMatchObject({ response: { status: 401 } });

  window.removeEventListener(AUTH_EXPIRED_EVENT, listener);
  expect(loadSession()).toBeNull();
  expect(listener).toHaveBeenCalledTimes(1);
});

test('a failed sign-in (401 without a token) does not sign anybody out or raise the alarm', async () => {
  fakeNetwork({ status: 401 });
  const listener = jest.fn();
  window.addEventListener(AUTH_EXPIRED_EVENT, listener);

  await expect(http.post('/Auth/login', {})).rejects.toBeDefined();

  window.removeEventListener(AUTH_EXPIRED_EVENT, listener);
  expect(listener).not.toHaveBeenCalled();
});

test.each([400, 403, 404, 409, 429, 500])('a %i answer keeps the user signed in', async (status) => {
  fakeNetwork({ status });
  saveSession(session(60));
  const listener = jest.fn();
  window.addEventListener(AUTH_EXPIRED_EVENT, listener);

  await expect(http.get('/Gowns')).rejects.toBeDefined();

  window.removeEventListener(AUTH_EXPIRED_EVENT, listener);
  expect(loadSession()).not.toBeNull();
  expect(listener).not.toHaveBeenCalled();
});
