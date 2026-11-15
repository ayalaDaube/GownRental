import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import Gowns from './Gowns';
import { aGown, gownModels, refused, renderWithApp, signedInState } from '../../testUtils';
import {
  createGownApi, deleteGownApi, deleteGownModelApi, fetchAllGroupsGowns, fetchAvailableGownsByDate,
  updateGownModelApi, updateGownSizeApi,
} from '../../services/Gowns/task.service';
import { placeOrderApi } from '../../services/Orders/task.service';

jest.mock('../../services/Gowns/task.service');
jest.mock('../../services/Orders/task.service');

const loadGowns = fetchAllGroupsGowns as jest.Mock;
const loadByDate = fetchAvailableGownsByDate as jest.Mock;
const createGown = createGownApi as jest.Mock;
const deleteSize = deleteGownApi as jest.Mock;
const deleteModel = deleteGownModelApi as jest.Mock;
const updateSize = updateGownSizeApi as jest.Mock;
const updateModel = updateGownModelApi as jest.Mock;
const placeOrder = placeOrderApi as jest.Mock;

const openPage = async () => {
  const view = renderWithApp(<Gowns />, { route: '/gowns', preloadedState: signedInState });
  await screen.findByText('Simple Gown');
  return view;
};

const open = (modelName: string) => userEvent.click(screen.getByText(modelName));

let confirmAnswer = true;

beforeEach(() => {
  jest.resetAllMocks();
  loadGowns.mockResolvedValue(gownModels);
  confirmAnswer = true;
  jest.spyOn(window, 'confirm').mockImplementation(() => confirmAnswer);
});

describe('the list', () => {
  test('every model is shown with its colour', async () => {
    await openPage();

    expect(screen.getByText('Simple Gown')).toBeInTheDocument();
    expect(screen.getByText('Evening Gown')).toBeInTheDocument();
    expect(screen.getByText('blue')).toBeInTheDocument();
    expect(screen.getByText('black')).toBeInTheDocument();
  });

  test('a spinner is shown while the gowns load', () => {
    loadGowns.mockReturnValue(new Promise(() => undefined));
    renderWithApp(<Gowns />, { preloadedState: signedInState });

    expect(screen.queryByText('Simple Gown')).not.toBeInTheDocument();
    expect(screen.queryByText('Gowns Collection')).not.toBeInTheDocument();
  });

  test('when the gowns cannot be loaded the user is told so and can try again', async () => {
    loadGowns.mockRejectedValueOnce(new Error('Network Error'));
    renderWithApp(<Gowns />, { preloadedState: signedInState });

    expect(await screen.findByRole('alert')).toHaveTextContent('Could not load the gowns');
    userEvent.click(screen.getByRole('button', { name: 'Try again' }));

    expect(await screen.findByText('Simple Gown')).toBeInTheDocument();
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
  });

  test('with no gowns at all the page invites adding the first one', async () => {
    loadGowns.mockResolvedValue([]);
    renderWithApp(<Gowns />, { preloadedState: signedInState });

    expect(await screen.findByText(/no gowns yet/i)).toBeInTheDocument();
  });
});

describe('searching', () => {
  test('typing a model id or a gown id keeps only the matching models', async () => {
    await openPage();

    userEvent.type(screen.getByPlaceholderText('Search by ID or Gown ID'), '2');
    expect(screen.getByText('Simple Gown')).toBeInTheDocument();     // gown 2 belongs to model 1
    expect(screen.getByText('Evening Gown')).toBeInTheDocument();    // model 2

    userEvent.clear(screen.getByPlaceholderText('Search by ID or Gown ID'));
    userEvent.type(screen.getByPlaceholderText('Search by ID or Gown ID'), '3');
    expect(screen.queryByText('Simple Gown')).not.toBeInTheDocument();
    expect(screen.getByText('Evening Gown')).toBeInTheDocument();    // gown 3
  });

  test('typing a size keeps only the models that have it', async () => {
    await openPage();

    userEvent.type(screen.getByPlaceholderText('Search by Size'), '40');

    expect(screen.queryByText('Simple Gown')).not.toBeInTheDocument();
    expect(screen.getByText('Evening Gown')).toBeInTheDocument();
  });

  test('both searches work together and clearing them brings everything back', async () => {
    await openPage();

    userEvent.type(screen.getByPlaceholderText('Search by ID or Gown ID'), '1');
    userEvent.type(screen.getByPlaceholderText('Search by Size'), '40');
    expect(screen.getByText(/no gowns match your search/i)).toBeInTheDocument();

    userEvent.clear(screen.getByPlaceholderText('Search by ID or Gown ID'));
    userEvent.clear(screen.getByPlaceholderText('Search by Size'));
    expect(screen.getByText('Simple Gown')).toBeInTheDocument();
    expect(screen.getByText('Evening Gown')).toBeInTheDocument();
  });
});

describe('availability on a date', () => {
  test('checking a date asks the server for that date and shows the answer', async () => {
    loadByDate.mockResolvedValue([
      { ...gownModels[0], details: [aGown(1, 1, 36, { isAvailable: false }), aGown(2, 1, 38)] },
    ]);
    await openPage();

    fireEvent.change(screen.getByLabelText('Event date to check'), { target: { value: '2027-03-05' } });
    userEvent.click(screen.getByRole('button', { name: 'Check Availability' }));

    await waitFor(() => expect(loadByDate).toHaveBeenCalledWith('2027-03-05'));
    expect(await screen.findByText('Simple Gown')).toBeInTheDocument();
    expect(screen.queryByText('Evening Gown')).not.toBeInTheDocument();
    open('Simple Gown');
    expect(screen.getByText('Rented on that date')).toBeInTheDocument();
    expect(screen.getAllByTitle('Not available on that date')[0]).toBeDisabled();
  });

  test('checking with no date shows all the gowns again', async () => {
    await openPage();
    loadGowns.mockClear();

    userEvent.click(screen.getByRole('button', { name: 'Check Availability' }));

    await waitFor(() => expect(loadGowns).toHaveBeenCalledTimes(1));
    expect(loadByDate).not.toHaveBeenCalled();
  });

  test('a size taken out of stock is shown as out of stock and cannot be added to the cart', async () => {
    loadGowns.mockResolvedValue([{ ...gownModels[0], details: [aGown(1, 1, 36, { isActive: false, isAvailable: false })] }]);
    await openPage();

    open('Simple Gown');

    expect(screen.getByText('Out of stock')).toBeInTheDocument();
    expect(screen.getByTitle('Out of stock')).toBeDisabled();
  });

  test('when the date cannot be checked the user is told', async () => {
    loadByDate.mockRejectedValue(new Error('Network Error'));
    await openPage();

    fireEvent.change(screen.getByLabelText('Event date to check'), { target: { value: '2027-03-05' } });
    userEvent.click(screen.getByRole('button', { name: 'Check Availability' }));

    expect(await screen.findByRole('alert')).toHaveTextContent(/could not check which gowns are available/i);
  });
});

describe('the cart and the order', () => {
  const addFirstSizeToCart = async () => {
    await openPage();
    open('Simple Gown');
    userEvent.click(screen.getAllByTitle('Add to cart')[0]);
  };

  test('adding a size puts it in the cart and adding it again takes it out', async () => {
    await addFirstSizeToCart();
    expect(screen.getByRole('button', { name: 'Place Order (1)' })).toBeInTheDocument();

    userEvent.click(screen.getByTitle('Remove from cart'));

    expect(screen.getByRole('button', { name: 'Place Order (0)' })).toBeInTheDocument();
  });

  test('an empty cart cannot be ordered', async () => {
    await openPage();

    userEvent.click(screen.getByRole('button', { name: 'Place Order (0)' }));

    expect(screen.getByText(/cart is empty/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Confirm' })).toBeDisabled();
  });

  test('the order form lists the gowns and offers the date that was checked', async () => {
    await openPage();
    fireEvent.change(screen.getByLabelText('Event date to check'), { target: { value: '2027-03-05' } });
    loadByDate.mockResolvedValue(gownModels);
    userEvent.click(screen.getByRole('button', { name: 'Check Availability' }));
    await screen.findByText('Simple Gown');
    open('Simple Gown');
    userEvent.click(screen.getAllByTitle('Add to cart')[0]);

    userEvent.click(screen.getByRole('button', { name: 'Place Order (1)' }));

    expect(screen.getByText(/#1 \(Simple Gown, size 36\)/)).toBeInTheDocument();
    expect(screen.getByTitle('Event date')).toHaveValue('2027-03-05');
  });

  test('a successful order sends all the gowns in one request, empties the cart and says so', async () => {
    placeOrder.mockResolvedValue({ rentals: [] });
    await openPage();
    open('Simple Gown');
    userEvent.click(screen.getAllByTitle('Add to cart')[0]);
    userEvent.click(screen.getAllByTitle('Add to cart')[0]);   // the second size (the first one now says "Remove")
    userEvent.click(screen.getByRole('button', { name: 'Place Order (2)' }));

    userEvent.type(screen.getByPlaceholderText('Customer ID'), ' C000001 ');
    fireEvent.change(screen.getByTitle('Event date'), { target: { value: '2027-03-05' } });
    userEvent.click(screen.getByRole('button', { name: 'Confirm' }));

    await waitFor(() => expect(placeOrder).toHaveBeenCalledTimes(1));
    expect(placeOrder).toHaveBeenCalledWith({
      customerId: 'C000001', eventDate: '2027-03-05', gownIds: [1, 2],
      customerName: undefined, customerPhone: undefined, customerEmail: undefined,
    });
    expect(await screen.findByRole('status')).toHaveTextContent('The order was placed: 2 gowns for 2027-03-05.');
    expect(screen.getByRole('button', { name: 'Place Order (0)' })).toBeInTheDocument();
    expect(screen.queryByPlaceholderText('Customer ID')).not.toBeInTheDocument();
  });

  test('details of a new customer are sent only when they are typed', async () => {
    placeOrder.mockResolvedValue({ rentals: [] });
    await addFirstSizeToCart();
    userEvent.click(screen.getByRole('button', { name: 'Place Order (1)' }));

    userEvent.type(screen.getByPlaceholderText('Customer ID'), 'NEWBIE123');
    fireEvent.change(screen.getByTitle('Event date'), { target: { value: '2027-03-05' } });
    userEvent.type(screen.getByPlaceholderText('Name (new customers only)'), 'Dana Levi');
    userEvent.type(screen.getByPlaceholderText('Phone (new customers only)'), '050-123-4567');
    userEvent.click(screen.getByRole('button', { name: 'Confirm' }));

    await waitFor(() => expect(placeOrder).toHaveBeenCalled());
    expect(placeOrder.mock.calls[0][0]).toMatchObject({ customerName: 'Dana Levi', customerPhone: '050-123-4567', customerEmail: undefined });
  });

  test('a refused order shows the reason inside the form, keeps the cart and can be sent again', async () => {
    placeOrder.mockRejectedValueOnce(refused(409, 'Gown 1 is already rented for an event on 2027-03-03.'));
    await addFirstSizeToCart();
    userEvent.click(screen.getByRole('button', { name: 'Place Order (1)' }));
    userEvent.type(screen.getByPlaceholderText('Customer ID'), 'C000001');
    fireEvent.change(screen.getByTitle('Event date'), { target: { value: '2027-03-05' } });

    userEvent.click(screen.getByRole('button', { name: 'Confirm' }));

    expect(await screen.findByText('Gown 1 is already rented for an event on 2027-03-03.')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('Customer ID')).toHaveValue('C000001');   // nothing typed is lost
    expect(screen.getByRole('button', { name: 'Confirm' })).toBeEnabled();

    placeOrder.mockResolvedValueOnce({ rentals: [] });
    fireEvent.change(screen.getByTitle('Event date'), { target: { value: '2027-04-10' } });
    userEvent.click(screen.getByRole('button', { name: 'Confirm' }));
    expect(await screen.findByRole('status')).toHaveTextContent('The order was placed');
  });

  test('cancelling the order form changes nothing', async () => {
    await addFirstSizeToCart();
    userEvent.click(screen.getByRole('button', { name: 'Place Order (1)' }));

    userEvent.click(screen.getByRole('button', { name: 'Cancel' }));

    expect(placeOrder).not.toHaveBeenCalled();
    expect(screen.getByRole('button', { name: 'Place Order (1)' })).toBeInTheDocument();
  });

  test('a size that is deleted also leaves the cart', async () => {
    deleteSize.mockResolvedValue(undefined);
    await addFirstSizeToCart();

    userEvent.click(screen.getAllByTitle('Delete size')[0]);

    expect(await screen.findByRole('button', { name: 'Place Order (0)' })).toBeInTheDocument();
  });
});

describe('deleting', () => {
  test('nothing is deleted when the user does not confirm', async () => {
    confirmAnswer = false;
    await openPage();
    open('Simple Gown');

    userEvent.click(screen.getAllByTitle('Delete size')[0]);
    userEvent.click(screen.getAllByTitle('Delete')[0]);

    expect(deleteSize).not.toHaveBeenCalled();
    expect(deleteModel).not.toHaveBeenCalled();
    expect(screen.getByText('Simple Gown')).toBeInTheDocument();
  });

  test('a size that is confirmed is deleted and the list is reloaded', async () => {
    deleteSize.mockResolvedValue(undefined);
    await openPage();
    open('Simple Gown');
    loadGowns.mockClear();

    userEvent.click(screen.getAllByTitle('Delete size')[0]);

    await waitFor(() => expect(deleteSize).toHaveBeenCalledWith(1));
    await waitFor(() => expect(loadGowns).toHaveBeenCalled());
  });

  test('a whole model is deleted after confirmation', async () => {
    deleteModel.mockResolvedValue(undefined);
    await openPage();
    loadGowns.mockResolvedValue([gownModels[0]]);   // what the server returns once the model is gone

    userEvent.click(screen.getAllByTitle('Delete')[1]);   // the second card: Evening Gown

    await waitFor(() => expect(deleteModel).toHaveBeenCalledWith(2));
    await waitFor(() => expect(screen.queryByText('Evening Gown')).not.toBeInTheDocument());
  });

  test('a gown with rental history cannot be deleted: the reason is shown and the gown stays', async () => {
    deleteModel.mockRejectedValue(refused(409, 'Gown model 1 has sizes with rental history and cannot be deleted.'));
    await openPage();

    userEvent.click(screen.getAllByTitle('Delete')[0]);

    expect(await screen.findByRole('alert')).toHaveTextContent('has sizes with rental history');
    expect(screen.getByText('Simple Gown')).toBeInTheDocument();
    userEvent.click(screen.getByRole('button', { name: 'Dismiss message' }));
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
  });
});

describe('adding and editing', () => {
  const fillNewGown = (overrides: Partial<Record<string, string>> = {}) => {
    const values: Record<string, string> = {
      'Gown Name': '  Aurora Gown ', 'Color (e.g. Red)': ' Silver ', 'Size (e.g. 36)': '38', 'Price (₪)': '320', ...overrides,
    };
    userEvent.click(screen.getByRole('button', { name: 'Click to add a new gown' }));
    Object.entries(values).forEach(([placeholder, value]) => userEvent.type(screen.getByPlaceholderText(placeholder), value));
  };

  test('a new gown is sent clean: trimmed text, numbers as numbers, an empty picture as none', async () => {
    createGown.mockResolvedValue({});
    await openPage();
    fillNewGown();

    userEvent.click(screen.getByRole('button', { name: 'Add' }));

    await waitFor(() => expect(createGown).toHaveBeenCalledTimes(1));
    expect(createGown).toHaveBeenCalledWith({
      name: 'Aurora Gown', color: 'Silver', description: undefined, imageUrl: null, size: 38, price: 320,
    });
    expect(await screen.findByRole('status')).toHaveTextContent('The gown was added.');
    expect(screen.queryByPlaceholderText('Gown Name')).not.toBeInTheDocument();
  });

  test('the form keeps what was typed when the server refuses, and says why', async () => {
    createGown.mockRejectedValue(refused(400, 'A name is required for a new gown model.'));
    await openPage();
    fillNewGown();

    userEvent.click(screen.getByRole('button', { name: 'Add' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('A name is required');
    expect(screen.getByPlaceholderText('Gown Name')).toHaveValue('  Aurora Gown ');
    expect(screen.getByRole('button', { name: 'Add' })).toBeEnabled();
  });

  test('the form refuses an empty name or an impossible size before asking the server', async () => {
    await openPage();
    userEvent.click(screen.getByRole('button', { name: 'Click to add a new gown' }));

    expect(screen.getByPlaceholderText('Gown Name')).toBeRequired();
    expect(screen.getByPlaceholderText('Color (e.g. Red)')).toBeRequired();
    expect(screen.getByPlaceholderText('Size (e.g. 36)')).toHaveAttribute('min', '1');
    expect(screen.getByPlaceholderText('Size (e.g. 36)')).toHaveAttribute('max', '100');
    expect(screen.getByPlaceholderText('Price (₪)')).toHaveAttribute('min', '1');
    expect(screen.getByPlaceholderText('Gown Name')).toHaveAttribute('maxlength', '60');
    expect(createGown).not.toHaveBeenCalled();
  });

  test('a size is added to an existing model from its card', async () => {
    createGown.mockResolvedValue({});
    await openPage();
    open('Simple Gown');
    const panel = screen.getByRole('form', { name: 'Add a size' });

    userEvent.type(within(panel).getByPlaceholderText('Size'), '40');
    userEvent.type(within(panel).getByPlaceholderText('Price'), '260');
    userEvent.click(within(panel).getByRole('button', { name: 'Add' }));

    await waitFor(() => expect(createGown).toHaveBeenCalledWith({ modelId: 1, size: 40, price: 260 }));
  });

  test('adding a size the model already has shows the reason', async () => {
    createGown.mockRejectedValue(refused(409, 'Gown model 1 already has size 36.'));
    await openPage();
    open('Simple Gown');
    const panel = screen.getByRole('form', { name: 'Add a size' });

    userEvent.type(within(panel).getByPlaceholderText('Size'), '36');
    userEvent.type(within(panel).getByPlaceholderText('Price'), '260');
    userEvent.click(within(panel).getByRole('button', { name: 'Add' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('already has size 36');
  });

  test('a size is edited and saved with the price, the size number and whether it is in stock', async () => {
    updateSize.mockResolvedValue({});
    await openPage();
    open('Simple Gown');

    userEvent.click(screen.getAllByTitle('Edit size')[0]);
    const price = screen.getAllByPlaceholderText('Price')[0];
    userEvent.clear(price);
    userEvent.type(price, '299');
    userEvent.click(screen.getByLabelText(/In stock/));
    userEvent.click(screen.getByRole('button', { name: 'Save' }));

    await waitFor(() => expect(updateSize).toHaveBeenCalledWith(1, { size: 36, price: 299, isActive: false }));
  });

  test('a refused size edit stays open and shows the reason', async () => {
    updateSize.mockRejectedValue(refused(409, 'Gown 1 has rental history, so its size cannot be changed.'));
    await openPage();
    open('Simple Gown');
    userEvent.click(screen.getAllByTitle('Edit size')[0]);

    userEvent.click(screen.getByRole('button', { name: 'Save' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('size cannot be changed');
    expect(screen.getByRole('button', { name: 'Save' })).toBeInTheDocument();
  });

  test('a model is edited with its picture and description', async () => {
    updateModel.mockResolvedValue({});
    await openPage();

    userEvent.click(screen.getAllByTitle('Update')[0]);
    const name = screen.getByPlaceholderText('Name');
    userEvent.clear(name);
    userEvent.type(name, 'Simple Gown Deluxe');
    userEvent.type(screen.getByPlaceholderText('Image URL (optional)'), 'https://example.com/a.jpg');
    userEvent.click(screen.getByRole('button', { name: 'Save' }));

    await waitFor(() => expect(updateModel).toHaveBeenCalledWith(1, {
      name: 'Simple Gown Deluxe', color: 'blue', description: 'A simple blue gown', imageUrl: 'https://example.com/a.jpg',
    }));
  });
});
