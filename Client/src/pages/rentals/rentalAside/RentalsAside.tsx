import './RentalsAside.css';
import { useSelector, useDispatch } from 'react-redux';
import { useEffect } from 'react';
import { getReturning, getRenting } from '../../../redux/Rentals/RentalSlice';

function RentalsAside() {
    const dispatch = useDispatch();
    const returns = useSelector((state: any) => state.rentals.returns);
    const pickups = useSelector((state: any) => state.rentals.pickups);

    const rentals = useSelector((state: any) => state.rentals.rentals);

    useEffect(() => {
        dispatch(getReturning() as any);
        dispatch(getRenting() as any);
    }, [rentals]);

    return (
        <aside className="rentals-aside">
            <h2 className="aside-title">New for today</h2>

            <section className="aside-section">
                <h3 className="aside-section-title returns">📦 Returns</h3>
                <div className="aside-list">
                    {returns.length === 0 ? <span className="aside-empty">No returns today</span> :
                        returns.map((r: any) => (
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
                        pickups.map((r: any) => (
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
