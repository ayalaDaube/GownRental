import { createAsyncThunk, createSlice } from '@reduxjs/toolkit';
import { clearSession, loadSession, saveSession } from '../../auth/session';
import http from '../../services/http';
import { getErrorStatus } from '../../services/apiError';

export interface AuthState {
  token: string | null;
  username: string | null;
  role: string | null;
  isLoading: boolean;
  error: string | null;
  /** True right after the user chose to sign out, so protected pages send them home and not to the sign-in page. */
  justSignedOut: boolean;
}

interface LoginResponse {
  token: string;
  expiresAtUtc: string;
  username: string;
  role: string;
}

const stored = loadSession();

const initialState: AuthState = {
  token: stored?.token ?? null,
  username: stored?.username ?? null,
  role: stored?.role ?? null,
  isLoading: false,
  error: null,
  justSignedOut: false,
};

export const login = createAsyncThunk<
  LoginResponse,
  { username: string; password: string },
  { rejectValue: string }
>('auth/login', async (credentials, { rejectWithValue }) => {
  try {
    const response = await http.post<LoginResponse>('/Auth/login', credentials);
    saveSession(response.data);
    return response.data;
  } catch (error) {
    const status = getErrorStatus(error);
    if (status === 401) {
      return rejectWithValue('Wrong username or password.');
    }
    if (status === 429) {
      return rejectWithValue('Too many attempts. Please wait a minute and try again.');
    }
    return rejectWithValue('Could not reach the server. Please try again.');
  }
});

const authSlice = createSlice({
  name: 'auth',
  initialState,
  reducers: {
    /** The user chose to sign out. */
    logout: (state) => {
      clearSession();
      state.token = null;
      state.username = null;
      state.role = null;
      state.error = null;
      state.justSignedOut = true;
    },
    /** The server ended the session (it expired or was refused): the user must sign in again. */
    sessionExpired: (state) => {
      clearSession();
      state.token = null;
      state.username = null;
      state.role = null;
      state.error = null;
      state.justSignedOut = false;
    },
    /** The home page was shown after signing out, so the next protected address leads to the sign-in page again. */
    acknowledgeSignOut: (state) => {
      state.justSignedOut = false;
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(login.pending, (state) => {
        state.isLoading = true;
        state.error = null;
        state.justSignedOut = false;
      })
      .addCase(login.fulfilled, (state, action) => {
        state.isLoading = false;
        state.token = action.payload.token;
        state.username = action.payload.username;
        state.role = action.payload.role;
      })
      .addCase(login.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.payload ?? 'Sign-in failed.';
      });
  },
});

export const { logout, sessionExpired, acknowledgeSignOut } = authSlice.actions;
export default authSlice.reducer;
