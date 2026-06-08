import { configureStore } from '@reduxjs/toolkit';
import rentalReducer from '../redux/Rentals/RentalSlice';
import gownReducer from '../redux/Gowns/GownSlice';
import appointmentReducer from '../redux/Appointments/AppointmentSlice';

export const store = configureStore({
  reducer: {
    rentals: rentalReducer,
    gowns: gownReducer,
    appointments: appointmentReducer,
  },
});

export type RootState = ReturnType<typeof store.getState>;
export default store; 