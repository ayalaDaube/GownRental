import { act, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Route, Routes } from 'react-router-dom';
import Home from './Home';
import SecretaryHome from '../secretary/SecretaryHome';
import Notice from '../../components/common/Notice';
import { render } from '@testing-library/react';
import { renderWithApp, signedInState } from '../../testUtils';
import { saveSession, loadSession, clearSession } from '../../auth/session';

beforeEach(() => clearSession());

describe('the home page for a visitor', () => {
  test('invites booking an appointment and offers staff a way in, with no staff tools', () => {
    renderWithApp(<Home />);

    expect(screen.getByRole('heading', { name: 'Your Rental Experience Starts Here' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Appointments' })).toHaveAttribute('href', '/appointments');
    expect(screen.getByRole('link', { name: 'Staff sign-in' })).toHaveAttribute('href', '/login');
    expect(screen.queryByRole('link', { name: 'Manage Rentals' })).not.toBeInTheDocument();
    expect(screen.queryByRole('link', { name: 'Gown Inventory' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Sign out' })).not.toBeInTheDocument();
  });

  test('there is no hidden password prompt on the page any more', () => {
    renderWithApp(<Home />);

    expect(screen.queryByLabelText(/password/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/Enter as Secretary/i)).not.toBeInTheDocument();
  });

  test('a signed-in secretary who visits it sees the way to the dashboard and can sign out', () => {
    saveSession({ token: 'a.b.c', username: 'secretary', role: 'Secretary', expiresAtUtc: new Date(Date.now() + 3_600_000).toISOString() });
    const { store } = renderWithApp(<Home />, { preloadedState: signedInState });

    expect(screen.getByRole('link', { name: 'Dashboard' })).toHaveAttribute('href', '/secretary');
    expect(screen.queryByRole('link', { name: 'Staff sign-in' })).not.toBeInTheDocument();

    act(() => screen.getByRole('button', { name: 'Sign out' }).click());

    expect(store.getState().auth.token).toBeNull();
    expect(loadSession()).toBeNull();
    expect(screen.getByRole('link', { name: 'Staff sign-in' })).toBeInTheDocument();
  });
});

describe('the secretary dashboard', () => {
  test('welcomes the person by name and links to every tool', () => {
    renderWithApp(<SecretaryHome />, { route: '/secretary', preloadedState: signedInState });

    expect(screen.getByRole('heading', { name: 'Welcome, secretary' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Manage Rentals' })).toHaveAttribute('href', '/rentals');
    expect(screen.getByRole('link', { name: 'Gown Inventory' })).toHaveAttribute('href', '/gowns');
    expect(screen.getByRole('link', { name: 'Appointments' })).toHaveAttribute('href', '/employee/appointments');
  });

  test('signing out ends the session and goes to the home page', () => {
    saveSession({ token: 'a.b.c', username: 'secretary', role: 'Secretary', expiresAtUtc: new Date(Date.now() + 3_600_000).toISOString() });
    const { store } = renderWithApp(
      <Routes>
        <Route path="/secretary" element={<SecretaryHome />} />
        <Route path="/" element={<div>Home page</div>} />
      </Routes>,
      { route: '/secretary', preloadedState: signedInState }
    );

    userEvent.click(screen.getByRole('button', { name: 'Sign out' }));

    expect(screen.getByText('Home page')).toBeInTheDocument();
    expect(store.getState().auth.token).toBeNull();
    expect(loadSession()).toBeNull();
  });
});

describe('messages', () => {
  test('an error message is announced as an alert and a success message as a status', () => {
    const { rerender } = render(<Notice type="error" text="It failed" onClose={() => undefined} />);
    expect(screen.getByRole('alert')).toHaveTextContent('It failed');

    rerender(<Notice type="success" text="It worked" onClose={() => undefined} />);
    expect(screen.getByRole('status')).toHaveTextContent('It worked');
  });

  test('the close button tells the owner of the message to remove it', () => {
    const onClose = jest.fn();
    render(<Notice type="error" text="It failed" onClose={onClose} />);

    userEvent.click(screen.getByRole('button', { name: 'Dismiss message' }));

    expect(onClose).toHaveBeenCalledTimes(1);
  });
});
