
import React from 'react';
import './PlaceOrder.css'
import { GownModelProps } from '../gownModel/GownModel';
import { useState } from 'react';
import { useDispatch } from 'react-redux';
import { fetchGroupsGowns, placeOrderThunk } from '../../../redux/Gowns/GownSlice';
export interface PlaceOrderProps {
  cart:GownModelProps[];
  onClose: () => void;
}
const PlaceOrder: React.FC<PlaceOrderProps> = ({ cart, onClose }) => {

    const dispatch = useDispatch();
    const [form, setForm] = useState({
        rentalId: 0,
        customerId: '',
        rentalDate: '',
        eventDate: '',
    });

    const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        setForm({ ...form, [e.target.name]: e.target.value });
    };

    const handleSubmit = (e: React.FormEvent) => {
        e.preventDefault();
        dispatch(placeOrderThunk({ cart, rentalInfo: form }) as any)
        .then(() => {
            dispatch(fetchGroupsGowns() as any);
        });
        onClose();
    };
    return (
        <div className="place-order-overlay" onClick={onClose}>
        <form className="place-order-form" onSubmit={handleSubmit} onClick={e => e.stopPropagation()}>
            <h3 className="place-order-title">Place Order</h3>
            <div className="place-order-fields">
                <input type="text" name="customerId" placeholder="Customer ID" onChange={handleChange} required />
                <input type="date" name="rentalDate" placeholder="Rental Date" onChange={handleChange} required />
                <input type="date" name="eventDate" placeholder="Event Date" onChange={handleChange} required />
            </div>
            <div className="place-order-actions">
                <button type="submit" className="place-order-submit-btn">Confirm</button>
                <button type="button" className="place-order-cancel-btn" onClick={onClose}>Cancel</button>
            </div>
        </form>
    </div>
    )
};
export default PlaceOrder;