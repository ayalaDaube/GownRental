import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Route, Routes } from 'react-router-dom';
import Login from './Login';
import http from '../../services/http';
import { loadSession, clearSession } from '../../auth/session';
import { renderWithApp, signedInState, refused } from '../../testUtils';

jest.mock('../../services/http', () => ({
  __esModule: true,
  default: { post: jest.fn() },
  AUTH_EXPIRED_EVENT: 'auth:expired',
}));

const post = http.post as jest.Mock;

const loginResponse = {
  token: 'abc.def.ghi',
  expiresAtUtc: new Date(Date.now() + 3_600_000).toISOString(),
  username: 'secretary',
  role: 'Secretary',
};

const screens = (
  <Routes>
    <Route path="/login" element={<Login />} />
    <Route path="/secretary" element={<div>Secretary dashboard</div>} />
    <Route path="/rentals" element={<div>Rentals page</div>} />
  </Routes>
);

const fill = (username: string, password: string) => {
  userEvent.type(screen.getByLabelText('Username'), username);
  userEvent.type(screen.getByLabelText('Password'), password);
};

beforeEach(() => {
  post.mockReset();
  clearSession();
});

test('the form asks for a username and a password', () => {
  renderWithApp(screens, { route: '/login' });

  expect(screen.getByRole('heading', { name: 'Staff sign-in' })).toBeInTheDocument();
  expect(screen.getByLabelText('Username')).toBeRequired();
  expect(screen.getByLabelText('Password')).toHaveAttribute('type', 'password');
  expect(screen.getByRole('button', { name: 'Sign in' })).toBeEnabled();
});

test('a correct sign-in opens the dashboard and remembers the session', async () => {
  post.mockResolvedValue({ data: loginResponse });
  renderWithApp(screens, { route: '/login' });

  fill('secretary', 'right-password');
  userEvent.click(screen.getByRole('button', { name: 'Sign in' }));

  expect(await screen.findByText('Secretary dashboard')).toBeInTheDocument();
  expect(post).toHaveBeenCalledWith('/Auth/login', { username: 'secretary', password: 'right-password' });
  expect(loadSession()?.token).toBe('abc.def.ghi');
});

test('spaces around the username are not sent', async () => {
  post.mockResolvedValue({ data: loginResponse });
  renderWithApp(screens, { route: '/login' });

  fill('   secretary  ', 'right-password');
  userEvent.click(screen.getByRole('button', { name: 'Sign in' }));

  await screen.findByText('Secretary dashboard');
  expect(post.mock.calls[0][1].username).toBe('secretary');
});

test('after signing in the user returns to the page they were trying to open', async () => {
  post.mockResolvedValue({ data: loginResponse });
  renderWithApp(screens, { route: { pathname: '/login', state: { from: { pathname: '/rentals' } } } });

  fill('secretary', 'right-password');
  userEvent.click(screen.getByRole('button', { name: 'Sign in' }));

  expect(await screen.findByText('Rentals page')).toBeInTheDocument();
});

test('a wrong password shows a clear message, keeps the form and signs nobody in', async () => {
  post.mockRejectedValue(refused(401, 'Invalid username or password.'));
  renderWithApp(screens, { route: '/login' });

  fill('secretary', 'wrong');
  userEvent.click(screen.getByRole('button', { name: 'Sign in' }));

  expect(await screen.findByRole('alert')).toHaveTextContent('Wrong username or password.');
  expect(screen.getByLabelText('Username')).toHaveValue('secretary');
  expect(screen.queryByText('Secretary dashboard')).not.toBeInTheDocument();
  expect(loadSession()).toBeNull();
});

test('too many attempts and an unreachable server each get their own message', async () => {
  renderWithApp(screens, { route: '/login' });
  fill('secretary', 'x');

  post.mockRejectedValueOnce(refused(429, ''));
  userEvent.click(screen.getByRole('button', { name: 'Sign in' }));
  expect(await screen.findByRole('alert')).toHaveTextContent(/too many attempts/i);

  post.mockRejectedValueOnce(new Error('Network Error'));
  userEvent.click(screen.getByRole('button', { name: 'Sign in' }));
  await waitFor(() => expect(screen.getByRole('alert')).toHaveTextContent(/could not reach the server/i));
});

test('while the request is running the button cannot be pressed again', async () => {
  let finish: (value: unknown) => void = () => undefined;
  post.mockReturnValue(new Promise(resolve => { finish = resolve; }));
  renderWithApp(screens, { route: '/login' });

  fill('secretary', 'right-password');
  userEvent.click(screen.getByRole('button', { name: 'Sign in' }));

  expect(await screen.findByRole('button', { name: 'Signing in…' })).toBeDisabled();
  userEvent.click(screen.getByRole('button', { name: 'Signing in…' }));
  expect(post).toHaveBeenCalledTimes(1);

  finish({ data: loginResponse });
  expect(await screen.findByText('Secretary dashboard')).toBeInTheDocument();
});

test('someone who is already signed in is sent straight to the dashboard', () => {
  renderWithApp(screens, { route: '/login', preloadedState: signedInState });

  expect(screen.getByText('Secretary dashboard')).toBeInTheDocument();
});

test('the page has a way back to the home page', () => {
  renderWithApp(screens, { route: '/login' });

  expect(screen.getByRole('link', { name: /back to the home page/i })).toHaveAttribute('href', '/');
});
