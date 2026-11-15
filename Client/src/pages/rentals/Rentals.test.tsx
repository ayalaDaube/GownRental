import { fireEvent, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import Rentals from './rentalview/Rentals';
import RentalsAside from './rentalAside/RentalsAside';
import { refused, renderWithApp, rentals, signedInState } from '../../testUtils';
import {
  createRentalApi, deleteRentalApi, fetchAllRentals, fetchRenting, fetchReturning, updateRentalApi,
} from '../../services/Rentals/task.service';

jest.mock('../../services/Rentals/task.service');

const loadRentals = fetchAllRentals as jest.Mock;
const loadReturning = fetchReturning as jest.Mock;
const loadRenting = fetchRenting as jest.Mock;
const createRental = createRentalApi as jest.Mock;
const updateRental = updateRentalApi as jest.Mock;
const deleteRental = deleteRentalApi as jest.Mock;

let confirmAnswer = true;

beforeEach(() => {
  jest.resetAllMocks();
  loadRentals.mockResolvedValue(rentals);
  loadReturning.mockResolvedValue([]);
  loadRenting.mockResolvedValue([]);
  confirmAnswer = true;
  jest.spyOn(window, 'confirm').mockImplementation(() => confirmAnswer);
});

const openPage = async () => {
  const view = renderWithApp(<><Rentals /><RentalsAside /></>, { route: '/rentals', preloadedState: signedInState });
  await screen.findByText('C000001');
  return view;
};

describe('the list of rentals', () => {
  test('every rental is shown with its customer, dates, gown and size', async () => {
    await openPage();

    expect(screen.getByText('C000001')).toBeInTheDocument();
    expect(screen.getByText('C000002')).toBeInTheDocument();
    expect(screen.getByText('24/12/2026')).toBeInTheDocument();
    expect(screen.getByText('30/12/2026')).toBeInTheDocument();
    expect(screen.getByText('01/11/2026')).toBeInTheDocument(); // the rental date, without the time
    expect(screen.getAllByText('Rental ID')).toHaveLength(2);
  });

  test('when the rentals cannot be loaded the user is told and can try again', async () => {
    loadRentals.mockRejectedValueOnce(new Error('Network Error'));
    renderWithApp(<Rentals />, { preloadedState: signedInState });

    expect(await screen.findByRole('alert')).toHaveTextContent('Could not load the rentals');
    userEvent.click(screen.getByRole('button', { name: 'Try again' }));

    expect(await screen.findByText('C000001')).toBeInTheDocument();
  });

  test('with no rentals the page says so', async () => {
    loadRentals.mockResolvedValue([]);
    renderWithApp(<Rentals />, { preloadedState: signedInState });

    expect(await screen.findByText('There are no rentals yet.')).toBeInTheDocument();
  });
});

describe('adding a rental', () => {
  const openForm = async () => {
    await openPage();
    userEvent.click(screen.getByRole('button', { name: '+ New Rental' }));
  };

  const fill = (customerId: string, eventDate: string, gownId: string) => {
    userEvent.type(screen.getByLabelText('Customer ID'), customerId);
    fireEvent.change(screen.getByLabelText('Event Date'), { target: { value: eventDate } });
    userEvent.type(screen.getByLabelText('Gown ID'), gownId);
  };

  test('a registered customer is rented a gown with just three details', async () => {
    createRental.mockResolvedValue({ ...rentals[0], rentalId: 3 });
    await openForm();
    fill('  C000003 ', '2027-03-05', '21');

    userEvent.click(screen.getByRole('button', { name: 'Add' }));

    await waitFor(() => expect(createRental).toHaveBeenCalledWith({
      customerId: 'C000003', eventDate: '2027-03-05', gownId: 21,
      customerName: undefined, customerPhone: undefined, customerEmail: undefined,
    }));
    await waitFor(() => expect(screen.queryByLabelText('Customer ID')).not.toBeInTheDocument());
    expect(loadRentals.mock.calls.length).toBeGreaterThan(1);   // the list was reloaded
  });

  test('a new customer is registered with the details typed in the same form', async () => {
    createRental.mockResolvedValue({ ...rentals[0], rentalId: 3 });
    await openForm();
    fill('NEWBIE123', '2027-03-05', '21');
    userEvent.type(screen.getByLabelText('Name'), 'Dana Levi');
    userEvent.type(screen.getByLabelText('Phone'), '050-123-4567');
    userEvent.type(screen.getByLabelText('Email (optional)'), 'dana@example.com');

    userEvent.click(screen.getByRole('button', { name: 'Add' }));

    await waitFor(() => expect(createRental).toHaveBeenCalledWith(expect.objectContaining({
      customerId: 'NEWBIE123', customerName: 'Dana Levi', customerPhone: '050-123-4567', customerEmail: 'dana@example.com',
    })));
  });

  test.each([
    [409, 'Gown 21 is already rented for an event on 2027-03-03. Events for the same gown must be more than 3 days apart.'],
    [400, 'Customer NEWBIE123 is not registered yet. Enter a name and a phone number to register them.'],
    [404, 'Gown 9999 does not exist.'],
  ])('a refused rental (%i) shows the reason and keeps everything that was typed', async (status, reason) => {
    createRental.mockRejectedValue(refused(status, reason));
    await openForm();
    fill('NEWBIE123', '2027-03-05', '21');

    userEvent.click(screen.getByRole('button', { name: 'Add' }));

    expect(await screen.findByRole('alert')).toHaveTextContent(reason);
    expect(screen.getByLabelText('Customer ID')).toHaveValue('NEWBIE123');
    expect(screen.getByLabelText('Gown ID')).toHaveValue(21);
    expect(screen.getByRole('button', { name: 'Add' })).toBeEnabled();
    expect(screen.queryByText('NEWBIE123', { selector: '.rental-card *' })).not.toBeInTheDocument();
  });

  test('the form does not ask for things the server decides: the booking time and the size', async () => {
    await openForm();

    expect(screen.queryByLabelText(/rental date/i)).not.toBeInTheDocument();
    expect(screen.queryByLabelText(/^size$/i)).not.toBeInTheDocument();
  });

  test('cancelling closes the form without sending anything', async () => {
    await openForm();

    userEvent.click(screen.getByRole('button', { name: 'Cancel' }));

    expect(screen.queryByLabelText('Customer ID')).not.toBeInTheDocument();
    expect(createRental).not.toHaveBeenCalled();
  });
});

describe('changing and deleting a rental', () => {
  test('the customer, date and gown of a rental are changed in one form', async () => {
    updateRental.mockResolvedValue({ ...rentals[0], eventDate: '2027-01-05' });
    await openPage();

    userEvent.click(screen.getAllByTitle('Update')[0]);
    expect(screen.getByRole('heading', { name: 'Update Rental #1' })).toBeInTheDocument();
    expect(screen.getByLabelText('Customer ID')).toHaveValue('C000001');
    expect(screen.getByLabelText('Event Date')).toHaveValue('2026-12-24');
    expect(screen.getByLabelText('Gown ID')).toHaveValue(1);
    fireEvent.change(screen.getByLabelText('Event Date'), { target: { value: '2027-01-05' } });
    userEvent.click(screen.getByRole('button', { name: 'Save' }));

    await waitFor(() => expect(updateRental).toHaveBeenCalledWith({
      rentalId: 1, customerId: 'C000001', eventDate: '2027-01-05', gownId: 1,
    }));
    expect(await screen.findByText('05/01/2027')).toBeInTheDocument();
  });

  test('a refused change shows the reason and leaves the rental as it was', async () => {
    updateRental.mockRejectedValue(refused(409, 'Gown 3 is already rented for an event on 2026-12-29.'));
    await openPage();
    userEvent.click(screen.getAllByTitle('Update')[0]);
    userEvent.clear(screen.getByLabelText('Gown ID'));
    userEvent.type(screen.getByLabelText('Gown ID'), '3');

    userEvent.click(screen.getByRole('button', { name: 'Save' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Gown 3 is already rented');
    expect(screen.getByRole('button', { name: 'Save' })).toBeEnabled();
    userEvent.click(screen.getByRole('button', { name: 'Cancel' }));
    expect(screen.getByText('24/12/2026')).toBeInTheDocument();
  });

  test('editing and cancelling shows the original values again next time', async () => {
    await openPage();
    userEvent.click(screen.getAllByTitle('Update')[0]);
    userEvent.clear(screen.getByLabelText('Customer ID'));
    userEvent.type(screen.getByLabelText('Customer ID'), 'SOMEONE99');
    userEvent.click(screen.getByRole('button', { name: 'Cancel' }));

    userEvent.click(screen.getAllByTitle('Update')[0]);

    expect(screen.getByLabelText('Customer ID')).toHaveValue('C000001');
  });

  test('nothing is deleted when the user does not confirm', async () => {
    confirmAnswer = false;
    await openPage();

    userEvent.click(screen.getAllByTitle('Delete')[0]);

    expect(deleteRental).not.toHaveBeenCalled();
    expect(screen.getByText('C000001')).toBeInTheDocument();
  });

  test('a confirmed deletion removes the rental from the list', async () => {
    deleteRental.mockResolvedValue(undefined);
    await openPage();

    userEvent.click(screen.getAllByTitle('Delete')[0]);

    await waitFor(() => expect(deleteRental).toHaveBeenCalledWith(1));
    await waitFor(() => expect(screen.queryByText('C000001')).not.toBeInTheDocument());
    expect(screen.getByText('C000002')).toBeInTheDocument();
  });

  test('a deletion the server refuses keeps the rental and says why', async () => {
    deleteRental.mockRejectedValue(refused(404, 'Rental 1 does not exist.'));
    await openPage();

    userEvent.click(screen.getAllByTitle('Delete')[0]);

    expect(await screen.findByRole('alert')).toHaveTextContent('Rental 1 does not exist.');
    expect(screen.getByText('C000001')).toBeInTheDocument();
  });
});

describe('what happens today', () => {
  test('gowns coming back and gowns being picked up are listed apart, with who and which', async () => {
    loadReturning.mockResolvedValue([rentals[0]]);
    loadRenting.mockResolvedValue([rentals[1]]);
    await openPage();

    expect(await screen.findByText('New for today')).toBeInTheDocument();
    expect(await screen.findByText('Rental Id: 1')).toBeInTheDocument();
    expect(screen.getByText('Rental Id: 2')).toBeInTheDocument();
    expect(screen.getByText('Gown Id: 3')).toBeInTheDocument();
  });

  test('a quiet day says there is nothing to return or pick up', async () => {
    await openPage();

    expect(await screen.findByText('No returns today')).toBeInTheDocument();
    expect(screen.getByText('No pick ups today')).toBeInTheDocument();
  });
});
