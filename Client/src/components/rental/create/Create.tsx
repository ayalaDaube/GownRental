import React, { useState } from 'react';
import './Create.css';
import { useDispatch } from 'react-redux';
import { addRentalThunk, getRentals, getReturning, getRenting } from '../../../redux/Rentals/RentalSlice';
import { AppDispatch } from '../../../store/store';

const emptyForm = { customerId: '', eventDate: '', gownId: '' };

const Create: React.FC = () => {
    const dispatch = useDispatch<AppDispatch>();
    const [isOpen, setIsOpen] = useState(false);
    const [isSaving, setIsSaving] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [form, setForm] = useState(emptyForm);

    const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        setForm({ ...form, [e.target.name]: e.target.value });
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setError(null);
        setIsSaving(true);
        try {
            // The size comes from the gown and the booking time from the server.
            await dispatch(addRentalThunk({
                customerId: form.customerId.trim(),
                eventDate: form.eventDate,
                gownId: Number(form.gownId),
            })).unwrap();
            dispatch(getRentals());
            dispatch(getReturning());
            dispatch(getRenting());
            setIsOpen(false);
            setForm(emptyForm);
        } catch (reason) {
            // The form stays open with what was typed; the message says what to fix.
            setError(typeof reason === 'string' ? reason : 'Could not add the rental.');
        } finally {
            setIsSaving(false);
        }
    };

    return (
        <div className="create-container">
            <button className="create-toggle-btn" onClick={() => { setIsOpen(true); setError(null); }}>+ New Rental</button>
            {isOpen && (
                <div className="create-overlay" onClick={() => setIsOpen(false)}>
                    <form className="create-form" onSubmit={handleSubmit} onClick={e => e.stopPropagation()}>
                        <h3 className="create-title">Add New Rental</h3>
                        {error && <div className="error-message" role="alert" style={{ color: '#d32f2f', padding: '10px', marginBottom: '10px', backgroundColor: '#ffebee', borderRadius: '4px', fontSize: '14px' }}>{error}</div>}
                        <div className="create-fields">
                            <label>Customer ID<input name="customerId" placeholder="Customer ID" value={form.customerId} onChange={handleChange} required /></label>
                            <label>Event Date<input name="eventDate" type="date" value={form.eventDate} onChange={handleChange} required /></label>
                            <label>Gown ID<input name="gownId" type="number" min={1} placeholder="Gown ID" value={form.gownId} onChange={handleChange} required /></label>
                        </div>
                        <div className="create-actions">
                            <button type="submit" className="create-submit-btn" disabled={isSaving}>{isSaving ? 'Adding…' : 'Add'}</button>
                            <button type="button" className="create-cancel-btn" onClick={() => setIsOpen(false)}>Cancel</button>
                        </div>
                    </form>
                </div>
            )}
        </div>
    );
};

export default Create;
