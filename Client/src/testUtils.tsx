import { ReactElement } from 'react';
import { render } from '@testing-library/react';
import { Provider } from 'react-redux';
import { MemoryRouter } from 'react-router-dom';
import { RootState, setupStore } from './store/store';
import { GownModelProps } from './redux/Gowns/GownTypes';
import { RentalProps } from './redux/Rentals/RentalType';
import { AppointmentType } from './redux/Appointments/AppointmentType';

/** Renders a screen the way the app does: with a fresh store and a router starting at the given address. */
export function renderWithApp(
  ui: ReactElement,
  options: { route?: string | { pathname: string; state?: unknown }; preloadedState?: Partial<RootState> } = {}
) {
  const store = setupStore(options.preloadedState);
  const utils = render(
    <Provider store={store}>
      <MemoryRouter initialEntries={[options.route ?? '/']}>{ui}</MemoryRouter>
    </Provider>
  );
  return { store, ...utils };
}

/** The state of a signed-in secretary. */
export const signedInState: Partial<RootState> = {
  auth: { token: 'a.b.c', username: 'secretary', role: 'Secretary', isLoading: false, error: null, justSignedOut: false },
};

export const aGown = (gownId: number, modelId: number, size: number, overrides: Partial<GownModelProps['details'][number]> = {}) => ({
  gownId, modelId, size, price: 200 + size, isActive: true, isAvailable: true, ...overrides,
});

export const gownModels: GownModelProps[] = [
  { id: 1, name: 'Simple Gown', color: 'blue', description: 'A simple blue gown', details: [aGown(1, 1, 36), aGown(2, 1, 38)] },
  { id: 2, name: 'Evening Gown', color: 'black', description: 'For the evening', details: [aGown(3, 2, 40, { isAvailable: false })] },
];

export const rentals: RentalProps[] = [
  { rentalId: 1, customerId: 'C000001', rentalDate: '2026-11-01T10:00:00', eventDate: '2026-12-24', gownId: 1, size: 36 },
  { rentalId: 2, customerId: 'C000002', rentalDate: '2026-11-02T10:00:00', eventDate: '2026-12-30', gownId: 3, size: 40 },
];

export const freeSlots: AppointmentType[] = [
  { id: 1, appointmentDate: '2026-12-01', hour: '09:00:00', customerId: '', numberOfPeople: null, isAvailable: true },
  { id: 2, appointmentDate: '2026-12-01', hour: '10:00:00', customerId: '', numberOfPeople: null, isAvailable: true },
];

/** What the API sends back for a refused request. */
export const refused = (status: number, detail: string) => ({ response: { status, data: { detail } } });
