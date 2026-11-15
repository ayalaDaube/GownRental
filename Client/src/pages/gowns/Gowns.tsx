import React, { useEffect, useState } from 'react';
import GownModel from '../../components/gown/gownModel/GownModel';
import Spinner from '../../components/rental/spinner/Spinner';
import Create from '../../components/gown/create/Create';
import Notice from '../../components/common/Notice';
import PlaceOrder from '../../components/gown/placeOrder/PlaceOrder';
import { addGownThunk, dismissNotice, fetchGroupsGowns, getAvailableGownsByDate } from '../../redux/Gowns/GownSlice';
import { GownModelProps } from '../../redux/Gowns/GownTypes';
import { NewGownInput } from '../../services/Gowns/task.service';
import { useAppDispatch, useAppSelector } from '../../store/hooks';
import './Gown.css';

function Gowns() {
  const dispatch = useAppDispatch();
  const { gowns, isLoading, error, notice, Cart: cart } = useAppSelector((state) => state.gowns);
  const [searchById, setSearchById] = useState('');
  const [searchBySize, setSearchBySize] = useState('');
  const [isOrderOpen, setIsOrderOpen] = useState(false);
  const [selectedDate, setSelectedDate] = useState('');

  // A model is shown when its id or the id of one of its sizes matches, and one of its sizes has the typed size.
  const matchesSearch = (gown: GownModelProps) => {
    const matchesId = gown.id.toString().includes(searchById)
      || gown.details.some(detail => detail.gownId.toString().includes(searchById));
    const matchesSize = gown.details.some(detail => detail.size.toString().includes(searchBySize));
    return matchesId && matchesSize;
  };
  const filteredGowns = gowns.filter(matchesSearch);

  const handleSearchById = (event: React.ChangeEvent<HTMLInputElement>) => {
    setSearchById(event.target.value);
  };

  const handleSearchBySize = (event: React.ChangeEvent<HTMLInputElement>) => {
    setSearchBySize(event.target.value);
  };

  // Shows what is available on the chosen date, or all gowns when no date is chosen.
  const refresh = () => {
    if (selectedDate) {
      dispatch(getAvailableGownsByDate(selectedDate));
    } else {
      dispatch(fetchGroupsGowns());
    }
  };

  // Resolves to true when the gown was saved, so the form knows whether it can close.
  const handleAdd = async (input: NewGownInput): Promise<boolean> => {
    const result = await dispatch(addGownThunk(input));
    if (addGownThunk.fulfilled.match(result)) {
      dispatch(fetchGroupsGowns());
      return true;
    }
    return false;
  };

  useEffect(() => {
    dispatch(fetchGroupsGowns());
  }, [dispatch]);

  if (isLoading) return <Spinner />;
  if (error) {
    return (
      <div className="gowns-page" role="alert">
        <p>{error}</p>
        <button onClick={refresh}>Try again</button>
      </div>
    );
  }

  return (
    <div className="gowns-page">
      <h1 className="gowns-page-title">Gowns Collection</h1>
      <p className="gowns-page-subtitle">✦ Find Your Perfect Gown ✦</p>
      {notice && <Notice type={notice.type} text={notice.text} onClose={() => dispatch(dismissNotice())} />}
      <div className="gowns-controls">
        <div className="gowns-search-bar">
          <input type="text" placeholder="Search by ID or Gown ID" value={searchById} onChange={handleSearchById} />
          <input placeholder="Search by Size" value={searchBySize} onChange={handleSearchBySize} type="number" />
        </div>
        <div className="gowns-date-search">
          <input type="date" aria-label="Event date to check" value={selectedDate} onChange={(e) => setSelectedDate(e.target.value)} />
          <button onClick={refresh}>Check Availability</button>
        </div>
        <Create onAdd={handleAdd} />
      </div>
      <div className="gowns-list">
        {filteredGowns.map((gown) => (
          <GownModel key={gown.id} {...gown} />
        ))}
        {filteredGowns.length === 0 && (
          <p className="gowns-empty">
            {gowns.length === 0 ? 'There are no gowns yet. Add the first one with the button above.' : 'No gowns match your search.'}
          </p>
        )}
      </div>
      <button className="place-order-btn" onClick={() => setIsOrderOpen(true)}>Place Order ({cart.length})</button>
      {isOrderOpen && (
        <PlaceOrder
          cart={cart}
          defaultEventDate={selectedDate}
          onClose={() => setIsOrderOpen(false)}
          onPlaced={refresh}
        />
      )}
    </div>
  );
}

export default Gowns;
