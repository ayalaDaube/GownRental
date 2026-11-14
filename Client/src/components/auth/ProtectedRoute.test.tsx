import { render, screen } from '@testing-library/react';
import { Provider } from 'react-redux';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { configureStore } from '@reduxjs/toolkit';
import ProtectedRoute from './ProtectedRoute';

// Only the auth state matters here, so a tiny reducer stands in for the real slice (and for axios).
const renderAt = (path: string, token: string | null) => {
  const store = configureStore({
    reducer: { auth: (state = { token }) => state },
  });

  render(
    <Provider store={store}>
      <MemoryRouter initialEntries={[path]}>
        <Routes>
          <Route path="/login" element={<div>Login page</div>} />
          <Route path="/gowns" element={<ProtectedRoute><div>Secret gown inventory</div></ProtectedRoute>} />
        </Routes>
      </MemoryRouter>
    </Provider>
  );
};

test('a visitor without a token is sent to the sign-in page', () => {
  renderAt('/gowns', null);

  expect(screen.getByText('Login page')).toBeInTheDocument();
  expect(screen.queryByText('Secret gown inventory')).not.toBeInTheDocument();
});

test('a signed-in user sees the protected page', () => {
  renderAt('/gowns', 'abc.def.ghi');

  expect(screen.getByText('Secret gown inventory')).toBeInTheDocument();
});
