# GownRental

A full-stack web application for managing an evening-gown rental business: a gown catalog with per-size inventory, rentals with date-based availability, and fitting appointments, with separate views for customers and for the shop secretary.

It was built as a team study project for a client who rents out evening gowns for events, and is now being developed further toward a production-quality product (see [Status](#status-and-roadmap)).

## Features

**Customers**
- Browse the gown collection grouped by model, with sizes and prices
- Check which gowns are free on a chosen event date
- Add gowns to a cart and place a rental order
- Book a fitting appointment from the list of free slots

**Secretary**
- Manage the gown inventory: add, edit and delete gowns and sizes
- Create, update and delete rentals
- See which customers are coming to pick up and which are returning gowns (three days before and after the event)
- Manage fitting appointments by day and cancel bookings

## Tech stack

| Layer | Technology |
|---|---|
| Client | React 18, TypeScript, Redux Toolkit, React Router 7, Axios (Create React App) |
| API | ASP.NET Core 8 Web API, Swagger / OpenAPI |
| Business logic | `Bl` class library |
| Data access | `Dal` class library, Entity Framework Core 8 |
| Database | SQL Server (LocalDB for development) |

## Architecture

```
Client (React)  ──HTTP/JSON──▶  Server (ASP.NET Core controllers)
                                      │
                                      ▼
                                 Bl  (business rules, validation, mapping to DTOs)
                                      │
                                      ▼
                                 Dal (EF Core services over DataManager DbContext)
                                      │
                                      ▼
                                SQL Server LocalDB
```

```
Client/            React application (src/pages, src/components, src/redux, src/services)
Server/
  Server/          Web API: controllers, Program.cs, configuration
  Bl/              Business logic services and DTOs
  Dal/             DbContext, entities, data services, EF Core migrations, demo data seeder
```

## API overview

Base URL in development: `https://localhost:7265` (Swagger UI at `/swagger`).

| Resource | Endpoints |
|---|---|
| Gowns | `GET /api/Gowns`, `GET /api/Gowns/groups`, `GET /api/Gowns/byDate?date=YYYY-MM-DD`, `GET /api/Gowns/{id}`, `POST`, `PUT`, `DELETE /api/Gowns/{id}` |
| Rentals | `GET /api/Rentals`, `GET /api/Rentals/{id}`, `GET /api/Rentals/returning`, `GET /api/Rentals/renting`, `POST`, `PUT`, `DELETE /api/Rentals/{id}` |
| Appointments | `GET /api/Appointment`, `GET /api/Appointment/{id}`, `GET /api/Appointment/getByDate/{date}`, `GET /api/Appointment/getAvailableAppointments`, `POST`, `DELETE /api/Appointment/{id}` |
| Auth | `POST /api/Auth/login`, `GET /api/Auth/me` |

### Authentication

Staff sign in with a username and password and receive a signed [JWT](https://jwt.io/introduction). The client sends it as `Authorization: Bearer <token>` on every request.

- **Public (no sign-in):** `GET /api/Appointment/getAvailableAppointments` and `POST /api/Appointment`, so visitors can book a fitting, plus `POST /api/Auth/login`.
- **Everything else requires the `Secretary` role.** The API is secure by default: a new endpoint is protected unless it is explicitly marked `[AllowAnonymous]`.
- Passwords are stored only as salted hashes (ASP.NET `PasswordHasher`). Login attempts are rate limited per client address.
- In the Swagger UI, call `POST /api/Auth/login`, then press **Authorize** and paste the token to try protected endpoints.

**Development account.** In the Development environment the API creates one demo secretary on startup. Its username and password are defined under `Seed:Secretary` in `Server/Server/appsettings.Development.json`, together with a development-only signing key under `Jwt:Key`. These values exist for local use only.

**Production.** Never reuse the development values. Provide a random signing key of at least 32 characters outside of source control, for example with an environment variable (`Jwt__Key`) or `dotnet user-secrets`, and set `Cors:AllowedOrigins` to the real client address. The API refuses to start without a valid signing key. Create the first staff account through your own provisioning process, since demo data and the demo account are only created in Development.

## Getting started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js](https://nodejs.org/) 18 or newer
- SQL Server LocalDB (installed with Visual Studio, or with SQL Server Express)

### Database

No manual setup is needed. On startup in the Development environment the API creates a LocalDB database named `GownRental`, applies the EF Core migrations and fills it with fictional demo data: gowns in several sizes, customers, rentals due for pick-up and return, and free fitting appointments for the next three weeks. Dates are relative to today, so the demo is always populated. The seeder only adds what is missing, so restarting does not duplicate anything.

The connection string is read from `ConnectionStrings:DefaultConnection` in `Server/Server/appsettings.json`. To start over, drop the database:

```bash
cd Server
dotnet tool restore
dotnet ef database drop --project Dal --startup-project Server --context Dal.Models.DataManager
```

Changing the model? Add a migration (the `dotnet-ef` tool is pinned in `Server/.config/dotnet-tools.json`):

```bash
cd Server
dotnet ef migrations add <Name> --project Dal --startup-project Server --context Dal.Models.DataManager
```

### Run the API

```bash
cd Server/Server
dotnet run --launch-profile https
```

### Run the client

```bash
cd Client
npm ci
npm start
```

The client runs at http://localhost:3000 and calls the API at `https://localhost:7265`. To use another API address, copy `Client/.env.example` to `Client/.env.local` and change `REACT_APP_API_URL`. If the browser blocks the API certificate, trust the ASP.NET Core development certificate once with `dotnet dev-certs https --trust`.

Staff sign in at http://localhost:3000/login. Visitors can use the home page and appointment booking without an account.

### Run the tests

```bash
cd Server
dotnet test            # API integration tests (authentication and authorization)

cd ../Client
npm test               # client tests (session, sign-in state, protected routes)
```

## Status and roadmap

The core flows work end to end. The goal is a polished product, in this order:

- [x] Single repository with the full history of all contributors
- [x] Connection string moved out of source code
- [x] Reproducible database: EF Core migrations and demo data
- [x] Real authentication (JWT) and role-based authorization instead of a client-side password
- [ ] Bug fixes in deletion, order placement and availability logic
- [ ] Code cleanup and strict typing in the client
- [ ] Automated tests and continuous integration
- [ ] Right-to-left layout and real gown images

## Team

| Contributor | Area |
|---|---|
| Ayala Daube ([@ayalaDaube](https://github.com/ayalaDaube)) | Gown catalog and inventory, availability by date, order cart, client/server project structure |
| Esther Joseph ([@EstherJoseph7](https://github.com/EstherJoseph7)) | Rentals, home pages, project integration |
| Naomi Britman | Fitting appointments |

The project began in [EstherJoseph7/GownRental-project](https://github.com/EstherJoseph7/GownRental-project); this repository keeps its complete commit history.

## License

[MIT](LICENSE)
