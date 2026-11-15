import Rental from './Rental';
import { useAppDispatch, useAppSelector } from '../../../store/hooks';
import { useEffect } from 'react';
import Spinner from '../../../components/rental/spinner/Spinner';
import { dismissNotice, getRentals, getReturning, getRenting } from '../../../redux/Rentals/RentalSlice';
import Notice from '../../../components/common/Notice';
import './Rental.css';

function Rentals() {
  const allRentals = useAppSelector((state) => state.rentals.rentals);
  const isLoading = useAppSelector((state) => state.rentals.isLoading);
  const notice = useAppSelector((state) => state.rentals.notice);
  const dispatch = useAppDispatch();

  useEffect(() => {
    dispatch(getRentals());
    dispatch(getReturning());
    dispatch(getRenting());
  }, [dispatch]);

  if (isLoading) {
    return (
      <div className="rentals-page" style={{ justifyContent: 'center', alignItems: 'center' }}>
        <Spinner />
      </div>
    )
  }
  return (
    <div className="rentals-page">
      {notice && <Notice type={notice.type} text={notice.text} onClose={() => dispatch(dismissNotice())} />}
      <div className="rentals-list">
        {allRentals.map((rental) => (
          <Rental key={rental.rentalId} {...rental} />
        ))}
      </div>
    </div>
  )
}

export default Rentals