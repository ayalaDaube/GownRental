// import React from 'react';
import './App.css';
import Rentals from '../pages/rentals/rentalview/Rentals';
import RentalsAside from '../pages/rentals/rentalAside/RentalsAside';
import Gowns from '../pages/gowns/Gowns';
import { Route, Routes} from 'react-router-dom'; 
function App() {
  return (
    <div className="app-layout">
        <Routes>
          <Route path="/rentals" element={<><main className="app-main"><Rentals /></main><RentalsAside /></>} />
          <Route path="/gowns" element={<Gowns />} />
        </Routes>
    </div>
  );
}

export default App;
