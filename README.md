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
  Dal/             DbContext, entities, data services, database/scripts (SQL)
```

## API overview

Base URL in development: `https://localhost:7265` (Swagger UI at `/swagger`).

| Resource | Endpoints |
|---|---|
| Gowns | `GET /api/Gowns`, `GET /api/Gowns/groups`, `GET /api/Gowns/byDate?date=YYYY-MM-DD`, `GET /api/Gowns/{id}`, `POST`, `PUT`, `DELETE /api/Gowns/{id}` |
| Rentals | `GET /api/Rentals`, `GET /api/Rentals/{id}`, `GET /api/Rentals/returning`, `GET /api/Rentals/renting`, `POST`, `PUT`, `DELETE /api/Rentals/{id}` |
| Appointments | `GET /api/Appointment`, `GET /api/Appointment/{id}`, `GET /api/Appointment/getByDate/{date}`, `GET /api/Appointment/getAvailableAppointments`, `POST`, `DELETE /api/Appointment/{id}` |

## Getting started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js](https://nodejs.org/) 18 or newer
- SQL Server LocalDB (installed with Visual Studio, or with SQL Server Express)

### Database

The database is a LocalDB file, `Server/Dal/database/DB.mdf`. Database files are not stored in Git. Reproducible setup (EF Core migrations and demo data) is the next roadmap item; until it lands, place a `DB.mdf` in that folder and apply the SQL scripts in `Server/Dal/database/scripts` in order.

The connection string is read from `ConnectionStrings:DefaultConnection` in `Server/Server/appsettings.json`.

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

The client runs at http://localhost:3000 and calls the API at `https://localhost:7265`. If the browser blocks the API certificate, trust the ASP.NET Core development certificate once with `dotnet dev-certs https --trust`.

## Status and roadmap

The core flows work end to end. The goal is a polished product, in this order:

- [x] Single repository with the full history of all contributors
- [x] Connection string moved out of source code
- [ ] Reproducible database: EF Core migrations and demo data
- [ ] Real authentication and roles (secretary vs. customer) instead of a client-side password
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
