import { configureStore } from '@reduxjs/toolkit';
import rentalReducer from '../redux/Rentals/RentalSlice';
import gownReducer from '../redux/Gowns/GownSlice';

export const store = configureStore({
  reducer: {
    rentals: rentalReducer,
    gowns: gownReducer,
  },
});

export type RootState = ReturnType<typeof store.getState>;
export default store; 