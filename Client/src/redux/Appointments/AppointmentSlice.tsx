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
            return rejectWithValue(getApiErrorMessage(error, 'לא ניתן היה לקבוע את התור. נסו שוב.'));
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
            return rejectWithValue(getApiErrorMessage(error, 'לא ניתן היה לבטל את התור. נסו שוב.'));
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
            .addCase(fetchAvailableAppointments.pending, (state) => { state.isLoading = true; state.error = null; })
            .addCase(fetchAvailableAppointments.fulfilled, (state, action) => { state.isLoading = false; state.error = null; state.available = action.payload; })
            .addCase(fetchAvailableAppointments.rejected, (state) => { state.isLoading = false; state.error = "לא ניתן לטעון את התורים הפנויים כרגע. נסו שוב."; })

            .addCase(fetchAllAppointments.pending, (state) => { state.isLoading = true; state.error = null; })
            .addCase(fetchAllAppointments.fulfilled, (state, action) => { state.isLoading = false; state.error = null; state.all = action.payload; })
            .addCase(fetchAllAppointments.rejected, (state) => { state.isLoading = false; state.error = "לא ניתן לטעון את התורים כרגע. נסו שוב."; })

            .addCase(fetchAppointmentsByDate.fulfilled, (state, action) => { state.all = action.payload; })

            .addCase(bookAppointmentThunk.fulfilled, (state, action) => {
                state.available = state.available.filter(a => a.id !== action.payload);
            })

            // A cancelled slot is not removed: it stays in the list as a free slot, so reload the list afterwards.
            .addCase(deleteAppointmentThunk.fulfilled, (state) => {
                state.notice = { type: 'success', text: 'התור בוטל.' };
            })
            .addCase(deleteAppointmentThunk.rejected, (state, action) => {
                state.notice = { type: 'error', text: action.payload ?? 'לא ניתן היה לבטל את התור. נסו שוב.' };
            });
    },
});

export const { dismissNotice } = appointmentSlice.actions;
export default appointmentSlice.reducer;
