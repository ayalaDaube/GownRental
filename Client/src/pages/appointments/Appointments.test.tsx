import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import Appointments from './appointments';
import EmployeeAppointments from './EmployeeAppointments';
import { freeSlots, refused, renderWithApp, signedInState } from '../../testUtils';
import { AppointmentType } from '../../redux/Appointments/AppointmentType';
import {
  bookAppointmentApi, deleteAppointmentApi, fetchAllAppointmentsApi, fetchAvailableAppointmentsApi,
} from '../../services/Appointments/task.service';

jest.mock('../../services/Appointments/task.service');

const loadFree = fetchAvailableAppointmentsApi as jest.Mock;
const loadAll = fetchAllAppointmentsApi as jest.Mock;
const book = bookAppointmentApi as jest.Mock;
const cancel = deleteAppointmentApi as jest.Mock;

let confirmAnswer = true;

beforeEach(() => {
  jest.resetAllMocks();
  loadFree.mockResolvedValue(freeSlots);
  loadAll.mockResolvedValue([]);
  confirmAnswer = true;
  jest.spyOn(window, 'confirm').mockImplementation(() => confirmAnswer);
});

describe('booking an appointment as a visitor', () => {
  const openFirstSlot = async () => {
    renderWithApp(<Appointments />, { route: '/appointments' });
    const buttons = await screen.findAllByRole('button', { name: 'הזמן תור' });
    userEvent.click(buttons[0]);
    return screen.getByRole('heading', { name: 'הזמנת תור' });
  };

  const fillId = (id: string) => userEvent.type(screen.getByLabelText('תעודת זהות'), id);

  test('every free slot is listed with its date and hour, and the visitor needs no account', async () => {
    renderWithApp(<Appointments />, { route: '/appointments' });

    expect(await screen.findByText('09:00:00')).toBeInTheDocument();
    expect(screen.getByText('10:00:00')).toBeInTheDocument();
    expect(screen.getAllByText('2026-12-01')).toHaveLength(2);
    expect(screen.getAllByRole('button', { name: 'הזמן תור' })).toHaveLength(2);
  });

  test('the Hebrew pages are laid out from right to left', async () => {
    renderWithApp(<Appointments />, { route: '/appointments' });
    await screen.findByText('09:00:00');

    expect(screen.getByTestId('appointments-page')).toHaveAttribute('dir', 'rtl');
    expect(screen.getByTestId('appointments-page')).toHaveAttribute('lang', 'he');
  });

  test('when there are no free slots the page says so', async () => {
    loadFree.mockResolvedValue([]);
    renderWithApp(<Appointments />, { route: '/appointments' });

    expect(await screen.findByText('אין תורים פנויים')).toBeInTheDocument();
  });

  test('when the slots cannot be loaded the page does not claim there are none', async () => {
    loadFree.mockRejectedValueOnce(new Error('Network Error'));
    renderWithApp(<Appointments />, { route: '/appointments' });

    expect(await screen.findByRole('alert')).toHaveTextContent('לא ניתן לטעון את התורים הפנויים');
    expect(screen.queryByText('אין תורים פנויים')).not.toBeInTheDocument();
    userEvent.click(screen.getByRole('button', { name: 'נסו שוב' }));
    expect(await screen.findByText('09:00:00')).toBeInTheDocument();
  });

  test('the form states the limits of what can be typed', async () => {
    await openFirstSlot();

    expect(screen.getByLabelText('תעודת זהות')).toHaveAttribute('minlength', '5');
    expect(screen.getByLabelText('תעודת זהות')).toHaveAttribute('maxlength', '9');
    expect(screen.getByLabelText('תעודת זהות')).toBeRequired();
    expect(screen.getByLabelText('מספר אנשים')).toHaveAttribute('min', '1');
    expect(screen.getByLabelText('מספר אנשים')).toHaveAttribute('max', '10');
  });

  test('a registered customer books with an id and a number of people, and gets a confirmation without personal details', async () => {
    book.mockResolvedValue(undefined);
    await openFirstSlot();

    fillId('C000001');
    userEvent.clear(screen.getByLabelText('מספר אנשים'));
    userEvent.type(screen.getByLabelText('מספר אנשים'), '3');
    loadFree.mockResolvedValue([freeSlots[1]]);
    userEvent.click(screen.getByRole('button', { name: 'אשר תור' }));

    expect(await screen.findByText('התור נקבע בהצלחה!')).toBeInTheDocument();
    expect(book).toHaveBeenCalledWith({
      appointmentDate: '2026-12-01', hour: '09:00:00', customerId: 'C000001', numberOfPeople: 3,
      customerName: undefined, customerPhone: undefined, customerEmail: undefined,
    });
    expect(screen.getByText('C000001')).toBeInTheDocument();
    expect(screen.getByText('3')).toBeInTheDocument();
    // the confirmation does not repeat a name, phone or email
    expect(screen.queryByText('שם')).not.toBeInTheDocument();
    expect(screen.queryByText('טלפון')).not.toBeInTheDocument();
    await waitFor(() => expect(screen.getAllByRole('button', { name: 'הזמן תור' })).toHaveLength(1));
  });

  test('a new customer sends their name, phone and email with the booking', async () => {
    book.mockResolvedValue(undefined);
    await openFirstSlot();

    fillId('NEWBIE123');
    userEvent.type(screen.getByLabelText('שם מלא (ללקוח חדש)'), 'דנה לוי');
    userEvent.type(screen.getByLabelText('טלפון (ללקוח חדש)'), '050-123-4567');
    userEvent.type(screen.getByLabelText('אימייל'), 'dana@example.com');
    userEvent.click(screen.getByRole('button', { name: 'אשר תור' }));

    await screen.findByText('התור נקבע בהצלחה!');
    expect(book).toHaveBeenCalledWith(expect.objectContaining({
      customerId: 'NEWBIE123', customerName: 'דנה לוי', customerPhone: '050-123-4567', customerEmail: 'dana@example.com',
    }));
  });

  test('a booking the server refuses is never shown as a success: the reason is shown and the list is refreshed', async () => {
    book.mockRejectedValue(refused(409, 'This appointment is already booked. Please choose another time.'));
    await openFirstSlot();
    fillId('C000001');
    loadFree.mockClear();
    loadFree.mockResolvedValue([freeSlots[1]]);

    userEvent.click(screen.getByRole('button', { name: 'אשר תור' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('This appointment is already booked');
    expect(screen.queryByText('התור נקבע בהצלחה!')).not.toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'הזמנת תור' })).toBeInTheDocument();
    await waitFor(() => expect(loadFree).toHaveBeenCalled());
  });

  test('a mistake in the details can be corrected and sent again from the same form', async () => {
    book.mockRejectedValueOnce(refused(400, 'The customer id must be 5 to 9 letters or digits.'));
    await openFirstSlot();
    fillId('12345');
    userEvent.click(screen.getByRole('button', { name: 'אשר תור' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('5 to 9');

    book.mockResolvedValueOnce(undefined);
    userEvent.click(screen.getByRole('button', { name: 'אשר תור' }));

    expect(await screen.findByText('התור נקבע בהצלחה!')).toBeInTheDocument();
    expect(book).toHaveBeenCalledTimes(2);
  });

  test('the button cannot be pressed twice while the booking is being saved', async () => {
    let finish: (value?: unknown) => void = () => undefined;
    book.mockReturnValue(new Promise(resolve => { finish = resolve; }));
    await openFirstSlot();
    fillId('C000001');

    userEvent.click(screen.getByRole('button', { name: 'אשר תור' }));

    expect(await screen.findByRole('button', { name: 'שומר…' })).toBeDisabled();
    expect(book).toHaveBeenCalledTimes(1);
    finish();
    await screen.findByText('התור נקבע בהצלחה!');
  });

  test('cancelling the form sends nothing and returns to the list', async () => {
    await openFirstSlot();

    userEvent.click(screen.getByRole('button', { name: 'ביטול' }));

    expect(book).not.toHaveBeenCalled();
    expect(screen.queryByRole('heading', { name: 'הזמנת תור' })).not.toBeInTheDocument();
  });

  test('after a confirmation the dialog closes with the close button', async () => {
    book.mockResolvedValue(undefined);
    await openFirstSlot();
    fillId('C000001');
    userEvent.click(screen.getByRole('button', { name: 'אשר תור' }));
    await screen.findByText('התור נקבע בהצלחה!');

    userEvent.click(screen.getByRole('button', { name: 'סגור' }));

    expect(screen.queryByText('התור נקבע בהצלחה!')).not.toBeInTheDocument();
  });
});

describe('the staff view of the appointments', () => {
  const booked: AppointmentType = {
    id: 5, appointmentDate: '2026-12-01', hour: '11:00:00', customerId: 'C000007', numberOfPeople: 3,
    isAvailable: false, customerName: 'Grace Lee', customerPhone: '0507890123',
  };
  const later: AppointmentType = { ...freeSlots[0], id: 6, appointmentDate: '2026-12-02', hour: '09:00:00' };

  const openStaffPage = async () => {
    loadAll.mockResolvedValue([booked, ...freeSlots, later]);
    renderWithApp(<EmployeeAppointments />, { route: '/employee/appointments', preloadedState: signedInState });
    await screen.findByText('Grace Lee');
  };

  test('the days are separate, the booked slots show who booked, the free ones are marked free', async () => {
    await openStaffPage();

    expect(screen.getByRole('heading', { name: '2026-12-01' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: '2026-12-02' })).toBeInTheDocument();
    expect(screen.getByText('0507890123')).toBeInTheDocument();
    expect(screen.getByText('C000007')).toBeInTheDocument();
    expect(screen.getAllByText('תפוס')).toHaveLength(1);
    expect(screen.getAllByText('פנוי')).toHaveLength(3);
    // only a booked slot can be cancelled
    expect(screen.getAllByRole('button', { name: 'בטל' })).toHaveLength(1);
  });

  test('typing an id number shows only that customer', async () => {
    await openStaffPage();

    userEvent.type(screen.getByPlaceholderText('חיפוש לפי תעודת זהות'), 'C000007');

    expect(screen.getByText('Grace Lee')).toBeInTheDocument();
    expect(screen.queryAllByText('פנוי')).toHaveLength(0);
  });

  test('a confirmed cancellation frees the slot and the list is reloaded', async () => {
    cancel.mockResolvedValue(undefined);
    await openStaffPage();
    loadAll.mockResolvedValue([{ ...booked, isAvailable: true, customerId: '', customerName: undefined, customerPhone: undefined, numberOfPeople: null }, ...freeSlots, later]);

    userEvent.click(screen.getByRole('button', { name: 'בטל' }));

    await waitFor(() => expect(cancel).toHaveBeenCalledWith(5));
    expect(await screen.findByRole('status')).toHaveTextContent('התור בוטל.');
    await waitFor(() => expect(screen.queryByText('Grace Lee')).not.toBeInTheDocument());
    expect(screen.getAllByText('פנוי')).toHaveLength(4);
  });

  test('nothing is cancelled when the secretary does not confirm', async () => {
    confirmAnswer = false;
    await openStaffPage();

    userEvent.click(screen.getByRole('button', { name: 'בטל' }));

    expect(cancel).not.toHaveBeenCalled();
    expect(screen.getByText('Grace Lee')).toBeInTheDocument();
  });

  test('a cancellation the server refuses shows the reason and keeps the booking', async () => {
    cancel.mockRejectedValue(refused(409, 'Appointment 5 is not booked.'));
    await openStaffPage();

    userEvent.click(screen.getByRole('button', { name: 'בטל' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Appointment 5 is not booked.');
    expect(screen.getByText('Grace Lee')).toBeInTheDocument();
  });

  test('the staff appointments page is laid out from right to left too', async () => {
    renderWithApp(<EmployeeAppointments />, { route: '/employee/appointments', preloadedState: signedInState });
    await screen.findByText('אין תורים');

    expect(screen.getByTestId('appointments-page')).toHaveAttribute('dir', 'rtl');
  });

  test('with no appointments the page says so, and a load failure is not shown as an empty day', async () => {
    renderWithApp(<EmployeeAppointments />, { route: '/employee/appointments', preloadedState: signedInState });
    expect(await screen.findByText('אין תורים')).toBeInTheDocument();
  });

  test('a load failure is reported with a retry button', async () => {
    loadAll.mockRejectedValueOnce(new Error('Network Error'));
    renderWithApp(<EmployeeAppointments />, { route: '/employee/appointments', preloadedState: signedInState });

    const alert = await screen.findByRole('alert');
    expect(within(alert).getByText(/לא ניתן לטעון/)).toBeInTheDocument();
    expect(screen.queryByText('אין תורים')).not.toBeInTheDocument();
  });
});
