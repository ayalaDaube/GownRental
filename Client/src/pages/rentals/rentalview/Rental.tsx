import './Rental.css';
import DeleteButton from '../../../components/rental/deleteButton/DeleteButton';
import Update from '../../../components/rental/update/Update';

export interface RentalProps {
    rentalId: number,
    customerId: number,
    rentalDate: string,
    eventDate: string,
    gownId: number,
    size: number
}

function Rental({ rentalId, customerId, rentalDate, eventDate, gownId, size }: RentalProps) {
    return (
        <div className="rental-card">
            <div className="rental-field"><span>Rental ID</span><strong>{rentalId}</strong></div>
            <div className="rental-field"><span>Customer ID</span><strong>{customerId}</strong></div>
            <div className="rental-field"><span>Rental Date</span><strong>{rentalDate}</strong></div>
            <div className="rental-field"><span>Event Date</span><strong>{eventDate}</strong></div>
            <div className="rental-field"><span>Gown ID</span><strong>{gownId}</strong></div>
            <div className="rental-field"><span>Size</span><strong>{size}</strong></div>
            <div className="rental-actions">
                <Update rental={{ rentalId, customerId, rentalDate, eventDate, gownId, size }} />
                <DeleteButton rentalId={rentalId} />
            </div>
        </div>
    )
}
export default Rental