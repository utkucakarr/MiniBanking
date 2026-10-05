# ADR 0002 — CQRS with our own handler abstractions (no MediatR)

- **Status:** Accepted
- **Date:** 2026-09-28

## Context
Every vertical slice (ADR 0001) is either a use case that changes state or one that reads state.
We want:
- commands and queries to be explicitly separate;
- cross-cutting concerns (logging, validation, transactions) applied once, not in every handler.

The common .NET answer is MediatR. Since mid-2025, MediatR 13+ is commercially licensed
(a free community license exists but requires a license key). 12.x remains open source but no longer receives updates.

## What CQRS means here
- **Command:** changes state through an aggregate, enforces business rules, and returns a minimal result
  (an id or success/failure). Example: `TransferMoney`.
- **Query:** has no side effects and reads directly into a DTO without loading aggregates (e.g. `AsNoTracking` or SQL).
  Example: `GetAccountStatement`.
- Same PostgreSQL database, same schema. CQRS here is **not** separate read/write databases and **not** event sourcing.

## Options considered
| Option | Pros | Cons |
|---|---|---|
| MediatR 12.x | Widely known | Frozen, no updates |
| MediatR 13+ (community license) | Maintained | License key and commercial dependency |
| **Own thin abstractions** | No dependency; commands and queries separated at the type level; high learning value | We write and test about 100–150 lines |
| No mediator | Simplest | Cross-cutting concerns repeated or handled per endpoint |

## Decision
We write our own minimal abstractions in `MiniBanking.BuildingBlocks`:
- `ICommand<TResponse>` / `ICommandHandler<TCommand, TResponse>`, and `ICommand` for commands without a return value;
- `IQuery<TResponse>` / `IQueryHandler<TQuery, TResponse>`;
- handlers return `Result<T>` (from `MiniBanking.SharedKernel`).

Wiring:
- Endpoints resolve the handler directly from DI. There is no central `Send()` dispatcher.
- Cross-cutting concerns are **decorators** around handlers. Planned order, from the endpoint inward:
  `Logging → Validation → Transaction (commands only) → Handler`.
- Handlers are registered by assembly scanning, one registration call per module.
- Commands and queries are internal to their module. Other modules never send them; they use the
  module's `.Contracts` interfaces instead (ADR 0001).

## Resolved points
- **Validation:** FluentValidation (ADR 0005). The validation decorator runs all `IValidator<TRequest>`s before
  the handler. On failure it returns a `ValidationError`, which carries the failures per field, without calling the handler.
  `ValidationError` derives from `Error`, so `Error` is not sealed.
- **Registration:** **Scrutor** (MIT).
  - Handlers are found by assembly scanning (`AddCqrs(assemblies)`), including `internal` ones, with a scoped lifetime.
  - Decorators are applied with `TryDecorate`. The last decorator registered is the outermost, so validation
    is registered first and logging last.
  - .NET's built-in container cannot decorate open generics on its own.
- **Transaction decorator:** added in Phase 1 together with the first module `DbContext` (ADR 0004).

## Consequences
- No third-party mediator and no licensing risk.
- Commands and queries can get different pipelines. For example, only commands open a transaction.
- The decorator order is explicit and visible in one place.
- We own this code, so it needs its own unit tests.
- Learning outcomes: generics and constraints, DI lifetimes and open generics, the Decorator pattern, CQRS.
