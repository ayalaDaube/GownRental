import './RentalsAside.css';
import { useAppDispatch, useAppSelector } from '../../../store/hooks';
import { useEffect } from 'react';
import { getReturning, getRenting } from '../../../redux/Rentals/RentalSlice';

function RentalsAside() {
    const dispatch = useAppDispatch();
    const returns = useAppSelector((state) => state.rentals.returns);
    const pickups = useAppSelector((state) => state.rentals.pickups);

    const rentals = useAppSelector((state) => state.rentals.rentals);

    useEffect(() => {
        dispatch(getReturning());
        dispatch(getRenting());
    }, [dispatch, rentals]);

    return (
        <aside className="rentals-aside">
            <h2 className="aside-title">New for today</h2>

            <section className="aside-section">
                <h3 className="aside-section-title returns">📦 Returns</h3>
                <div className="aside-list">
                    {returns.length === 0 ? <span className="aside-empty">No returns today</span> :
                        returns.map((r) => (
                            <div key={r.rentalId} className="aside-card returns-card">
                                <span>Rental Id: {r.rentalId}</span>
                                <span>Customer Id: {r.customerId}</span>
                                <span>Gown Id: {r.gownId}</span>
                            </div>
                        ))}
                </div>
            </section>

            <section className="aside-section">
                <h3 className="aside-section-title pickups">🛍️ Pick ups</h3>
                <div className="aside-list">
                    {pickups.length === 0 ? <span className="aside-empty">No pick ups today</span> :
                        pickups.map((r) => (
                            <div key={r.rentalId} className="aside-card pickups-card">
                                <span>Rental Id: {r.rentalId}</span>
                                <span>Customer Id: {r.customerId}</span>
                                <span>Gown Id: {r.gownId}</span>
                            </div>
                        ))}
                </div>
            </section>
        </aside>
    );
}

export default RentalsAside;
