import { combineReducers, configureStore } from '@reduxjs/toolkit';
import rentalReducer from '../redux/Rentals/RentalSlice';
import gownReducer from '../redux/Gowns/GownSlice';
import appointmentReducer from '../redux/Appointments/AppointmentSlice';
import authReducer from '../redux/Auth/AuthSlice';

const rootReducer = combineReducers({
  rentals: rentalReducer,
  gowns: gownReducer,
  appointments: appointmentReducer,
  auth: authReducer,
});

export type RootState = ReturnType<typeof rootReducer>;

/** Creates a store. The app uses one; tests create a fresh one each, optionally starting from a given state. */
export function setupStore(preloadedState?: Partial<RootState>) {
  return configureStore({ reducer: rootReducer, preloadedState });
}

export type AppStore = ReturnType<typeof setupStore>;
export type AppDispatch = AppStore['dispatch'];

export const store = setupStore();
export default store;
