import { createAsyncThunk, createSlice } from "@reduxjs/toolkit";
import { AppointmentType } from "./AppointmentType";
import { getApiErrorMessage } from "../../services/apiError";
import {
    fetchAvailableAppointmentsApi,
    fetchAllAppointmentsApi,
    fetchAppointmentsByDateApi,
    bookAppointmentApi,
    deleteAppointmentApi,
    BookingInput,
} from "../../services/Appointments/task.service";

interface initialStateType {
    available: AppointmentType[];
    all: AppointmentType[];
    isLoading: boolean;
    error: string | null;
    notice: { type: 'error' | 'success'; text: string } | null;
}

const initialState: initialStateType = {
    available: [],
    all: [],
    isLoading: false,
    error: null,
    notice: null,
};

export const fetchAvailableAppointments = createAsyncThunk("appointments/fetchAvailable", async () => {
    return await fetchAvailableAppointmentsApi();
});

export const fetchAllAppointments = createAsyncThunk("appointments/fetchAll", async () => {
    return await fetchAllAppointmentsApi();
});

export const fetchAppointmentsByDate = createAsyncThunk("appointments/fetchByDate", async (date: string) => {
    return await fetchAppointmentsByDateApi(date);
});

export const bookAppointmentThunk = createAsyncThunk<number, { slotId: number; booking: BookingInput }, { rejectValue: string }>(
    "appointments/book",
    async ({ slotId, booking }, { rejectWithValue }) => {
        try {
            await bookAppointmentApi(booking);
            return slotId;
        } catch (error) {
            return rejectWithValue(getApiErrorMessage(error, 'Could not book the appointment.'));
        }
    }
);

export const deleteAppointmentThunk = createAsyncThunk<number, number, { rejectValue: string }>(
    "appointments/delete",
    async (id, { rejectWithValue }) => {
        try {
            await deleteAppointmentApi(id);
            return id;
        } catch (error) {
            return rejectWithValue(getApiErrorMessage(error, 'Could not cancel the appointment.'));
        }
    }
);

const appointmentSlice = createSlice({
    name: "appointments",
    initialState,
    reducers: {
        dismissNotice: (state) => {
            state.notice = null;
        },
    },
    extraReducers: (builder) => {
        builder
            .addCase(fetchAvailableAppointments.pending, (state) => { state.isLoading = true; })
            .addCase(fetchAvailableAppointments.fulfilled, (state, action) => { state.isLoading = false; state.available = action.payload; })
            .addCase(fetchAvailableAppointments.rejected, (state) => { state.isLoading = false; state.error = "Failed to load available appointments."; })

            .addCase(fetchAllAppointments.pending, (state) => { state.isLoading = true; })
            .addCase(fetchAllAppointments.fulfilled, (state, action) => { state.isLoading = false; state.all = action.payload; })
            .addCase(fetchAllAppointments.rejected, (state) => { state.isLoading = false; state.error = "Failed to load appointments."; })

            .addCase(fetchAppointmentsByDate.fulfilled, (state, action) => { state.all = action.payload; })

            .addCase(bookAppointmentThunk.fulfilled, (state, action) => {
                state.available = state.available.filter(a => a.id !== action.payload);
            })

            // A cancelled slot is not removed: it stays in the list as a free slot, so reload the list afterwards.
            .addCase(deleteAppointmentThunk.fulfilled, (state) => {
                state.notice = { type: 'success', text: 'The appointment was cancelled.' };
            })
            .addCase(deleteAppointmentThunk.rejected, (state, action) => {
                state.notice = { type: 'error', text: action.payload ?? 'Could not cancel the appointment.' };
            });
    },
});

export const { dismissNotice } = appointmentSlice.actions;
export default appointmentSlice.reducer;
