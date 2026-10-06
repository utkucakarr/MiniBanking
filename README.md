# MiniBanking

A mini **core-banking system** built with **.NET 10** as a **modular monolith**.

The goal is a realistic, well-tested backend that covers the core concepts of a bank:
- customers and KYC;
- accounts and IBANs;
- a **double-entry ledger**;
- **idempotent, concurrency-safe payments**;
- multi-currency FX;
- interest accrual and end-of-day processing.

Every significant technical decision is documented as an [Architecture Decision Record](docs/adr).

> 🚧 **Status:** work in progress. Phase 0 (foundations) is complete; Phase 1 (customers & identity) is in progress. See the [roadmap](#roadmap).

---

## Architecture

One deployable ASP.NET Core host loads independent business modules. Each module:
- owns its data, in its own PostgreSQL schema;
- exposes only a public `Contracts` project to other modules;
- organizes its code as vertical slices (`Features/`) around a separate `Domain/` model.

```
                       ┌──────────────────────────────┐
   HTTP (Minimal API)  │       MiniBanking.Api        │  host: module loading, middleware, OpenAPI
                       └──────────────┬───────────────┘
        ┌────────────┬────────────┬───┴────────┬────────────┬────────────┐
        ▼            ▼            ▼            ▼            ▼            ▼
   Customers     Accounts      Ledger      Payments        FX        Notifications ...
   (schema)      (schema)     (schema)     (schema)     (schema)       (schema)
        └────────────┴─────── contracts / integration events ───────────┘
                                      │
              ┌───────────────────────┴───────────────────────┐
              │ BuildingBlocks: CQRS, decorators, errors, ... │
              │ SharedKernel:   Result, Entity, Money, ...    │
              └───────────────────────────────────────────────┘
```

Inside a module, every request flows through the same pipeline:

```
Endpoint → Logging → Validation → Transaction (commands only) → Handler → Domain (aggregate) → Result
```

### Key design decisions

| Topic | Decision | ADR |
|---|---|---|
| Structure | Modular monolith; 2 projects per module (`Module` + `Module.Contracts`); vertical slices | [0001](docs/adr/0001-solution-and-module-structure.md) |
| CQRS | Own command/query handler abstractions with decorators (no MediatR) | [0002](docs/adr/0002-cqrs-with-own-dispatching.md) |
| API | Minimal APIs, one endpoint per slice, `/api/v1`, OpenAPI + Scalar | [0003](docs/adr/0003-api-style-minimal-apis.md) |
| Data access | EF Core + PostgreSQL, schema per module, optimistic concurrency, strongly typed UUID v7 ids | [0004](docs/adr/0004-data-access.md) |
| Errors | `Result` pattern for business failures, RFC 9457 ProblemDetails, FluentValidation | [0005](docs/adr/0005-error-handling-and-validation.md) |
| Testing | Domain unit tests + integration tests on real PostgreSQL (Testcontainers) | [0006](docs/adr/0006-testing-strategy.md) |
| Auth | ASP.NET Core Identity + JWT, rotating refresh tokens, resource-based authorization | [0007](docs/adr/0007-authentication-and-authorization.md) |
| Money | `decimal`-only `Money` value object, no implicit rounding, safe allocation | [0008](docs/adr/0008-money-and-rounding-policy.md) |

### Banking rules enforced in code

- **Money is never `double`.** It is a `Money(decimal, Currency)` value object. Currencies never mix implicitly, and
  amounts can't have more decimals than the currency allows (e.g. `100.555 TRY` is rejected).
- **No hidden rounding.** Every calculation names its rounding rule (`MidpointRounding.ToEven` / `AwayFromZero`).
- **Splitting never loses a cent.** `100.00 / 3 → 33.34 + 33.33 + 33.33`.
- **Financial records are append-only.** Corrections are reversal entries, never updates or deletes.
- **Every balance change goes through the double-entry ledger.** Debits always equal credits.

---

## Tech stack

| Area | Technology |
|---|---|
| Runtime | .NET 10, C# 14 |
| Web | ASP.NET Core Minimal APIs, OpenAPI, Scalar |
| Data | PostgreSQL, EF Core 10 (Npgsql) |
| Application | Own CQRS handlers, FluentValidation, Scrutor (decorators) |
| Testing | xUnit v3 (Microsoft Testing Platform), AwesomeAssertions, Testcontainers, Respawn, NetArchTest |
| Tooling | Central Package Management, `.editorconfig`, Docker Compose, GitHub Actions |

---

## Project structure

```
MiniBanking.slnx
├── src/
│   ├── Bootstrapper/MiniBanking.Api          # host: Program.cs, OpenAPI + Scalar, health check
│   ├── BuildingBlocks/
│   │   ├── MiniBanking.SharedKernel          # Result, Error, Entity, AggregateRoot, Money, Currency
│   │   └── MiniBanking.BuildingBlocks        # CQRS, decorators, transactions, persistence, ProblemDetails, modules
│   └── Modules/
│       └── Customers/
│           ├── MiniBanking.Customers           # Domain/, Features/, Infrastructure/ (internal)
│           └── MiniBanking.Customers.Contracts # the only part other modules may reference
├── tests/
│   ├── Modules/MiniBanking.Customers.Tests   # domain unit tests (value objects, Customer aggregate)
│   ├── MiniBanking.SharedKernel.Tests
│   ├── MiniBanking.BuildingBlocks.Tests
│   ├── MiniBanking.IntegrationTests          # HTTP → handler → real PostgreSQL (Testcontainers)
│   └── MiniBanking.ArchitectureTests         # module boundaries, domain purity
├── docs/adr/                                 # architecture decision records
└── docker-compose.yml                        # local PostgreSQL
```

---

## Getting started

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) and Docker
(local PostgreSQL; the integration tests start their own throwaway PostgreSQL with Testcontainers).

```bash
git clone https://github.com/utkucakarr/MiniBanking.git
cd MiniBanking

dotnet build
dotnet test                 # needs Docker running

docker compose up -d --wait # PostgreSQL on localhost:5433
dotnet run --project src/Bootstrapper/MiniBanking.Api   # applies migrations on startup (Development)
```

To add a migration (`dotnet tool restore` installs `dotnet-ef` once):

```bash
dotnet ef migrations add <Name> --project src/Modules/Customers/MiniBanking.Customers \
  --startup-project src/Bootstrapper/MiniBanking.Api --output-dir Infrastructure/Migrations
```

Then open **http://localhost:5080/scalar** for the interactive API documentation, or
**http://localhost:5080/health** for the health check.
`src/Bootstrapper/MiniBanking.Api/MiniBanking.Api.http` has ready-made requests for Visual Studio / VS Code.

### API

| Method | Route | Description |
|---|---|---|
| `POST` | `/api/v1/customers` | Register a retail customer (TCKN checksum, 18+ rule, KYC starts as `Pending`) → `201` + `Location` |
| `GET` | `/api/v1/customers/{id}` | Read a customer → `200` / `404` |
| `GET` | `/api/v1/reference/currencies` | Supported currencies |
| `GET` | `/health` | Health check |

Errors are RFC 9457 ProblemDetails with a stable `code`, e.g. `Customers.NationalIdAlreadyRegistered` (409)
or `Customers.Underage` (422).

---

## Roadmap

- [x] **Phase 0 — Foundations**
  - [x] Solution skeleton, central package management, code style
  - [x] `Result` / `Error`, `Entity` / `AggregateRoot`, domain events
  - [x] `Money` / `Currency` value objects
  - [x] CQRS abstractions with logging and validation decorators
  - [x] ProblemDetails error mapping, global exception handler, module system
  - [x] API host (OpenAPI + Scalar, health check, console logging, integration smoke tests)
- [ ] **Phase 1 — Customers & Identity:** customer onboarding, KYC, JWT authentication
  - [x] Persistence foundation: PostgreSQL via Docker Compose, module `DbContext` with its own schema,
    transaction decorator, Testcontainers + Respawn integration tests, architecture tests
  - [x] Register a customer (first end-to-end slice): `Customer` aggregate, TCKN validation, `POST`/`GET` endpoints
  - [ ] CI (GitHub Actions)
  - [ ] KYC verification, Identity (JWT, refresh tokens)
- [ ] **Phase 2 — Accounts:** IBAN, account lifecycle, holds, available vs ledger balance
- [ ] **Phase 3 — Ledger:** double-entry bookkeeping, chart of accounts
- [ ] **Phase 4 — Payments:** deposits, withdrawals, transfers, idempotency, concurrency
- [ ] **Phase 5 — Outbox & integration events, notifications**
- [ ] **Phase 6 — FX:** exchange rates, cross-currency transfers
- [ ] **Phase 7 — Interest, end-of-day processing, statements**
- [ ] **Phase 8 — Hardening:** audit, rate limiting, performance
