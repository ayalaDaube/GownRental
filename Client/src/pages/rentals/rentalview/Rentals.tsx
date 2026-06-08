import Rental from './Rental';
import { useSelector, useDispatch } from 'react-redux';
import { useEffect } from 'react';
import Spinner from '../../../components/rental/spinner/Spinner';
import { getRentals, getReturning, getRenting } from '../../../redux/Rentals/RentalSlice';
import './Rental.css';

function Rentals() {
  const allRentals = useSelector((state: any) => state.rentals.rentals);
  const isLoading = useSelector((state: any) => state.rentals.isLoading);
  const dispatch = useDispatch();

  useEffect(() => {
    dispatch(getRentals() as any);
  }, []);

  if (isLoading) {
    return (
      <div className="rentals-page" style={{ justifyContent: 'center', alignItems: 'center' }}>
        <Spinner />
      </div>
    )
  }
  return (
    <div className="rentals-page">
      <div className="rentals-list">
        {allRentals.map((rental: any) => (
          <Rental key={rental.rentalId} {...rental} />
        ))}
      </div>
    </div>
  )
}

export default Rentals