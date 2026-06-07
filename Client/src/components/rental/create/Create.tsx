import React, { useState } from 'react';
import './Create.css';
import { useDispatch } from 'react-redux';
import { addRentalThunk, getRentals, getReturning, getRenting } from '../../../redux/Rentals/RentalSlice';
import { RentalProps } from '../../../pages/rentals/rentalview/Rental';

const Create: React.FC = () => {
    const dispatch = useDispatch();
    const [isOpen, setIsOpen] = useState(false);
    const [form, setForm] = useState({
        rentalId: 0,
        customerId: '',
        rentalDate: '',
        eventDate: '',
        gownId: 0,
        size: 0,
    });

    const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        setForm({ ...form, [e.target.name]: e.target.value });
    };

    const handleSubmit = (e: React.FormEvent) => {
        e.preventDefault();
        const rental: RentalProps = { ...form, customerId: Number(form.customerId), gownId: Number(form.gownId), size: Number(form.size) };
        dispatch(addRentalThunk(rental) as any).then(() => {
            dispatch(getRentals() as any);
            dispatch(getReturning() as any);
            dispatch(getRenting() as any);
        });
        setIsOpen(false);
        setForm({ rentalId: 0, customerId: '', rentalDate: '', eventDate: '', gownId: 0, size: 0 });
    };

    return (
        <div className="create-container">
            <button className="create-toggle-btn" onClick={() => setIsOpen(true)}>+ New Rental</button>
            {isOpen && (
                <div className="create-overlay" onClick={() => setIsOpen(false)}>
                    <form className="create-form" onSubmit={handleSubmit} onClick={e => e.stopPropagation()}>
                        <h3 className="create-title">Add New Rental</h3>
                        <div className="create-fields">
                            <label>Customer ID<input name="customerId" placeholder="Customer ID" onChange={handleChange} required /></label>
                            <label>Rental Date<input name="rentalDate" type="date" onChange={handleChange} required /></label>
                            <label>Event Date<input name="eventDate" type="date" onChange={handleChange} required /></label>
                            <label>Gown ID<input name="gownId" placeholder="Gown ID" onChange={handleChange} required /></label>
                            <label>Size<input name="size" placeholder="Size" onChange={handleChange} required /></label>
                        </div>
                        <div className="create-actions">
                            <button type="submit" className="create-submit-btn">Add</button>
                            <button type="button" className="create-cancel-btn" onClick={() => setIsOpen(false)}>Cancel</button>
                        </div>
                    </form>
                </div>
            )}
        </div>
    );
};

export default Create;
