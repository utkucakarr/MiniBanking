# ADR 0004 — Data access with EF Core and PostgreSQL

- **Status:** Accepted
- **Date:** 2026-09-28

## Context
Each module owns its data (ADR 0001). Commands change aggregates inside a transaction,
and queries read directly into DTOs (ADR 0002). Banking data has strict requirements:
- exact decimal amounts;
- safe concurrent updates;
- no mixing of identifiers;
- controlled schema changes.

## Decision

### ORM
- **EF Core 10 + Npgsql** for both commands and queries.
- Queries use `AsNoTracking()` and project straight into DTOs (`Select`).
- Dapper may be added for a specific heavy read (e.g. statements) if measured to be necessary.

### One database, one schema per module
- One PostgreSQL database `minibanking`.
- Each module has its own **`internal` DbContext** with `HasDefaultSchema("<module>")`
  and its own migrations history table in that schema.
- No DbContext maps another module's tables.
- Optional hardening later: one PostgreSQL role per module with rights on its own schema only.

### No repository abstraction by default
- Handlers use their module's DbContext directly. It already implements Unit of Work and Repository, and it is `internal` to the module.
- A dedicated repository per aggregate is introduced only if loading an aggregate becomes complex.
- Generic repositories (`IRepository<T>`) are not used.

### Transactions
- One command runs in **one transaction on its own module's DbContext**. The transaction decorator
  (ADR 0002) begins it, runs the handler, calls `SaveChanges` and commits.
- Transactions are not shared across module DbContexts.
- Cross-module workflows (e.g. a transfer touching Payments, Ledger and Accounts) are designed in a
  dedicated ADR in Phase 4, based on idempotent contracts and the outbox.

### Concurrency
- **Optimistic concurrency by default**, using PostgreSQL's `xmin` system column as the row version.
  No extra column is needed. Conflicts surface as a `Conflict` result.
- **Pessimistic locking** (`SELECT ... FOR UPDATE`) only where it is needed, e.g. balance-critical
  Ledger postings. It is introduced in Phases 3–4 and proven with concurrent integration tests.

### Modeling conventions
| Concern | Convention |
|---|---|
| Money | EF Core complex type → `amount numeric(19,4)` + `currency char(3)` |
| Identifiers | Strongly typed ids (`AccountId`, `CustomerId`, …) as `readonly record struct` wrapping a **UUID v7** (`Guid.CreateVersion7()`), mapped with value converters |
| Naming | `snake_case` tables and columns via `EFCore.NamingConventions` (MIT) |
| Time | `timestamptz`, UTC `DateTimeOffset` from .NET's built-in `TimeProvider` (`FakeTimeProvider` in tests) |
| Financial records | Insert-only tables (no updates or deletes); corrections are reversal rows |

### Migrations
- Each module has its own migrations in `Infrastructure/Migrations/`.
- **Development:** applied automatically at startup.
- **Production:** never at startup. Migrations are applied as an explicit deployment step
  (EF migration bundle or generated SQL script) so schema changes can be reviewed.

## Consequences
- Module data is isolated by schema while operations stay simple: one database, one backup.
- Handlers stay short and use full LINQ; there is no repository boilerplate.
- Strongly typed ids add some converter code but make id mix-ups a compile error.
- UUID v7 keeps inserts index-friendly and doesn't leak row counts.
- Cross-module consistency is deliberately deferred to a focused Phase 4 decision.
