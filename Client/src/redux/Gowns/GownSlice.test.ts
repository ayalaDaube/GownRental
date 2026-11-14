import { configureStore } from '@reduxjs/toolkit';
import gownReducer, { addGownThunk, deleteGownModelThunk, deleteGownThunk, dismissNotice, updateGownModelThunk, updateGownSizeThunk } from './GownSlice';
import { createGownApi, deleteGownApi, deleteGownModelApi, updateGownModelApi, updateGownSizeApi } from '../../services/Gowns/task.service';

// The services are replaced so no network is used (and axios is not loaded by the test runner).
jest.mock('../../services/Gowns/task.service', () => ({
  fetchAllGowns: jest.fn(),
  fetchAllGroupsGowns: jest.fn(),
  createGownApi: jest.fn(),
  deleteGownApi: jest.fn(),
  deleteGownModelApi: jest.fn(),
  updateGownSizeApi: jest.fn(),
  updateGownModelApi: jest.fn(),
  fetchAvailableGownsByDate: jest.fn(),
}));
jest.mock('../../services/Rentals/task.service', () => ({ createRentalApi: jest.fn() }));

const deleteSize = deleteGownApi as jest.Mock;
const deleteModel = deleteGownModelApi as jest.Mock;
const createGown = createGownApi as jest.Mock;
const updateSize = updateGownSizeApi as jest.Mock;
const updateModel = updateGownModelApi as jest.Mock;

const model = (id: number, sizes: number[]) => ({
  id,
  name: `Model ${id}`,
  color: 'Blue',
  description: '',
  details: sizes.map(gownId => ({ gownId, size: 36, modelId: id, price: 100, isActive: true, isAvailable: true })),
});

const storeWith = (gowns: any[]) => {
  const store = configureStore({ reducer: { gowns: gownReducer } });
  store.dispatch({ type: 'gowns/fetchGroupsGowns/fulfilled', payload: gowns });
  return store;
};

beforeEach(() => jest.resetAllMocks());

test('deleting a size removes only that size and keeps the model while it has other sizes', async () => {
  deleteSize.mockResolvedValue(undefined);
  const store = storeWith([model(1, [10, 11]), model(2, [20])]);

  await store.dispatch(deleteGownThunk(10) as any);

  const { gowns } = store.getState().gowns;
  expect(gowns.map(m => m.id)).toEqual([1, 2]);
  expect(gowns[0].details.map(d => d.gownId)).toEqual([11]);
});

test('deleting the last size removes the model from the list', async () => {
  deleteSize.mockResolvedValue(undefined);
  const store = storeWith([model(1, [10, 11]), model(2, [20])]);

  await store.dispatch(deleteGownThunk(20) as any);

  expect(store.getState().gowns.gowns.map(m => m.id)).toEqual([1]);
});

test('a size id that equals another model id does not remove the wrong model', async () => {
  deleteSize.mockResolvedValue(undefined);
  // Size 2 belongs to model 1, while a different model has id 2.
  const store = storeWith([model(1, [2, 3]), model(2, [20])]);

  await store.dispatch(deleteGownThunk(2) as any);

  expect(store.getState().gowns.gowns.map(m => m.id)).toEqual([1, 2]);
});

test('a refused deletion keeps the gown and shows the reason', async () => {
  deleteSize.mockRejectedValue({ response: { status: 409, data: { detail: 'Gown 10 has rental history and cannot be deleted.' } } });
  const store = storeWith([model(1, [10])]);

  await store.dispatch(deleteGownThunk(10) as any);

  const state = store.getState().gowns;
  expect(state.gowns).toHaveLength(1);
  expect(state.notice).toEqual({ type: 'error', text: 'Gown 10 has rental history and cannot be deleted.' });
});

test('deleting a model removes it, and a refusal shows the reason', async () => {
  deleteModel.mockResolvedValueOnce(undefined);
  deleteModel.mockRejectedValueOnce({ response: { status: 409, data: { detail: 'Model 2 has sizes with rental history.' } } });
  const store = storeWith([model(1, [10]), model(2, [20])]);

  await store.dispatch(deleteGownModelThunk(1) as any);
  expect(store.getState().gowns.gowns.map(m => m.id)).toEqual([2]);

  await store.dispatch(deleteGownModelThunk(2) as any);
  expect(store.getState().gowns.gowns.map(m => m.id)).toEqual([2]);
  expect(store.getState().gowns.notice?.text).toBe('Model 2 has sizes with rental history.');
});

test('the message can be dismissed', async () => {
  deleteSize.mockRejectedValue({ response: { status: 404, data: { detail: 'Gown 10 does not exist.' } } });
  const store = storeWith([model(1, [10])]);
  await store.dispatch(deleteGownThunk(10) as any);

  store.dispatch(dismissNotice());

  expect(store.getState().gowns.notice).toBeNull();
});

test('adding a gown sends only what the user typed and reports success', async () => {
  createGown.mockResolvedValue({ id: 24, modelId: 16 });
  const store = storeWith([]);

  await store.dispatch(addGownThunk({ name: 'Aurora', color: 'Silver', size: 38, price: 320 }) as any);

  expect(createGown).toHaveBeenCalledWith({ name: 'Aurora', color: 'Silver', size: 38, price: 320 });
  expect(store.getState().gowns.notice).toEqual({ type: 'success', text: 'The gown was added.' });
});

test('a refused new gown shows the reason from the server', async () => {
  createGown.mockRejectedValue({ response: { status: 409, data: { detail: 'Gown model 1 already has size 36.' } } });
  const store = storeWith([]);

  await store.dispatch(addGownThunk({ modelId: 1, size: 36, price: 250 }) as any);

  expect(store.getState().gowns.notice).toEqual({ type: 'error', text: 'Gown model 1 already has size 36.' });
});

test('editing a size and a model is sent to the server and reported', async () => {
  updateSize.mockResolvedValue({});
  updateModel.mockRejectedValue({ response: { status: 400, data: { errors: { Name: ['The Name field is required.'] } } } });
  const store = storeWith([model(1, [10])]);

  await store.dispatch(updateGownSizeThunk({ gownId: 10, changes: { size: 37, price: 275, isActive: true } }) as any);
  expect(updateSize).toHaveBeenCalledWith(10, { size: 37, price: 275, isActive: true });
  expect(store.getState().gowns.notice?.type).toBe('success');

  await store.dispatch(updateGownModelThunk({ modelId: 1, changes: { name: '', color: 'Red' } }) as any);
  expect(store.getState().gowns.notice).toEqual({ type: 'error', text: 'The Name field is required.' });
});
