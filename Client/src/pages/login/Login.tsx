import { FormEvent, useEffect, useState } from 'react';
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom';
import { useDispatch, useSelector } from 'react-redux';
import { login } from '../../redux/Auth/AuthSlice';
import { AppDispatch, RootState } from '../../store/store';
import './Login.css';

interface LocationState {
  from?: { pathname?: string };
}

function Login() {
  const dispatch = useDispatch<AppDispatch>();
  const navigate = useNavigate();
  const location = useLocation();
  const { token, isLoading, error } = useSelector((state: RootState) => state.auth);

  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');

  // Send the user back to the page they originally asked for, or to the dashboard.
  const destination = (location.state as LocationState | null)?.from?.pathname ?? '/secretary';

  useEffect(() => {
    const previousTitle = document.title;
    document.title = 'Staff sign-in · GownRental';
    return () => {
      document.title = previousTitle;
    };
  }, []);

  if (token) {
    return <Navigate to={destination} replace />;
  }

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault();
    const result = await dispatch(login({ username: username.trim(), password }));
    if (login.fulfilled.match(result)) {
      navigate(destination, { replace: true });
    }
  };

  return (
    <main className="login-page">
      <form className="login-card" onSubmit={handleSubmit}>
        <span className="eyebrow">Gown Rental</span>
        <h1>Staff sign-in</h1>
        <p className="login-hint">Sign in to manage gowns, rentals and appointments.</p>

        <label htmlFor="username">Username</label>
        <input
          id="username"
          name="username"
          autoComplete="username"
          autoFocus
          required
          value={username}
          onChange={(event) => setUsername(event.target.value)}
        />

        <label htmlFor="password">Password</label>
        <input
          id="password"
          name="password"
          type="password"
          autoComplete="current-password"
          required
          value={password}
          onChange={(event) => setPassword(event.target.value)}
        />

        {error && (
          <p className="login-error" role="alert">
            {error}
          </p>
        )}

        <button type="submit" className="button button-primary" disabled={isLoading}>
          {isLoading ? 'Signing in…' : 'Sign in'}
        </button>

        <Link to="/" className="login-back">
          ← Back to the home page
        </Link>
      </form>
    </main>
  );
}

export default Login;
