import React, { useState } from 'react';
import { useAppDispatch } from '../../store/hooks';
import { AppointmentType } from '../../redux/Appointments/AppointmentType';
import { bookAppointmentThunk, fetchAvailableAppointments } from '../../redux/Appointments/AppointmentSlice';
import '../../pages/appointments/appointments.css';

interface Props {
    appointment: AppointmentType;
    onClose: () => void;
}

function BookAppointment({ appointment, onClose }: Props) {
    const dispatch = useAppDispatch();
    const [form, setForm] = useState({ customerId: '', customerName: '', customerPhone: '', customerEmail: '', numberOfPeople: 1 });
    const [confirmed, setConfirmed] = useState(false);
    const [isSaving, setIsSaving] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setError(null);
        setIsSaving(true);

        const result = await dispatch(bookAppointmentThunk({
            slotId: appointment.id,
            booking: {
                appointmentDate: appointment.appointmentDate,
                hour: appointment.hour,
                customerId: form.customerId.trim(),
                customerName: form.customerName.trim() || undefined,
                customerPhone: form.customerPhone.trim() || undefined,
                customerEmail: form.customerEmail.trim() || undefined,
                numberOfPeople: form.numberOfPeople,
            },
        }));
        setIsSaving(false);

        if (bookAppointmentThunk.fulfilled.match(result)) {
            dispatch(fetchAvailableAppointments());
            setConfirmed(true);
        } else {
            // The booking was not made (for example someone else took this time). Say so, and refresh the list.
            setError(result.payload ?? 'לא ניתן היה לקבוע את התור. נסו שוב.');
            dispatch(fetchAvailableAppointments());
        }
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
                        <div className="success-row"><span>תעודת זהות</span><strong>{form.customerId}</strong></div>
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
                {error && <div role="alert" style={{ color: '#b3261e', background: '#fdecea', padding: '10px', borderRadius: '8px', marginBottom: '10px', fontSize: '14px' }}>{error}</div>}
                <form onSubmit={handleSubmit}>
                    <label>תעודת זהות<input required minLength={5} maxLength={9} value={form.customerId} onChange={e => setForm({ ...form, customerId: e.target.value })} /></label>
                    <label>שם מלא (ללקוח חדש)<input maxLength={60} value={form.customerName} onChange={e => setForm({ ...form, customerName: e.target.value })} /></label>
                    <label>טלפון (ללקוח חדש)<input type="tel" value={form.customerPhone} onChange={e => setForm({ ...form, customerPhone: e.target.value })} /></label>
                    <label>אימייל<input type="email" maxLength={100} value={form.customerEmail} onChange={e => setForm({ ...form, customerEmail: e.target.value })} /></label>
                    <label>מספר אנשים<input type="number" min={1} max={10} value={form.numberOfPeople} onChange={e => setForm({ ...form, numberOfPeople: Number(e.target.value) })} /></label>
                    <div className="modal-actions">
                        <button type="submit" disabled={isSaving}>{isSaving ? 'שומר…' : 'אשר תור'}</button>
                        <button type="button" onClick={onClose}>ביטול</button>
                    </div>
                </form>
            </div>
        </div>
    );
}

export default BookAppointment;
