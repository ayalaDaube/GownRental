import React, { useState } from 'react';
import './Update.css';
import { FaEdit } from 'react-icons/fa';
import { useDispatch } from 'react-redux';
import { updateRentalThunk, getReturning, getRenting } from '../../../redux/Rentals/RentalSlice';
import { RentalProps } from '../../../pages/rentals/rentalview/Rental';

const Update: React.FC<{ rental: RentalProps }> = ({ rental }) => {
    const dispatch = useDispatch();
    const [isOpen, setIsOpen] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [form, setForm] = useState({
        ...rental,
        rentalDate: rental.rentalDate?.toString().slice(0, 10),
        eventDate: rental.eventDate?.toString().slice(0, 10),
    });

    const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        setForm({ ...form, [e.target.name]: e.target.value });
    };

    const handleSubmit = (e: React.FormEvent) => {
        e.preventDefault();
        e.stopPropagation();
        setError(null);
        dispatch(updateRentalThunk({ ...form, gownId: Number(form.gownId), size: Number(form.size) }) as any)
            .then((result: any) => {
                if (result.payload && typeof result.payload === 'string') {
                    // Error from rejectWithValue
                    setError(result.payload);
                } else if (result.type?.endsWith('/rejected')) {
                    // Rejected action
                    setError(result.payload || 'Failed to update rental');
                } else {
                    // Success
                    setIsOpen(false);
                    dispatch(getReturning() as any);
                    dispatch(getRenting() as any);
                }
            })
            .catch((err: any) => {
                setError(err?.message || 'Failed to update rental');
            });
    };

    return (
        <>
            <button className="update-btn" title="Update" onClick={() => { setIsOpen(true); setError(null); }}>
                <FaEdit />
            </button>
            {isOpen && (
                <div className="update-overlay" onClick={() => setIsOpen(false)}>
                    <form className="update-form" onSubmit={handleSubmit} onClick={e => e.stopPropagation()}>
                        <h3 className="update-title">Update Rental</h3>
                        {error && <div className="error-message" style={{ color: '#d32f2f', padding: '10px', marginBottom: '10px', backgroundColor: '#ffebee', borderRadius: '4px', fontSize: '14px' }}>{error}</div>}
                        <div className="update-fields">
                            <label>Customer ID<input name="customerId" placeholder="Customer ID" defaultValue={rental.customerId} onChange={handleChange} required /></label>
                            <label>Rental Date<input name="rentalDate" type="date" defaultValue={rental.rentalDate?.toString().slice(0, 10)} onChange={handleChange} required /></label>
                            <label>Event Date<input name="eventDate" type="date" defaultValue={rental.eventDate?.toString().slice(0, 10)} onChange={handleChange} required /></label>
                            <label>Gown ID<input name="gownId" placeholder="Gown ID" defaultValue={rental.gownId} onChange={handleChange} required /></label>
                            <label>Size<input name="size" placeholder="Size" defaultValue={rental.size} onChange={handleChange} required /></label>
                        </div>
                        <div className="update-actions">
                            <button type="submit" className="update-submit-btn">Save</button>
                            <button type="button" className="update-cancel-btn" onClick={() => setIsOpen(false)}>Cancel</button>
                        </div>
                    </form>
                </div>
            )}
        </>
    );
};

export default Update;
