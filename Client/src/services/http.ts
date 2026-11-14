import axios from 'axios';
import { clearSession, getToken } from '../auth/session';

// The API address comes from REACT_APP_API_URL (see .env.example); the default matches the local dev server.
export const API_URL = process.env.REACT_APP_API_URL ?? 'https://localhost:7265';

/** Event fired when the server rejects the stored token, so the app can sign the user out. */
export const AUTH_EXPIRED_EVENT = 'auth:expired';

/** The one Axios instance every service uses: shared base address and automatic token. */
const http = axios.create({ baseURL: `${API_URL}/api` });

http.interceptors.request.use((config) => {
  const token = getToken();
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

http.interceptors.response.use(
  (response) => response,
  (error) => {
    // 401 on a request that carried a token means it expired or is invalid. Failed logins are not affected,
    // because no token is stored while signing in.
    if (error.response?.status === 401 && getToken()) {
      clearSession();
      window.dispatchEvent(new Event(AUTH_EXPIRED_EVENT));
    }
    return Promise.reject(error);
  }
);

export default http;
