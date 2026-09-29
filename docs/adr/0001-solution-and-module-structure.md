# ADR 0001 — Solution and module structure

- **Status:** Accepted
- **Date:** 2026-09-28

## Context
MiniBanking is a modular monolith: one deployable host with independent business modules
(Customers, Identity, Accounts, Ledger, Payments, FX, Interest & EOD, Notifications).
We need to decide:
- how many projects each module has;
- how code is organized inside a module;
- where shared code lives;
- how tests and the solution file are laid out.

Two different levels of architecture are involved:
- **Modular monolith** (macro): how the system is split into modules and deployed.
- **Vertical slice** (micro): how code is organized inside a module, by feature instead of by technical layer.

These are complementary, not alternatives.

## Options considered
| Option | Pros | Cons |
|---|---|---|
| 4 projects per module (Domain / Application / Infrastructure / Contracts) | The compiler enforces every layer | ~40 projects, heavy ceremony, empty projects in simple modules, slower builds |
| **2 projects per module (Module + Contracts)** | The compiler enforces the boundary *between* modules; lightweight | Layering *inside* a module is enforced by architecture tests, not the compiler |
| 1 project per module | Simplest | No separate public contract, so module boundaries blur |

For the inside of a module:
- **Layered folders** scatter one use case across many folders.
- **Vertical slices** keep one use case together.

## Decision
1. **Two projects per module:**
   - `MiniBanking.<Module>` contains everything internal to the module. Its types are `internal` by default.
   - `MiniBanking.<Module>.Contracts` is the only thing other modules may reference: interfaces, DTOs, integration events.
2. **Inside a module:** a separate `Domain/` folder, use cases as vertical slices, and a separate `Infrastructure/` folder:
   ```
   MiniBanking.Accounts/
     Domain/            aggregates, value objects, domain events (business rules)
     Features/
       OpenAccount/     command, handler, validator, endpoint for one use case
       FreezeAccount/
     Infrastructure/    DbContext (own schema), EF configurations, migrations
     AccountsModule.cs  registers the module in the host
   ```
   The domain stays separate because aggregate rules (e.g. "a frozen account cannot be debited")
   belong to all features, not to one.
3. **Domain purity is enforced by architecture tests:** `Domain/` must not depend on EF Core,
   ASP.NET Core or `Infrastructure/`.
4. **Ledger is revisited in Phase 3.** Because its invariants are the most critical, its domain may be
   extracted into its own `MiniBanking.Ledger.Domain` project so the compiler enforces isolation.
   Keeping `Domain/` folders clean makes this a cheap, reversible step.
5. **Shared code:**
   - `MiniBanking.SharedKernel` holds pure, rarely changing concepts every module needs
     (`Money`, `Currency`, `Result`, `Entity`, `AggregateRoot`). It has no framework dependencies.
     Module-specific concepts such as `Iban` stay in their module.
   - `MiniBanking.BuildingBlocks` holds technical infrastructure: module registration, error handling,
     pipeline behaviors and, later, the outbox.
6. **Tests:**
   - one unit test project per module (`tests/Modules/MiniBanking.<Module>.Tests`);
   - one `MiniBanking.IntegrationTests` project using a real PostgreSQL via Testcontainers;
   - one `MiniBanking.ArchitectureTests` project.
7. **Solution file:** `MiniBanking.slnx`, the XML solution format that is the default in .NET 10.

## Resulting layout
```
MiniBanking.slnx
Directory.Build.props, Directory.Packages.props, global.json, docker-compose.yml
docs/adr/
src/
  Bootstrapper/MiniBanking.Api
  BuildingBlocks/MiniBanking.SharedKernel
  BuildingBlocks/MiniBanking.BuildingBlocks
  Modules/<Module>/MiniBanking.<Module>
  Modules/<Module>/MiniBanking.<Module>.Contracts
tests/
  Modules/MiniBanking.<Module>.Tests
  MiniBanking.IntegrationTests
  MiniBanking.ArchitectureTests
```

## Consequences
- About 18 projects instead of about 40, with the same compiler-enforced module boundaries.
- Discipline is still needed inside modules. The architecture tests are the safety net and must exist
  from the first module onward.
- All modules follow one pattern, which makes the codebase predictable while learning.
- Any module can later be extracted into a service, or split into more projects, along existing boundaries.
