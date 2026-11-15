# GownRental client

The React front end of GownRental: a visitor page with appointment booking, and a staff area for gowns, orders, rentals and appointments. See the [main README](../README.md) for what the product does and how the whole system runs.

## Run

```bash
npm ci
npm start      # http://localhost:3000, expects the API at https://localhost:7265
```

To use another API address, copy `.env.example` to `.env.local` and change `REACT_APP_API_URL`.

## Scripts

| Command | What it does |
|---|---|
| `npm start` | development server with reload |
| `npm test` | unit tests (watch mode; add `-- --watchAll=false` for a single run) |
| `npm run build` | production build into `build/` |

## Structure

```
src/
  app/          root component and routes
  auth/         the stored sign-in session
  components/   reusable pieces (gown, rental, appointment forms, notices, protected route)
  pages/        one folder per screen
  redux/        state: one slice per area (auth, gowns, rentals, appointments)
  services/     the HTTP client and the calls to the API
  store/        the Redux store
```
