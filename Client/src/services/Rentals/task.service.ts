import http from '../http';
import { RentalProps } from '../../pages/rentals/rentalview/Rental';

const RENTALS_API_URL = '/Rentals';

/** What the user types to create a rental. The size and the booking time are decided by the server. */
export interface NewRentalInput {
  customerId: string;
  gownId: number;
  eventDate: string;
}

/** What the user can change on an existing rental. */
export interface RentalChangesInput extends NewRentalInput {
  rentalId: number;
}

export const fetchAllRentals = async (): Promise<RentalProps[]> => {
  const response = await http.get(RENTALS_API_URL);
  return response.data;
};

export const createRentalApi = async (rental: NewRentalInput): Promise<RentalProps> => {
  const response = await http.post(RENTALS_API_URL, rental);
  return response.data;
};

export const deleteRentalApi = async (rentalId: number): Promise<void> => {
  await http.delete(`${RENTALS_API_URL}/${rentalId}`);
};

export const updateRentalApi = async (rental: RentalChangesInput): Promise<RentalProps> => {
  const response = await http.put(RENTALS_API_URL, rental);
  return response.data;
};

export const fetchReturning = async (): Promise<RentalProps[]> => {
  const response = await http.get(`${RENTALS_API_URL}/returning`);
  return response.data;
};

export const fetchRenting = async (): Promise<RentalProps[]> => {
  const response = await http.get(`${RENTALS_API_URL}/renting`);
  return response.data;
};
