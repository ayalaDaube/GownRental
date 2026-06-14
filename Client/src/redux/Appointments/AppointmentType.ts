export interface AppointmentType {
    id: number;
    appointmentDate: string;
    customerId: string;
    hour: string;
    numberOfPeople: number | null;
    isAvailable: boolean | null;
    customerName: string;
    customerPhone: string;
    customerEmail?: string;
}
