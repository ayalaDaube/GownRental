import Rental from './Rental';
import Create from '../../../components/rental/create/Create';
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
  const error = useAppSelector((state) => state.rentals.error);
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
  if (error) {
    return (
      <div className="rentals-page" role="alert">
        <p>{error}</p>
        <button onClick={() => dispatch(getRentals())}>Try again</button>
      </div>
    );
  }
  return (
    <div className="rentals-page">
      {notice && <Notice type={notice.type} text={notice.text} onClose={() => dispatch(dismissNotice())} />}
      <Create />
      <div className="rentals-list">
        {allRentals.map((rental) => (
          <Rental key={rental.rentalId} {...rental} />
        ))}
        {allRentals.length === 0 && <p className="rentals-empty">There are no rentals yet.</p>}
      </div>
    </div>
  )
}

export default Rentals