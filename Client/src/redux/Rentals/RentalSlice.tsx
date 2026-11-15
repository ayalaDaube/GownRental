import { createAsyncThunk, createSlice } from "@reduxjs/toolkit";
import { RentalProps } from "./RentalType";
import {
  fetchAllRentals, createRentalApi, deleteRentalApi, updateRentalApi, fetchReturning, fetchRenting,
  NewRentalInput, RentalChangesInput,
} from "../../services/Rentals/task.service";
import { getApiErrorMessage } from "../../services/apiError";

interface initialStateType {
  rentals: RentalProps[];
  returns: RentalProps[];
  pickups: RentalProps[];
  isLoading: boolean;
  error: string | null;
  notice: { type: 'error' | 'success'; text: string } | null;
}

const initialState: initialStateType = {
  rentals: [],
  returns: [],
  pickups: [],
  isLoading: true,
  error: null,
  notice: null,
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

export const addRentalThunk = createAsyncThunk<RentalProps, NewRentalInput, { rejectValue: string }>(
  'rentals/addRental',
  async (rental, { rejectWithValue }) => {
    try {
      return await createRentalApi(rental);
    } catch (error) {
      return rejectWithValue(getApiErrorMessage(error, 'Could not add the rental.'));
    }
  }
);

export const deleteRentalThunk = createAsyncThunk<number, number, { rejectValue: string }>(
  'rentals/deleteRental',
  async (rentalId, { rejectWithValue }) => {
    try {
      await deleteRentalApi(rentalId);
      return rentalId;
    } catch (error) {
      return rejectWithValue(getApiErrorMessage(error, 'Could not delete the rental.'));
    }
  }
);

export const updateRentalThunk = createAsyncThunk<RentalProps, RentalChangesInput, { rejectValue: string }>(
  'rentals/updateRental',
  async (rental, { rejectWithValue }) => {
    try {
      return await updateRentalApi(rental);
    } catch (error) {
      return rejectWithValue(getApiErrorMessage(error, 'Could not update the rental.'));
    }
  }
);

const rentalSlice = createSlice({
  name: "rentals",
  initialState,
  reducers: {
    dismissNotice: (state) => {
      state.notice = null;
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(getRentals.pending, (state) => {
        // The spinner is for the first load only; a refresh after an edit keeps the list in place.
        if (state.rentals.length === 0) state.isLoading = true;
      })
      .addCase(getRentals.fulfilled, (state, action) => { state.isLoading = false; state.rentals = action.payload; state.error = null; })
      .addCase(getRentals.rejected, (state) => { state.isLoading = false; state.error = "Failed to load rentals."; })

      .addCase(addRentalThunk.fulfilled, (state, action) => { state.rentals.push(action.payload); })

      .addCase(deleteRentalThunk.fulfilled, (state, action) => {
        state.rentals = state.rentals.filter(r => r.rentalId !== action.payload);
      })
      .addCase(deleteRentalThunk.rejected, (state, action) => {
        state.notice = { type: 'error', text: action.payload ?? 'Could not delete the rental.' };
      })

      .addCase(updateRentalThunk.fulfilled, (state, action) => {
        const index = state.rentals.findIndex(r => r.rentalId === action.payload.rentalId);
        if (index !== -1) state.rentals[index] = action.payload;
      })

      .addCase(getReturning.fulfilled, (state, action) => { state.returns = action.payload; })
      .addCase(getRenting.fulfilled, (state, action) => { state.pickups = action.payload; });
  }
});

export const { dismissNotice } = rentalSlice.actions;
export default rentalSlice.reducer;
