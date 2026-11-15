import { useEffect } from 'react';
import { Link } from 'react-router-dom';
import '../login/Login.css';

/** Shown for an address that does not exist, instead of an empty screen. */
function NotFound() {
  useEffect(() => {
    const previousTitle = document.title;
    document.title = 'Page not found · GownRental';
    return () => {
      document.title = previousTitle;
    };
  }, []);

  return (
    <main className="login-page">
      <div className="login-card">
        <span className="eyebrow">Gown Rental</span>
        <h1>Page not found</h1>
        <p className="login-hint">The page you are looking for does not exist or was moved.</p>
        <Link to="/" className="button button-primary">Back to the home page</Link>
      </div>
    </main>
  );
}

export default NotFound;
