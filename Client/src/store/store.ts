import { configureStore } from '@reduxjs/toolkit';
import rentalReducer from '../redux/Rentals/RentalSlice';
import gownReducer from '../redux/Gowns/GownSlice';
import appointmentReducer from '../redux/Appointments/AppointmentSlice';
import authReducer from '../redux/Auth/AuthSlice';

export const store = configureStore({
  reducer: {
    rentals: rentalReducer,
    gowns: gownReducer,
    appointments: appointmentReducer,
    auth: authReducer,
  },
});

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
export default store; 