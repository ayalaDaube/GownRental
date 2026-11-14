import { configureStore } from '@reduxjs/toolkit';
import authReducer, { login, logout } from './AuthSlice';
import http from '../../services/http';
import { loadSession } from '../../auth/session';

// The real HTTP client is replaced so no network is used (and axios is not loaded by the test runner).
jest.mock('../../services/http', () => ({
  __esModule: true,
  default: { post: jest.fn() },
}));

const post = http.post as jest.Mock;

const newStore = () => configureStore({ reducer: { auth: authReducer } });

const loginResponse = {
  token: 'abc.def.ghi',
  expiresAtUtc: new Date(Date.now() + 60 * 60 * 1000).toISOString(),
  username: 'secretary',
  role: 'Secretary',
};

beforeEach(() => {
  sessionStorage.clear();
  post.mockReset();
});

test('a successful sign-in stores the token and who signed in', async () => {
  post.mockResolvedValue({ data: loginResponse });
  const store = newStore();

  await store.dispatch(login({ username: 'secretary', password: 'a-password' }));

  expect(store.getState().auth).toMatchObject({ token: 'abc.def.ghi', username: 'secretary', role: 'Secretary', error: null });
  expect(loadSession()?.token).toBe('abc.def.ghi');
});

test('wrong credentials show a clear message and store nothing', async () => {
  post.mockRejectedValue({ response: { status: 401 } });
  const store = newStore();

  await store.dispatch(login({ username: 'secretary', password: 'wrong' }));

  expect(store.getState().auth.token).toBeNull();
  expect(store.getState().auth.error).toBe('Wrong username or password.');
  expect(loadSession()).toBeNull();
});

test('too many attempts are reported as such', async () => {
  post.mockRejectedValue({ response: { status: 429 } });
  const store = newStore();

  await store.dispatch(login({ username: 'secretary', password: 'wrong' }));

  expect(store.getState().auth.error).toMatch(/too many attempts/i);
});

test('a server that cannot be reached gets its own message', async () => {
  post.mockRejectedValue(new Error('Network Error'));
  const store = newStore();

  await store.dispatch(login({ username: 'secretary', password: 'a-password' }));

  expect(store.getState().auth.error).toMatch(/could not reach the server/i);
});

test('signing out clears the token and the stored session', async () => {
  post.mockResolvedValue({ data: loginResponse });
  const store = newStore();
  await store.dispatch(login({ username: 'secretary', password: 'a-password' }));

  store.dispatch(logout());

  expect(store.getState().auth.token).toBeNull();
  expect(loadSession()).toBeNull();
});
