import React, { useState } from 'react';
import './PlaceOrder.css'
import { useAppDispatch } from '../../../store/hooks';
import { placeOrderThunk } from '../../../redux/Gowns/GownSlice';
import { CartItem } from '../../../redux/Gowns/GownTypes';

export interface PlaceOrderProps {
  cart: CartItem[];
  onClose: () => void;
  /** Called after the order was saved, so the page can refresh what is available. */
  onPlaced?: () => void;
  /** The date picked on the page, offered as the event date. */
  defaultEventDate?: string;
}

const PlaceOrder: React.FC<PlaceOrderProps> = ({ cart, onClose, onPlaced, defaultEventDate }) => {
    const dispatch = useAppDispatch();
    const [isSaving, setIsSaving] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [form, setForm] = useState({
        customerId: '',
        eventDate: defaultEventDate ?? '',
        customerName: '',
        customerPhone: '',
        customerEmail: '',
    });

    const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        setForm({ ...form, [e.target.name]: e.target.value });
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setError(null);
        setIsSaving(true);
        try {
            await dispatch(placeOrderThunk({
                customerId: form.customerId.trim(),
                eventDate: form.eventDate,
                gownIds: cart.map(item => item.gownId),
                customerName: form.customerName.trim() || undefined,
                customerPhone: form.customerPhone.trim() || undefined,
                customerEmail: form.customerEmail.trim() || undefined,
            })).unwrap();
            onPlaced?.();
            onClose();
        } catch (reason) {
            // Nothing was saved. The form stays open so the order can be corrected and sent again.
            setError(typeof reason === 'string' ? reason : 'Could not place the order.');
        } finally {
            setIsSaving(false);
        }
    };

    return (
        <div className="place-order-overlay" onClick={onClose}>
        <form className="place-order-form" onSubmit={handleSubmit} onClick={e => e.stopPropagation()}>
            <h3 className="place-order-title">Place Order</h3>
            {cart.length === 0 ? (
                <p>The cart is empty. Add gowns to the cart first.</p>
            ) : (
                <p style={{ fontSize: '14px', margin: '0 0 10px' }}>
                    Gowns: {cart.map(item => `#${item.gownId} (${item.name}, size ${item.size})`).join(', ')}
                </p>
            )}
            {error && <div role="alert" style={{ color: '#d32f2f', padding: '10px', marginBottom: '10px', backgroundColor: '#ffebee', borderRadius: '4px', fontSize: '14px' }}>{error}</div>}
            <div className="place-order-fields">
                <input type="text" name="customerId" placeholder="Customer ID" value={form.customerId} onChange={handleChange} required />
                <input type="date" name="eventDate" title="Event date" value={form.eventDate} onChange={handleChange} required />
                <input type="text" name="customerName" placeholder="Name (new customers only)" maxLength={60} value={form.customerName} onChange={handleChange} />
                <input type="tel" name="customerPhone" placeholder="Phone (new customers only)" value={form.customerPhone} onChange={handleChange} />
                <input type="email" name="customerEmail" placeholder="Email (optional)" maxLength={100} value={form.customerEmail} onChange={handleChange} />
            </div>
            <div className="place-order-actions">
                <button type="submit" className="place-order-submit-btn" disabled={isSaving || cart.length === 0}>{isSaving ? 'Placing…' : 'Confirm'}</button>
                <button type="button" className="place-order-cancel-btn" onClick={onClose}>Cancel</button>
            </div>
        </form>
    </div>
    )
};
export default PlaceOrder;
