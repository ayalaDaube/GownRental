import React, { useState } from 'react';
import { useDispatch } from 'react-redux';
import { AppointmentType } from '../../redux/Appointments/AppointmentType';
import { bookAppointmentThunk, fetchAvailableAppointments } from '../../redux/Appointments/AppointmentSlice';
import '../../pages/appointments/appointments.css';

interface Props {
    appointment: AppointmentType;
    onClose: () => void;
}

function BookAppointment({ appointment, onClose }: Props) {
    const dispatch = useDispatch<any>();
    const [form, setForm] = useState({ customerId: '', customerName: '', customerPhone: '', customerEmail: '', numberOfPeople: 1 });
    const [confirmed, setConfirmed] = useState(false);

    const handleSubmit = (e: React.FormEvent) => {
        e.preventDefault();
        const booked: AppointmentType = {
            ...appointment,
            ...form,
            numberOfPeople: form.numberOfPeople,
            isAvailable: false,
        };
        dispatch(bookAppointmentThunk(booked)).then(() => {
            dispatch(fetchAvailableAppointments());
            setConfirmed(true);
        });
    };

    if (confirmed) {
        return (
            <div className="modal-overlay">
                <div className="modal">
                    <div className="success-icon">✓</div>
                    <h3 className="success-title">התור נקבע בהצלחה!</h3>
                    <div className="success-details">
                        <div className="success-row"><span>תאריך</span><strong>{appointment.appointmentDate}</strong></div>
                        <div className="success-row"><span>שעה</span><strong>{appointment.hour}</strong></div>
                        <div className="success-row"><span>שם</span><strong>{form.customerName}</strong></div>
                        <div className="success-row"><span>תעודת זהות</span><strong>{form.customerId}</strong></div>
                        <div className="success-row"><span>טלפון</span><strong>{form.customerPhone}</strong></div>
                        {form.customerEmail && <div className="success-row"><span>אימייל</span><strong>{form.customerEmail}</strong></div>}
                        <div className="success-row"><span>מספר אנשים</span><strong>{form.numberOfPeople}</strong></div>
                    </div>
                    <button className="apt-book-btn" onClick={onClose}>סגור</button>
                </div>
            </div>
        );
    }

    return (
        <div className="modal-overlay">
            <div className="modal">
                <h3>הזמנת תור</h3>
                <p>{appointment.appointmentDate} | {appointment.hour}</p>
                <form onSubmit={handleSubmit}>
                    <label>תעודת זהות<input required value={form.customerId} onChange={e => setForm({ ...form, customerId: e.target.value })} /></label>
                    <label>שם מלא<input required value={form.customerName} onChange={e => setForm({ ...form, customerName: e.target.value })} /></label>
                    <label>טלפון<input required value={form.customerPhone} onChange={e => setForm({ ...form, customerPhone: e.target.value })} /></label>
                    <label>אימייל<input value={form.customerEmail} onChange={e => setForm({ ...form, customerEmail: e.target.value })} /></label>
                    <label>מספר אנשים<input type="number" min={1} value={form.numberOfPeople} onChange={e => setForm({ ...form, numberOfPeople: Number(e.target.value) })} /></label>
                    <div className="modal-actions">
                        <button type="submit">אשר תור</button>
                        <button type="button" onClick={onClose}>ביטול</button>
                    </div>
                </form>
            </div>
        </div>
    );
}

export default BookAppointment;
