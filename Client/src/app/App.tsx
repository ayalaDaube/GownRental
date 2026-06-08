import React from 'react';
import './App.css';
import Home from '../pages/home/Home';
import Rentals from '../pages/rentals/rentalview/Rentals';
import RentalsAside from '../pages/rentals/rentalAside/RentalsAside';
import Gowns from '../pages/gowns/Gowns';
import SecretaryHome from '../pages/secretary/SecretaryHome';
import { Route, Routes, Navigate } from 'react-router-dom'; 
import EmployeeAppointments from '../pages/appointments/EmployeeAppointments';
import Appointments from '../pages/appointments/appointments';

function App() {
  return (
    <div className="app-layout">
        <Routes>
          <Route path="/" element={<Home />} />
          <Route path="/rentals" element={<><main className="app-main"><Rentals /></main><RentalsAside /></>} />
          <Route path="/gowns" element={<Gowns />} />
          <Route path="/secretary" element={<SecretaryHome />} />
        <Route path="/appointments" element={<Appointments />} />
        <Route path="/employee/appointments" element={<EmployeeAppointments />} />
        </Routes>
    </div>
  );
}

export default App;
