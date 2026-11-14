import { configureStore } from '@reduxjs/toolkit';
import rentalReducer, {
  addRentalThunk, deleteRentalThunk, dismissNotice, getRentals, updateRentalThunk,
} from './RentalSlice';
import { createRentalApi, deleteRentalApi, fetchAllRentals, updateRentalApi } from '../../services/Rentals/task.service';

// The services are replaced so no network is used (and axios is not loaded by the test runner).
jest.mock('../../services/Rentals/task.service', () => ({
  fetchAllRentals: jest.fn(),
  createRentalApi: jest.fn(),
  deleteRentalApi: jest.fn(),
  updateRentalApi: jest.fn(),
  fetchReturning: jest.fn(),
  fetchRenting: jest.fn(),
}));

const fetchAll = fetchAllRentals as jest.Mock;
const create = createRentalApi as jest.Mock;
const update = updateRentalApi as jest.Mock;
const remove = deleteRentalApi as jest.Mock;

const rental = (rentalId: number, gownId = 21) => ({
  rentalId, customerId: 'C000001', rentalDate: '2026-11-14T10:00:00', eventDate: '2026-12-24', gownId, size: 44,
});

const newStore = () => configureStore({ reducer: { rentals: rentalReducer } });

beforeEach(() => jest.resetAllMocks());

test('a created rental is added to the list exactly as the server returned it', async () => {
  create.mockResolvedValue(rental(11));
  const store = newStore();

  const result = await store.dispatch(addRentalThunk({ customerId: 'C000001', gownId: 21, eventDate: '2026-12-24' }));

  expect(addRentalThunk.fulfilled.match(result)).toBe(true);
  expect(store.getState().rentals.rentals).toEqual([rental(11)]);
});

test('a refused rental gives the form the reason from the server and adds nothing', async () => {
  create.mockRejectedValue({ response: { status: 409, data: { detail: 'Gown 21 is already rented for an event on 2026-12-22.' } } });
  const store = newStore();

  const result = await store.dispatch(addRentalThunk({ customerId: 'C000001', gownId: 21, eventDate: '2026-12-24' }));

  expect(addRentalThunk.rejected.match(result)).toBe(true);
  expect(result.payload).toBe('Gown 21 is already rented for an event on 2026-12-22.');
  expect(store.getState().rentals.rentals).toEqual([]);
});

test('an updated rental replaces the old one with the server\'s version', async () => {
  fetchAll.mockResolvedValue([rental(1, 21), rental(2, 20)]);
  update.mockResolvedValue({ ...rental(1, 21), eventDate: '2027-01-05' });
  const store = newStore();
  await store.dispatch(getRentals());

  await store.dispatch(updateRentalThunk({ rentalId: 1, customerId: 'C000001', gownId: 21, eventDate: '2027-01-05' }));

  expect(store.getState().rentals.rentals.map(r => r.eventDate)).toEqual(['2027-01-05', '2026-12-24']);
});

test('a refused update reports the reason and keeps the list unchanged', async () => {
  fetchAll.mockResolvedValue([rental(1)]);
  update.mockRejectedValue({ response: { status: 404, data: { detail: 'Gown 9999 does not exist.' } } });
  const store = newStore();
  await store.dispatch(getRentals());

  const result = await store.dispatch(updateRentalThunk({ rentalId: 1, customerId: 'C000001', gownId: 9999, eventDate: '2026-12-24' }));

  expect(result.payload).toBe('Gown 9999 does not exist.');
  expect(store.getState().rentals.rentals).toEqual([rental(1)]);
});

test('deleting removes the rental, and a refusal shows a dismissible message', async () => {
  fetchAll.mockResolvedValue([rental(1), rental(2)]);
  remove.mockResolvedValueOnce(undefined);
  remove.mockRejectedValueOnce({ response: { status: 404, data: { detail: 'Rental 2 does not exist.' } } });
  const store = newStore();
  await store.dispatch(getRentals());

  await store.dispatch(deleteRentalThunk(1));
  expect(store.getState().rentals.rentals.map(r => r.rentalId)).toEqual([2]);

  await store.dispatch(deleteRentalThunk(2));
  expect(store.getState().rentals.rentals.map(r => r.rentalId)).toEqual([2]);
  expect(store.getState().rentals.notice).toEqual({ type: 'error', text: 'Rental 2 does not exist.' });

  store.dispatch(dismissNotice());
  expect(store.getState().rentals.notice).toBeNull();
});

test('refreshing a list that is already shown does not bring the spinner back', async () => {
  fetchAll.mockResolvedValue([rental(1)]);
  const store = newStore();
  await store.dispatch(getRentals());
  expect(store.getState().rentals.isLoading).toBe(false);

  store.dispatch({ type: getRentals.pending.type });

  expect(store.getState().rentals.isLoading).toBe(false);
});
