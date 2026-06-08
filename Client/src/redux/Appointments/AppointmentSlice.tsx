import { createAsyncThunk, createSlice } from "@reduxjs/toolkit";
import { AppointmentType } from "./AppointmentType";
import {
    fetchAvailableAppointmentsApi,
    fetchAllAppointmentsApi,
    fetchAppointmentsByDateApi,
    bookAppointmentApi,
    deleteAppointmentApi,
} from "../../services/Appointments/task.service";

interface initialStateType {
    available: AppointmentType[];
    all: AppointmentType[];
    isLoading: boolean;
    error: string | null;
}

const initialState: initialStateType = {
    available: [],
    all: [],
    isLoading: false,
    error: null,
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

export const bookAppointmentThunk = createAsyncThunk("appointments/book", async (appointment: AppointmentType) => {
    await bookAppointmentApi(appointment);
    return appointment.id;
});

export const deleteAppointmentThunk = createAsyncThunk("appointments/delete", async (id: number) => {
    await deleteAppointmentApi(id);
    return id;
});

const appointmentSlice = createSlice({
    name: "appointments",
    initialState,
    reducers: {},
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

            .addCase(deleteAppointmentThunk.fulfilled, (state, action) => {
                state.all = state.all.filter(a => a.id !== action.payload);
            });
    },
});

export default appointmentSlice.reducer;
