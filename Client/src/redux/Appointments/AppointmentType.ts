export interface AppointmentType {
    id: number;
    appointmentDate: string;
    customerId: string;
    hour: string;
    numberOfPeople: number | null;
    isAvailable: boolean | null;
    // Shown to staff for booked slots; empty for free slots.
    customerName?: string;
    customerPhone?: string;
    customerEmail?: string;
}
