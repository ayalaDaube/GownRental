import axios from 'axios';
import { RentalProps } from '../../pages/rentals/rentalview/Rental';

const RENTALS_API_URL = 'https://localhost:7265/api/Rentals';
export const fetchAllRentals = async (): Promise<RentalProps[]> => {
  await new Promise(resolve => setTimeout(resolve, 1000));
  const response = await axios.get(RENTALS_API_URL);
  return response.data;
};


export const createRentalApi = async (rental: RentalProps): Promise<RentalProps> => {
  try {
    const response = await axios.post(RENTALS_API_URL, rental);
    return response.data;
  } catch (error: any) {
    if (error.response?.data?.error) {
      return Promise.reject({ error: error.response.data.error });
    }
    throw error;
  }
};

export const deleteRentalApi = async (rentalId: number): Promise<void> => {
  await axios.delete(`${RENTALS_API_URL}/${rentalId}`);
};

export const updateRentalApi = async (rental: RentalProps): Promise<RentalProps> => {
  try {
    const response = await axios.put(`${RENTALS_API_URL}`, rental);
    return response.data;
  } catch (error: any) {
    if (error.response?.data?.error) {
      return Promise.reject({ error: error.response.data.error });
    }
    throw error;
  }
};

export const fetchReturning = async (): Promise<RentalProps[]> => {
  const response = await axios.get(`${RENTALS_API_URL}/returning`);
  return response.data;
};

export const fetchRenting = async (): Promise<RentalProps[]> => {
  const response = await axios.get(`${RENTALS_API_URL}/renting`);
  return response.data;
};

