import Rental from './Rental';
import { useSelector, useDispatch } from 'react-redux';
import { useEffect } from 'react';
import Spinner from '../../../components/rental/spinner/Spinner';
import { dismissNotice, getRentals, getReturning, getRenting } from '../../../redux/Rentals/RentalSlice';
import Notice from '../../../components/common/Notice';
import './Rental.css';

function Rentals() {
  const allRentals = useSelector((state: any) => state.rentals.rentals);
  const isLoading = useSelector((state: any) => state.rentals.isLoading);
  const notice = useSelector((state: any) => state.rentals.notice);
  const dispatch = useDispatch();

  useEffect(() => {
    dispatch(getRentals() as any);
    dispatch(getReturning() as any);
    dispatch(getRenting() as any);
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
      {notice && <Notice type={notice.type} text={notice.text} onClose={() => dispatch(dismissNotice())} />}
      <div className="rentals-list">
        {allRentals.map((rental: any) => (
          <Rental key={rental.rentalId} {...rental} />
        ))}
      </div>
    </div>
  )
}

export default Rentals