import { configureStore } from '@reduxjs/toolkit';
import appointmentReducer, {
  bookAppointmentThunk, deleteAppointmentThunk, dismissNotice, fetchAvailableAppointments,
} from './AppointmentSlice';
import { bookAppointmentApi, deleteAppointmentApi, fetchAvailableAppointmentsApi } from '../../services/Appointments/task.service';

// The services are replaced so no network is used (and axios is not loaded by the test runner).
jest.mock('../../services/Appointments/task.service', () => ({
  fetchAvailableAppointmentsApi: jest.fn(),
  fetchAllAppointmentsApi: jest.fn(),
  fetchAppointmentsByDateApi: jest.fn(),
  bookAppointmentApi: jest.fn(),
  deleteAppointmentApi: jest.fn(),
}));

const fetchAvailable = fetchAvailableAppointmentsApi as jest.Mock;
const book = bookAppointmentApi as jest.Mock;
const cancel = deleteAppointmentApi as jest.Mock;

const slot = (id: number) => ({
  id, appointmentDate: '2026-12-01', hour: `${9 + id}:00:00`, customerId: '', numberOfPeople: null, isAvailable: true,
});

const booking = {
  appointmentDate: '2026-12-01', hour: '10:00:00', customerId: 'C000001', numberOfPeople: 2,
};

const newStore = () => configureStore({ reducer: { appointments: appointmentReducer } });

beforeEach(() => jest.resetAllMocks());

test('a booked slot is removed from the list of free slots', async () => {
  fetchAvailable.mockResolvedValue([slot(1), slot(2)]);
  book.mockResolvedValue(undefined);
  const store = newStore();
  await store.dispatch(fetchAvailableAppointments());

  const result = await store.dispatch(bookAppointmentThunk({ slotId: 1, booking }));

  expect(bookAppointmentThunk.fulfilled.match(result)).toBe(true);
  expect(store.getState().appointments.available.map(s => s.id)).toEqual([2]);
});

test('a booking the server refuses is reported as a failure with the reason, never as a success', async () => {
  fetchAvailable.mockResolvedValue([slot(1)]);
  book.mockRejectedValue({ response: { status: 409, data: { detail: 'This appointment is already booked. Please choose another time.' } } });
  const store = newStore();
  await store.dispatch(fetchAvailableAppointments());

  const result = await store.dispatch(bookAppointmentThunk({ slotId: 1, booking }));

  expect(bookAppointmentThunk.rejected.match(result)).toBe(true);
  expect(result.payload).toBe('This appointment is already booked. Please choose another time.');
  expect(store.getState().appointments.available.map(s => s.id)).toEqual([1]);
});

test('an unreachable server is reported with a clear message', async () => {
  book.mockRejectedValue(new Error('Network Error'));
  const store = newStore();

  const result = await store.dispatch(bookAppointmentThunk({ slotId: 1, booking }));

  expect(result.payload).toMatch(/could not reach the server/i);
});

test('cancelling reports success, and a refusal shows a message that can be dismissed', async () => {
  cancel.mockResolvedValueOnce(undefined);
  cancel.mockRejectedValueOnce({ response: { status: 409, data: { detail: 'Appointment 5 is not booked.' } } });
  const store = newStore();

  await store.dispatch(deleteAppointmentThunk(4));
  expect(store.getState().appointments.notice).toEqual({ type: 'success', text: 'התור בוטל.' });

  await store.dispatch(deleteAppointmentThunk(5));
  expect(store.getState().appointments.notice).toEqual({ type: 'error', text: 'Appointment 5 is not booked.' });

  store.dispatch(dismissNotice());
  expect(store.getState().appointments.notice).toBeNull();
});
