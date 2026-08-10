# Invoice Builder

An app for turning customer/order data into branded, accurate PDF invoices — create Senders (the business issuing invoices) and Customers (who gets billed), build invoices with line items and tax, and download a print-ready PDF.

This project is being built incrementally as a learning exercise, with every architectural decision explained and logged as it's made. See [DECISIONS.md](DECISIONS.md) for the full reasoning behind each choice, including the ones that didn't pan out and why.

## Tech Stack

**Backend**
- ASP.NET Core 10 — Minimal APIs, no MVC/Controllers
- Entity Framework Core (Npgsql provider) — code-first migrations
- FluentValidation — request validation, mirrored client-side where it matters
- IronPDF — HTML-to-PDF rendering with PDF/UA tagging and digital signing (see [Known Limitations](#known-limitations))
- xUnit — unit and integration tests

**Frontend**
- Angular 21 — standalone components, no NgModules
- TypeScript
- Tailwind CSS 4
- Signals for state (in services), Reactive Forms for anything with validation
- Vitest — component and service tests

**Database:** PostgreSQL 16

**Deployment (local):** Docker Compose — one command brings up Postgres, the API, and the Angular app together

## Architecture

The backend is a **modular monolith**: one deployable API, but internally split into modules with real boundaries, so any module could be extracted into its own service later without a rewrite.

```
backend/src/
  InvoiceBuilder.Api/         entry point — Program.cs wires every module together
  InvoiceBuilder.Invoices/    the only fully-built module: Customers, Senders, Invoices, PDF generation
  InvoiceBuilder.Users/       stub — accounts, roles, authentication
  InvoiceBuilder.Payments/    stub — payment records, status, methods
  InvoiceBuilder.Reports/     stub — summaries, dashboard metrics
  InvoiceBuilder.Shared/      cross-module primitives (paging types, etc.)
```

Each module exposes two extension methods that `Program.cs` calls — `Add<Module>Module(services)` for DI registration and `Map<Module>Module(app)` for endpoint routes — so `Program.cs` stays a thin composition root instead of accumulating logic itself.

Within `InvoiceBuilder.Invoices`:

```
Contracts/     DTOs (request/response records) + FluentValidation validators — never the same
               shape as the EF entities, so the API's public contract can evolve independently
               of the database schema
Data/          DbContext, EF entity configurations, migrations
Domain/        Invoice, Customer, Sender, InvoiceLineItem — plain entities with real behavior
               (e.g. Invoice.RecalculateTotals()), not anemic data bags
Endpoints/     Minimal API route handlers, one file per resource
Pdf/           HTML template generation + IronPDF rendering pipeline
Services/      InvoiceNumberGenerator (sequential, per-year numbering)
```

The frontend mirrors this by feature, not by file type:

```
frontend/src/app/
  core/
    models/      TypeScript interfaces mirroring the backend DTOs exactly
    services/    CustomerService, SenderService, InvoiceService — each owns its
                 state as signals, updated from HttpClient calls
  customers/     list page + create/edit form (one form component serves both)
  senders/       same pattern
  invoices/      same pattern, plus the line-item FormArray and live totals panel
  shared/        ConfirmDialog, Pager, and small stateless helpers
                 (downloadBlob, extractErrorMessage) reused across features
```

Routes are lazy-loaded per feature (`loadComponent`), so the initial bundle only contains the app shell.

## Features

- **Senders & Customers** — paged list, create, edit, delete (with confirmation)
- **Invoices** — paged list, create, edit, delete (with confirmation); dynamic line items with a live subtotal/tax/total panel that recalculates as you type
- **PDF generation** — invoice → HTML → PDF/UA-tagged, digitally-signed PDF, with pagination that holds up for one line item or a few hundred (see limitation below)
- **Client + server validation** — every rule enforced by the backend (FluentValidation) is mirrored in the Angular forms for instant feedback, with the backend as the actual authority

## Known Limitations

Documented in detail in [DECISIONS.md](DECISIONS.md) rather than worked around:

- **PDF generation needs an IronPDF license key.** There's no free tier that renders without one — set `IronPdf:LicenseKey` in `appsettings.Development.json` or the `IronPdf__LicenseKey` environment variable to enable it.
- **PDF generation doesn't work in the Docker container on Apple Silicon.** IronPDF's Linux Chrome engine ships `linux-x64` binaries only — no `linux-arm64` build exists. On an ARM Mac, `dotnet run` locally works fine (once licensed); the Dockerized `api` container will not, unless it's built for `linux/amd64` (via emulation) or actually deployed to an x64 host.
- **Users/Payments/Reports are stubs.** Registered and wired into the app so the module boundary exists, but hold no real logic yet — deferred per the project roadmap until the core invoice flow was solid.

## Getting Started

**Requirements:** Docker Desktop (with Compose). That's it for the standard path — the API, database, and frontend all build inside containers.

```bash
git clone <this-repo>
cd InvoiceBuilder
docker compose up -d --build
```

Then open **http://localhost:4200**. The API is reachable directly at **http://localhost:5080** (try `/health`), and Postgres is exposed on host port **5433** if you want to connect with a client (`localhost:5433`, database `invoicebuilder`, user/password `invoicebuilder` — see [DECISIONS.md](DECISIONS.md) for why these are hardcoded and fine for local dev only).

The database schema is applied automatically on API startup — a fresh volume works out of the box, no manual migration step needed.

### Running without Docker

Backend:
```bash
cd backend
dotnet run --project src/InvoiceBuilder.Api
```
Requires a reachable Postgres instance; connection string comes from `appsettings.Development.json` (defaults to `localhost:5433`, matching the Dockerized Postgres if you started just that service with `docker compose up -d postgres`).

Frontend:
```bash
cd frontend
npm install
npm start   # ng serve, http://localhost:4200
```

## Running Tests

```bash
# Backend — unit + integration (integration tests need a reachable Postgres; see connection
# string in CustomWebApplicationFactory.cs)
cd backend
dotnet test

# Frontend
cd frontend
npm test
```

## Project Status

Phases 1–9 of the build roadmap are complete: project scaffolding, database/domain model, the Invoice module's API, PDF generation, the full Angular frontend (shell, CRUD screens, PDF download flow), Docker Compose integration, and test coverage for both backend and frontend. See [DECISIONS.md](DECISIONS.md) for the complete, dated log of every architectural decision made along the way — including a couple of real bugs that were found and fixed by the tests written in that phase, not just the features they were meant to cover.

Not yet built (stretch goals): real Users/Auth, Payments, and Reports modules.
