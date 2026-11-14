import GownModel, { GownModelProps } from '../../components/gown/gownModel/GownModel';
import { useSelector, useDispatch } from 'react-redux';
import { useEffect, useState } from 'react';
import Spinner from '../../components/rental/spinner/Spinner';
import Create from '../../components/gown/create/Create';
import { addGownThunk, dismissNotice, fetchGroupsGowns, getAvailableGownsByDate } from '../../redux/Gowns/GownSlice';
import Notice from '../../components/common/Notice';
import './Gown.css';
import PlaceOrder from '../../components/gown/placeOrder/PlaceOrder';

function Gowns() {
  const gowns = useSelector((state: any) => state.gowns.gowns);
  const isLoading = useSelector((state: any) => state.gowns.isLoading);
  const error = useSelector((state: any) => state.gowns.error);
  const cart = useSelector((state: any) => state.gowns.Cart);
  const notice = useSelector((state: any) => state.gowns.notice);
  const dispatch = useDispatch();
  const [searchById, setSearchById] = useState('');
  const [searchBySize, setSearchBySize] = useState('');
  const [isOrderOpen, setIsOrderOpen] = useState(false);
  const [selectedDate, setSelectedDate] = useState('');

  const filterdGowns = gowns.filter((gown: any) =>
   ( gown.id.toString().includes(searchById) | gown.details.some((detail: any) => detail.gownId.toString().includes(searchById))) && gown.details.some((detail: any) => detail.size.toString().includes(searchBySize)));

  const handleSearchById = (event: any) => {
    setSearchById(event.target.value);
  }

  const handleSearchBySize = (event: any) => {
    setSearchBySize(event.target.value);
  }
  const handleAdd = (gown: GownModelProps) => {
    dispatch(addGownThunk(gown) as any);
  };

  useEffect(() => {
    dispatch(fetchGroupsGowns() as any);
  }, []);

  if (isLoading) return <Spinner />;
  if (error) return <div>{error}</div>;


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
          <input type="date" value={selectedDate} onChange={(e) => setSelectedDate(e.target.value)} />
          <button onClick={() => selectedDate ? dispatch(getAvailableGownsByDate(selectedDate) as any) : dispatch(fetchGroupsGowns() as any)}>Check Availability</button>
        </div>
        <Create onAdd={handleAdd} />
      </div>
      <div className="gowns-list">
        {filterdGowns.map((gown: any) => (
          <GownModel key={gown.id} {...gown} />
        ))}
      </div>
      <button className="place-order-btn" onClick={() => setIsOrderOpen(true)}>Place Order ({cart?.length || 0})</button>
      {isOrderOpen && <PlaceOrder cart={cart} onClose={() => setIsOrderOpen(false)} />}
    </div>
  );
} 

export default Gowns;
