import http from '../http';
import { AppointmentType } from '../../redux/Appointments/AppointmentType';

const APPOINTMENTS_API_URL = '/Appointment';

export const fetchAvailableAppointmentsApi = async (): Promise<AppointmentType[]> => {
    const response = await http.get(`${APPOINTMENTS_API_URL}/getAvailableAppointments`);
    return response.data;
};

export const fetchAllAppointmentsApi = async (): Promise<AppointmentType[]> => {
    const response = await http.get(APPOINTMENTS_API_URL);
    return response.data;
};

export const fetchAppointmentsByDateApi = async (date: string): Promise<AppointmentType[]> => {
    const response = await http.get(`${APPOINTMENTS_API_URL}/getByDate/${date}`);
    return response.data;
};

/** What a visitor sends to book a slot; name and phone are needed only by a new customer. */
export interface BookingInput {
    appointmentDate: string;
    hour: string;
    customerId: string;
    numberOfPeople: number;
    customerName?: string;
    customerPhone?: string;
    customerEmail?: string;
}

export const bookAppointmentApi = async (booking: BookingInput): Promise<void> => {
    await http.post(APPOINTMENTS_API_URL, booking);
};

export const deleteAppointmentApi = async (id: number): Promise<void> => {
    await http.delete(`${APPOINTMENTS_API_URL}/${id}`);
};
