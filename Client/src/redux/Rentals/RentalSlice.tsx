import { createAsyncThunk, createSlice } from "@reduxjs/toolkit";
import { RentalProps } from "../../pages/rentals/rentalview/Rental";
import { fetchAllRentals, createRentalApi, deleteRentalApi, updateRentalApi, fetchReturning, fetchRenting } from "../../services/Rentals/task.service";

interface initialStateType {
  rentals: RentalProps[];
  returns: RentalProps[];
  pickups: RentalProps[];
  isLoading: boolean;
  error: string | null;
}

const initialState: initialStateType = {
  rentals: [],
  returns: [],
  pickups: [],
  isLoading: true,
  error: null,
};

export const getRentals = createAsyncThunk('rentals/getRentals', async () => {
  return await fetchAllRentals();
});

export const getReturning = createAsyncThunk('rentals/getReturning', async () => {
  return await fetchReturning();
});

export const getRenting = createAsyncThunk('rentals/getRenting', async () => {
  return await fetchRenting();
});

export const addRentalThunk = createAsyncThunk('rentals/addRental', async (rental: RentalProps, { rejectWithValue }) => {
  try {
    return await createRentalApi(rental);
  } catch (error: any) {
    return rejectWithValue(error?.error || 'Failed to add rental');
  }
});

export const deleteRentalThunk = createAsyncThunk('rentals/deleteRental', async (rentalId: number) => {
  await deleteRentalApi(rentalId);
  return rentalId;
});

export const updateRentalThunk = createAsyncThunk('rentals/updateRental', async (rental: RentalProps, { rejectWithValue }) => {
  try {
    return await updateRentalApi(rental);
  } catch (error: any) {
    return rejectWithValue(error?.error || 'Failed to update rental');
  }
});

const rentalSlice = createSlice({
  name: "rentals",
  initialState,
  reducers: {
    setRentals: (state, action: { payload: RentalProps[] }) => {
      state.rentals = action.payload;
    },
    deleteRental: (state, action: { payload: number }) => {
      state.rentals = state.rentals.filter(r => r.rentalId !== action.payload);
    },
    updateRental: (state, action: { payload: RentalProps }) => {
      const index = state.rentals.findIndex(r => r.rentalId === action.payload.rentalId);
      if (index !== -1) state.rentals[index] = action.payload;
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(getRentals.pending, (state) => { state.isLoading = true; })
      .addCase(getRentals.fulfilled, (state, action) => { state.isLoading = false; state.rentals = action.payload; })
      .addCase(getRentals.rejected, (state) => { state.isLoading = false; state.error = "Failed to load rentals."; })

      .addCase(addRentalThunk.fulfilled, (state, action) => { state.isLoading = false; state.rentals.push(action.payload); state.error = null; })
      .addCase(addRentalThunk.rejected, (state, action) => { state.error = action.payload as string || "Failed to add rental."; })

      .addCase(deleteRentalThunk.fulfilled, (state, action) => {
        state.rentals = state.rentals.filter(r => r.rentalId !== action.payload);
      })

      .addCase(updateRentalThunk.fulfilled, (state, action) => {
        const updated = action.meta.arg;
        const index = state.rentals.findIndex(r => r.rentalId === updated.rentalId);
        if (index !== -1) state.rentals[index] = updated;
        state.error = null;
      })
      .addCase(updateRentalThunk.rejected, (state, action) => {
        state.error = action.payload as string || "Failed to update rental.";
      })

      .addCase(getReturning.fulfilled, (state, action) => { state.returns = action.payload; })
      .addCase(getRenting.fulfilled, (state, action) => { state.pickups = action.payload; });
  }
});

export const { setRentals, deleteRental, updateRental } = rentalSlice.actions;
export default rentalSlice.reducer;
