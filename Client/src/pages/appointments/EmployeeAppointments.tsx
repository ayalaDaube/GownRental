import React, { useEffect, useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { fetchAllAppointments, deleteAppointmentThunk, dismissNotice } from '../../redux/Appointments/AppointmentSlice';
import Notice from '../../components/common/Notice';
import { RootState } from '../../store/store';
import { AppointmentType } from '../../redux/Appointments/AppointmentType';
import './appointments.css';

function EmployeeAppointments() {
    const dispatch = useDispatch<any>();
    const { all, isLoading, notice } = useSelector((state: RootState) => state.appointments);
    const [searchId, setSearchId] = useState('');

    useEffect(() => {
        dispatch(fetchAllAppointments());
    }, [dispatch]);

    // A cancelled slot is still listed, now as free, so the list is reloaded after every cancellation.
    const handleCancel = async (id: number) => {
        if (!window.confirm('Cancel this appointment?')) return;
        await dispatch(deleteAppointmentThunk(id));
        dispatch(fetchAllAppointments());
    };

    const filtered = all.filter(a =>
        searchId === '' || (a.customerId ?? '').includes(searchId)
    );

    const grouped = filtered.reduce((acc: Record<string, AppointmentType[]>, a) => {
        const date = a.appointmentDate;
        if (!acc[date]) acc[date] = [];
        acc[date].push(a);
        return acc;
    }, {});

    const sortedDates = Object.keys(grouped).sort();

    return (
        <div className="appointments-page">
            <h2>ניהול תורים</h2>
            {notice && <Notice type={notice.type} text={notice.text} onClose={() => dispatch(dismissNotice())} />}
            <div className="employee-filters">
                <input
                    placeholder="חיפוש לפי תעודת זהות"
                    value={searchId}
                    onChange={e => setSearchId(e.target.value)}
                />
            </div>
            {isLoading && <p>טוען...</p>}
            {sortedDates.length === 0 && !isLoading && <p className="no-appointments">אין תורים</p>}
            {sortedDates.map(date => (
                <div key={date} className="date-group">
                    <h3 className="date-group-title">{date}</h3>
                    <div className="table-wrapper">
                        <table className="appointments-table">
                            <thead>
                                <tr>
                                    <th>שעה</th>
                                    <th>שם לקוח</th>
                                    <th>טלפון</th>
                                    <th>ת.ז.</th>
                                    <th>מספר אנשים</th>
                                    <th>סטטוס</th>
                                    <th>ביטול</th>
                                </tr>
                            </thead>
                            <tbody>
                                {grouped[date].map(a => (
                                    <tr key={a.id}>
                                        <td>{a.hour}</td>
                                        <td>{a.customerName || '-'}</td>
                                        <td>{a.customerPhone || '-'}</td>
                                        <td>{a.customerId || '-'}</td>
                                        <td>{a.numberOfPeople || '-'}</td>
                                        <td>
                                            <span className={a.isAvailable ? 'status-available' : 'status-booked'}>
                                                {a.isAvailable ? 'פנוי' : 'תפוס'}
                                            </span>
                                        </td>
                                        <td>
                                            {!a.isAvailable && (
                                                <button className="delete-btn" onClick={() => handleCancel(a.id)}>בטל</button>
                                            )}
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                </div>
            ))}
        </div>
    );
}

export default EmployeeAppointments;
