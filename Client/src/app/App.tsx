import React, { useEffect } from 'react';
import './App.css';
import { useAppDispatch } from '../store/hooks';
import { Route, Routes, useNavigate } from 'react-router-dom';
import Home from '../pages/home/Home';
import Login from '../pages/login/Login';
import Rentals from '../pages/rentals/rentalview/Rentals';
import RentalsAside from '../pages/rentals/rentalAside/RentalsAside';
import Gowns from '../pages/gowns/Gowns';
import SecretaryHome from '../pages/secretary/SecretaryHome';
import EmployeeAppointments from '../pages/appointments/EmployeeAppointments';
import Appointments from '../pages/appointments/appointments';
import NotFound from '../pages/notfound/NotFound';
import ProtectedRoute from '../components/auth/ProtectedRoute';
import { sessionExpired } from '../redux/Auth/AuthSlice';
import { AUTH_EXPIRED_EVENT } from '../services/http';

function App() {
  const dispatch = useAppDispatch();
  const navigate = useNavigate();

  // The server rejected our token (expired or invalid): sign out and ask the user to sign in again.
  useEffect(() => {
    const handleExpired = () => {
      dispatch(sessionExpired());
      navigate('/login', { replace: true });
    };
    window.addEventListener(AUTH_EXPIRED_EVENT, handleExpired);
    return () => window.removeEventListener(AUTH_EXPIRED_EVENT, handleExpired);
  }, [dispatch, navigate]);

  return (
    <div className="app-layout">
      <Routes>
        {/* Public: visitors can read the home page and book a fitting appointment. */}
        <Route path="/" element={<Home />} />
        <Route path="/appointments" element={<Appointments />} />
        <Route path="/login" element={<Login />} />

        {/* Staff only: require signing in. */}
        <Route path="/secretary" element={<ProtectedRoute><SecretaryHome /></ProtectedRoute>} />
        <Route path="/gowns" element={<ProtectedRoute><Gowns /></ProtectedRoute>} />
        <Route
          path="/rentals"
          element={
            <ProtectedRoute>
              <>
                <main className="app-main"><Rentals /></main>
                <RentalsAside />
              </>
            </ProtectedRoute>
          }
        />
        <Route
          path="/employee/appointments"
          element={<ProtectedRoute><EmployeeAppointments /></ProtectedRoute>}
        />

        {/* Any other address. */}
        <Route path="*" element={<NotFound />} />
      </Routes>
    </div>
  );
}

export default App;
