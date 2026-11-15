import { act, screen } from '@testing-library/react';
import App from './App';
import { renderWithApp, signedInState } from '../testUtils';
import { AUTH_EXPIRED_EVENT } from '../services/http';
import { saveSession, loadSession, clearSession } from '../auth/session';
import { fetchAllGroupsGowns } from '../services/Gowns/task.service';
import { fetchAllRentals, fetchRenting, fetchReturning } from '../services/Rentals/task.service';
import { fetchAllAppointmentsApi, fetchAvailableAppointmentsApi } from '../services/Appointments/task.service';

jest.mock('../services/Gowns/task.service');
jest.mock('../services/Rentals/task.service');
jest.mock('../services/Appointments/task.service');
jest.mock('../services/Orders/task.service');

beforeEach(() => {
  clearSession();
  (fetchAllGroupsGowns as jest.Mock).mockResolvedValue([]);
  (fetchAllRentals as jest.Mock).mockResolvedValue([]);
  (fetchRenting as jest.Mock).mockResolvedValue([]);
  (fetchReturning as jest.Mock).mockResolvedValue([]);
  (fetchAllAppointmentsApi as jest.Mock).mockResolvedValue([]);
  (fetchAvailableAppointmentsApi as jest.Mock).mockResolvedValue([]);
});

const STAFF_ONLY = ['/secretary', '/gowns', '/rentals', '/employee/appointments'];

describe('who can open which page', () => {
  test.each(STAFF_ONLY)('a visitor who opens %s is sent to the sign-in page', (route) => {
    renderWithApp(<App />, { route });

    expect(screen.getByRole('heading', { name: 'Staff sign-in' })).toBeInTheDocument();
  });

  test('everyone can open the home page', () => {
    renderWithApp(<App />, { route: '/' });

    expect(screen.getByRole('heading', { name: 'Your Rental Experience Starts Here' })).toBeInTheDocument();
  });

  test('everyone can open the appointment booking page', async () => {
    renderWithApp(<App />, { route: '/appointments' });

    expect(await screen.findByText('אין תורים פנויים')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'תורים פנויים' })).toBeInTheDocument();
  });

  test('everyone can open the sign-in page', () => {
    renderWithApp(<App />, { route: '/login' });

    expect(screen.getByRole('heading', { name: 'Staff sign-in' })).toBeInTheDocument();
  });

  test('the home page offers staff a way in and visitors the booking', () => {
    renderWithApp(<App />, { route: '/' });

    expect(screen.getByRole('link', { name: 'Staff sign-in' })).toHaveAttribute('href', '/login');
    expect(screen.getByRole('link', { name: 'Appointments' })).toHaveAttribute('href', '/appointments');
    expect(screen.queryByText('Sign out')).not.toBeInTheDocument();
  });

  test('a signed-in secretary reaches the dashboard and sees their name', () => {
    renderWithApp(<App />, { route: '/secretary', preloadedState: signedInState });

    expect(screen.getByRole('heading', { name: /Welcome, secretary/ })).toBeInTheDocument();
  });

  test('a signed-in secretary who opens the sign-in page is sent on to the dashboard', () => {
    renderWithApp(<App />, { route: '/login', preloadedState: signedInState });

    expect(screen.getByRole('heading', { name: /Welcome, secretary/ })).toBeInTheDocument();
  });

  test.each([
    ['/gowns', 'Gowns Collection'],
    ['/rentals', 'New for today'],
    ['/employee/appointments', 'ניהול תורים'],
  ])('a signed-in secretary can open %s', async (route, content) => {
    renderWithApp(<App />, { route, preloadedState: signedInState });

    expect(await screen.findByText(content)).toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: 'Staff sign-in' })).not.toBeInTheDocument();
  });

  test('an address that does not exist shows a page that says so and leads home', () => {
    renderWithApp(<App />, { route: '/this/page/does/not/exist' });

    expect(screen.getByRole('heading', { name: /page not found/i })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /home/i })).toHaveAttribute('href', '/');
  });
});

describe('signing out', () => {
  test('the dashboard has a sign-out button that ends the session and returns to the home page', () => {
    saveSession({ token: 'a.b.c', username: 'secretary', role: 'Secretary', expiresAtUtc: new Date(Date.now() + 3_600_000).toISOString() });
    const { store } = renderWithApp(<App />, { route: '/secretary', preloadedState: signedInState });

    act(() => screen.getByRole('button', { name: 'Sign out' }).click());

    expect(store.getState().auth.token).toBeNull();
    expect(loadSession()).toBeNull();
    expect(screen.getByRole('link', { name: 'Staff sign-in' })).toBeInTheDocument();
  });

  test('after signing out the home page is shown, not the sign-in page', () => {
    renderWithApp(<App />, { route: '/secretary', preloadedState: signedInState });

    act(() => screen.getByRole('button', { name: 'Sign out' }).click());

    expect(screen.getByRole('heading', { name: 'Your Rental Experience Starts Here' })).toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: 'Staff sign-in' })).not.toBeInTheDocument();
  });

  test('when the server says the session ended the user is sent to sign in again', async () => {
    saveSession({ token: 'a.b.c', username: 'secretary', role: 'Secretary', expiresAtUtc: new Date(Date.now() + 3_600_000).toISOString() });
    const { store } = renderWithApp(<App />, { route: '/rentals', preloadedState: signedInState });

    act(() => { window.dispatchEvent(new Event(AUTH_EXPIRED_EVENT)); });

    expect(store.getState().auth.token).toBeNull();
    expect(await screen.findByRole('heading', { name: 'Staff sign-in' })).toBeInTheDocument();
  });
});
