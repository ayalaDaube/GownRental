import axios from 'axios';
import { AppointmentType } from '../../redux/Appointments/AppointmentType';

const APPOINTMENTS_API_URL = 'https://localhost:7265/api/Appointment';

export const fetchAvailableAppointmentsApi = async (): Promise<AppointmentType[]> => {
    const response = await axios.get(`${APPOINTMENTS_API_URL}/getAvailableAppointments`);
    return response.data;
};

export const fetchAllAppointmentsApi = async (): Promise<AppointmentType[]> => {
    const response = await axios.get(APPOINTMENTS_API_URL);
    return response.data;
};

export const fetchAppointmentsByDateApi = async (date: string): Promise<AppointmentType[]> => {
    const response = await axios.get(`${APPOINTMENTS_API_URL}/getByDate/${date}`);
    return response.data;
};

export const bookAppointmentApi = async (appointment: AppointmentType): Promise<void> => {
    await axios.post(APPOINTMENTS_API_URL, appointment);
};

export const deleteAppointmentApi = async (id: number): Promise<void> => {
    await axios.delete(`${APPOINTMENTS_API_URL}/${id}`);
};
