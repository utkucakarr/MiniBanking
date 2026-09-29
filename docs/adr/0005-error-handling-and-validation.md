# ADR 0005 — Error handling and validation

- **Status:** Accepted
- **Date:** 2026-09-28

## Context
Banking operations fail for many *expected* reasons: insufficient funds, frozen account, KYC not verified,
limit exceeded. They also fail for *unexpected* reasons: bugs, database outages. We need:
- consistent handling of both kinds;
- stable, machine-readable error codes for clients;
- no leaking of internals.

## Decision

### Results for expected failures, exceptions for the unexpected
- **Business rule violations** are returned as `Result.Failure(error)` by domain methods and handlers.
  They are never thrown. They are normal outcomes and must be visible in method signatures.
- **Exceptions** are only for bugs (e.g. adding different currencies, null arguments) and infrastructure
  failures (e.g. database unavailable).
- `DbUpdateConcurrencyException` is translated into a `Conflict` error.

### Errors
- `Error(Code, Message, Type)` lives in `MiniBanking.SharedKernel`.
- Codes follow `<Module>.<Name>` (e.g. `Accounts.InsufficientFunds`). They are stable, and clients may rely on them,
  e.g. for localized messages. Messages are for developers and may change.
- Each module defines its errors in one place, e.g. `internal static class AccountErrors`.

`ErrorType` maps to an HTTP status:

| ErrorType | HTTP | Example |
|---|---|---|
| `Validation` | 400 | Malformed input, invalid IBAN |
| `Unauthorized` | 401 | Not authenticated |
| `Forbidden` | 403 | Accessing another customer's account |
| `NotFound` | 404 | Account doesn't exist |
| `Conflict` | 409 | Concurrency conflict, idempotency key reused with a different payload |
| `BusinessRule` | 422 | Insufficient funds, account frozen, KYC not verified |

### Input validation
- **FluentValidation** (Apache 2.0) validators for commands and queries run in the validation decorator
  (ADR 0002), before the handler. Invalid requests never reach the database.
- Validators check **shape only**: required fields, ranges, formats.
- Rules that depend on current state (balance, account status) belong to the **domain**, not validators.

### Always-valid value objects
- Value objects (`Iban`, `Money`, …) have private constructors and a static `Create(...)` returning `Result<T>`.
- An instance can only exist if it is valid, so code receiving an `Iban` never re-validates it.
- `Create` is used for untrusted external input. EF Core materializes persisted values through the private constructor.

### HTTP mapping
- All errors are returned as **RFC 9457 ProblemDetails** (`application/problem+json`), with extra members
  `code` and `traceId`. Validation errors also carry an `errors` dictionary keyed by field.
- One central `Result → ProblemDetails` mapper; endpoints call it through `result.Match(...)`.
- A global `IExceptionHandler`:
  - logs unexpected exceptions with full detail;
  - returns a generic 500 with only `traceId`;
  - never exposes stack traces or exception messages to clients.

## Consequences
- Failure paths are explicit and cheap. No exceptions are used for control flow.
- Clients get one predictable error format and stable codes.
- Validation lives in three clearly separated places: validators (shape), value objects (concept validity),
  domain (state-dependent rules).
- Changing the status code of an error type is a one-line change.
