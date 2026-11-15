import React, { useEffect, useState } from 'react';
import { useAppDispatch, useAppSelector } from '../../store/hooks';
import { fetchAvailableAppointments } from '../../redux/Appointments/AppointmentSlice';
import { AppointmentType } from '../../redux/Appointments/AppointmentType';
import BookAppointment from '../../components/appointment/BookAppointment';
import './appointments.css';

function Appointments() {
    const dispatch = useAppDispatch();
    const { available, isLoading, error } = useAppSelector((state) => state.appointments);
    const [selected, setSelected] = useState<AppointmentType | null>(null);

    useEffect(() => {
        dispatch(fetchAvailableAppointments());
    }, [dispatch]);

    return (
        <div className="appointments-page">
            <h2>תורים פנויים</h2>
            {isLoading && <p>טוען...</p>}
            {error && (
                <div role="alert">
                    <p>{error}</p>
                    <button className="apt-book-btn" onClick={() => dispatch(fetchAvailableAppointments())}>נסו שוב</button>
                </div>
            )}
            <div className="appointments-grid">
                {available.length === 0 && !isLoading && !error && (
                    <p className="no-appointments">אין תורים פנויים</p>
                )}
                {available.map(a => (
                    <div key={a.id} className="appointment-card">
                        <div className="apt-field"><span>תאריך</span><strong>{a.appointmentDate}</strong></div>
                        <div className="apt-field"><span>שעה</span><strong>{a.hour}</strong></div>
                        <button className="apt-book-btn" onClick={() => setSelected(a)}>הזמן תור</button>
                    </div>
                ))}
            </div>
            {selected && <BookAppointment appointment={selected} onClose={() => setSelected(null)} />}
        </div>
    );
}

export default Appointments;
