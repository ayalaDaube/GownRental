import { createAsyncThunk, createSlice } from '@reduxjs/toolkit';
import { clearSession, loadSession, saveSession } from '../../auth/session';
import http from '../../services/http';

export interface AuthState {
  token: string | null;
  username: string | null;
  role: string | null;
  isLoading: boolean;
  error: string | null;
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
  } catch (error: any) {
    if (error.response?.status === 401) {
      return rejectWithValue('Wrong username or password.');
    }
    if (error.response?.status === 429) {
      return rejectWithValue('Too many attempts. Please wait a minute and try again.');
    }
    return rejectWithValue('Could not reach the server. Please try again.');
  }
});

const authSlice = createSlice({
  name: 'auth',
  initialState,
  reducers: {
    logout: (state) => {
      clearSession();
      state.token = null;
      state.username = null;
      state.role = null;
      state.error = null;
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(login.pending, (state) => {
        state.isLoading = true;
        state.error = null;
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

export const { logout } = authSlice.actions;
export default authSlice.reducer;
