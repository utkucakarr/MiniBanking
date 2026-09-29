# ADR 0003 — API style: Minimal APIs

- **Status:** Accepted
- **Date:** 2026-09-28

## Context
Modules are organized as vertical slices (ADR 0001), and handlers are resolved directly from DI (ADR 0002).
We need to choose:
- how HTTP endpoints are written and registered;
- how URLs and versions look;
- how the API is documented.

## Options considered
| Option | Pros | Cons |
|---|---|---|
| Controllers | Familiar; common in existing codebases | One controller per resource groups many slices together, which works against vertical slices; the constructor carries every action's dependencies |
| **Minimal APIs** | One endpoint per slice, next to its handler; each endpoint takes only what it needs; default in current templates; in .NET 10 it has built-in validation, OpenAPI metadata, route groups and endpoint filters | Less familiar to people used to MVC |

For registration:
- **Explicit list per module** is readable and uses no reflection, but a slice can be forgotten.
- **Auto-discovery** through an `IEndpoint` interface and reflection prevents forgotten slices, but hides the wiring.

## Decision
1. **Minimal APIs.** Each slice owns its endpoint, e.g. `Features/FreezeAccount/FreezeAccountEndpoint.cs`,
   with a static `Map(IEndpointRouteBuilder)` method.
2. **Explicit registration.** Each module's `MapEndpoints`:
   - creates one **route group** (e.g. `/api/v1/accounts`) that carries tags and authorization;
   - calls every slice's `Map` method explicitly.
   Integration tests catch forgotten slices.
3. **URL design:**
   - resources are plural nouns: `/api/v1/accounts`, `/api/v1/transfers`;
   - state transitions are actions: `POST /api/v1/accounts/{id}/freeze`;
   - money-moving commands require an `Idempotency-Key` header (details in the Payments phase).
4. **Versioning:** a fixed `v1` URL segment for now. `Asp.Versioning.Http` is added only when a real v2 is needed.
5. **Responses:**
   - endpoints return `TypedResults`, so OpenAPI metadata is inferred from return types;
   - failures are mapped from `Result` to RFC 9457 ProblemDetails (details in the error-handling ADR).
6. **Documentation:**
   - the built-in `Microsoft.AspNetCore.OpenApi` generates the OpenAPI document;
   - **Scalar** (MIT) serves the UI in Development. Swashbuckle is not used.

## Consequences
- Endpoint, command, handler and validator for a use case live in one folder.
- Adding a slice means adding one `Map` call in the module. A missing call shows up as a 404 in integration tests.
- The API contract is documented from code. A typed frontend client can later be generated from the OpenAPI document.
- Controllers are still interview-relevant. They can be compared by rewriting one module's endpoints as an exercise.
