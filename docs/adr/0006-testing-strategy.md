# ADR 0006 — Testing strategy and tools

- **Status:** Accepted
- **Date:** 2026-09-28

## Context
Correctness is the main quality attribute of a banking system. Business rules, database behaviour
(transactions, constraints, locking) and module boundaries all need automated verification.
Handlers use the DbContext directly (ADR 0004), which rules out mocking the database.

## Decision

### Strategy
| Test type | Scope | Example |
|---|---|---|
| **Domain unit tests** (the majority) | Aggregates and value objects; no database, no mocks | A frozen account cannot be debited; a journal entry whose debits ≠ credits is rejected |
| **Integration tests** | One slice end to end: HTTP → validation → handler → EF Core → real PostgreSQL | `POST /api/v1/accounts` returns 201 and persists; bad input returns 400 |
| **Concurrency tests** | Parallel requests against a real database | Ten parallel withdrawals never overdraw an account |
| **Architecture tests** | Structural rules from ADR 0001 | Modules reference only other modules' `.Contracts`; `Domain/` has no EF Core or ASP.NET Core dependency |
| **BuildingBlocks unit tests** | Our own infrastructure | Decorator order; `Result` → ProblemDetails mapping |

- Handlers are tested through integration tests, not unit tests with mocks.
- **The EF Core InMemory provider is not used.** It has no transactions, constraints, `xmin` or row locking,
  which are exactly the behaviours we need to verify.
- There are no end-to-end UI tests until a frontend exists.

### Tools
| Need | Choice | Reason |
|---|---|---|
| Framework | **xUnit v3** | Most widely used in modern .NET |
| Assertions | **AwesomeAssertions** (Apache 2.0) | Community fork of FluentAssertions 7. Same `.Should()` syntax found in industry codebases, without FluentAssertions 8's commercial license |
| Database | **Testcontainers.PostgreSql** | Real PostgreSQL in Docker, same engine as production |
| Database reset | **Respawn** | Fast table cleanup between tests without restarting the container |
| API host | `WebApplicationFactory<Program>` | Runs the full app in memory |
| Architecture | **NetArchTest.Rules** | Simple fluent rules over assemblies |
| Test doubles | **NSubstitute** (used sparingly) | Clean syntax; Moq avoided after the 2023 SponsorLink incident |
| Time | `FakeTimeProvider` (`Microsoft.Extensions.TimeProvider.Testing`) | Controls `TimeProvider` (ADR 0004), including timers, for limits, interest and EOD jobs |

How the choice was made: licensing and maintenance first. Where options were equal, familiarity in the job market decided.
Assertion libraries are a low-cost, reversible choice.

### Layout
```
tests/
  Modules/MiniBanking.<Module>.Tests   domain unit tests; access internals via InternalsVisibleTo
  MiniBanking.BuildingBlocks.Tests
  MiniBanking.IntegrationTests         one shared PostgreSQL container per test run
  MiniBanking.ArchitectureTests
```

### Conventions
- Arrange / Act / Assert.
- Test names describe behaviour, e.g. `Withdraw_fails_when_account_is_frozen`.
- Test-first (TDD) for domain rules, especially Money and Ledger.
- Test data via simple builders per aggregate (e.g. `new AccountBuilder().Frozen().Build()`).

### Continuous integration
A GitHub Actions workflow runs build and all tests, including Testcontainers, on every push and pull request.

## Consequences
- Tests run against real PostgreSQL behaviour, so concurrency and transaction bugs are caught.
- Integration tests need Docker locally and in CI.
- Few mocks means tests are less coupled to implementation details and survive refactoring.
