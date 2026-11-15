/** A rental of one gown, as the API returns it. */
export interface RentalProps {
    rentalId: number;
    customerId: string;
    rentalDate: string;
    eventDate: string;
    gownId: number;
    size: number;
}
