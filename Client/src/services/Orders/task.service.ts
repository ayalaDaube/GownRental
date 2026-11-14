import http from '../http';

/** One customer renting several gowns for the same event. */
export interface OrderInput {
  customerId: string;
  eventDate: string;
  gownIds: number[];
  // Only needed when the customer is not registered yet.
  customerName?: string;
  customerPhone?: string;
  customerEmail?: string;
}

/** Rents all the gowns together, or none of them if any one cannot be rented. */
export const placeOrderApi = async (order: OrderInput) => {
  const response = await http.post('/Orders', order);
  return response.data;
};
