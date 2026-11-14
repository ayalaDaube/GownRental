import React, { useState } from 'react';
import './Update.css';
import { FaEdit } from 'react-icons/fa';
import { useDispatch } from 'react-redux';
import { updateRentalThunk, getReturning, getRenting } from '../../../redux/Rentals/RentalSlice';
import { RentalProps } from '../../../pages/rentals/rentalview/Rental';
import { AppDispatch } from '../../../store/store';

const toForm = (rental: RentalProps) => ({
    customerId: rental.customerId,
    eventDate: rental.eventDate?.toString().slice(0, 10),
    gownId: String(rental.gownId),
});

const Update: React.FC<{ rental: RentalProps }> = ({ rental }) => {
    const dispatch = useDispatch<AppDispatch>();
    const [isOpen, setIsOpen] = useState(false);
    const [isSaving, setIsSaving] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [form, setForm] = useState(toForm(rental));

    const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        setForm({ ...form, [e.target.name]: e.target.value });
    };

    const open = () => {
        setForm(toForm(rental));
        setError(null);
        setIsOpen(true);
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        e.stopPropagation();
        setError(null);
        setIsSaving(true);
        try {
            await dispatch(updateRentalThunk({
                rentalId: rental.rentalId,
                customerId: form.customerId.trim(),
                eventDate: form.eventDate,
                gownId: Number(form.gownId),
            })).unwrap();
            dispatch(getReturning());
            dispatch(getRenting());
            setIsOpen(false);
        } catch (reason) {
            setError(typeof reason === 'string' ? reason : 'Could not update the rental.');
        } finally {
            setIsSaving(false);
        }
    };

    return (
        <>
            <button className="update-btn" title="Update" onClick={open}>
                <FaEdit />
            </button>
            {isOpen && (
                <div className="update-overlay" onClick={() => setIsOpen(false)}>
                    <form className="update-form" onSubmit={handleSubmit} onClick={e => e.stopPropagation()}>
                        <h3 className="update-title">Update Rental #{rental.rentalId}</h3>
                        {error && <div className="error-message" role="alert" style={{ color: '#d32f2f', padding: '10px', marginBottom: '10px', backgroundColor: '#ffebee', borderRadius: '4px', fontSize: '14px' }}>{error}</div>}
                        <div className="update-fields">
                            <label>Customer ID<input name="customerId" placeholder="Customer ID" value={form.customerId} onChange={handleChange} required /></label>
                            <label>Event Date<input name="eventDate" type="date" value={form.eventDate} onChange={handleChange} required /></label>
                            <label>Gown ID<input name="gownId" type="number" min={1} placeholder="Gown ID" value={form.gownId} onChange={handleChange} required /></label>
                        </div>
                        <div className="update-actions">
                            <button type="submit" className="update-submit-btn" disabled={isSaving}>{isSaving ? 'Saving…' : 'Save'}</button>
                            <button type="button" className="update-cancel-btn" onClick={() => setIsOpen(false)}>Cancel</button>
                        </div>
                    </form>
                </div>
            )}
        </>
    );
};

export default Update;
