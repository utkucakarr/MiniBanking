# ADR 0007 — Authentication and authorization

- **Status:** Accepted
- **Date:** 2026-09-28

## Context
The system has customers, tellers and administrators. Customers must only ever access their own data.
The Identity module has to:
- authenticate users;
- issue credentials the other modules can verify without depending on Identity internals.

## Options considered
| Option | Pros | Cons |
|---|---|---|
| **ASP.NET Core Identity + own JWT issuance** | Full understanding of password hashing, JWT signing, claims, refresh-token rotation and policies | We own security-sensitive code |
| `MapIdentityApi` (.NET 8+) | Fastest setup | Proprietary (non-JWT) tokens; little control or learning |
| Keycloak (external OIDC provider) | Closest to real bank setups; teaches OAuth2/OIDC | Most auth logic lives in the provider's configuration, not in .NET code |
| Duende IdentityServer / OpenIddict | Full OAuth2 server | Duende is commercial; OpenIddict is overly complex here |

## Decision

### Authentication
- The **Identity module** uses ASP.NET Core Identity's core services:
  - `UserManager` as the user store;
  - `PasswordHasher` (PBKDF2) for passwords;
  - built-in lockout.
- Its tables live in the `identity` schema.
- **Access tokens:** JWT, signed, lifetime **15 minutes**. Claims: `sub` (user id), `customer_id` (if any), roles.
- **Refresh tokens:**
  - random, opaque values stored **hashed** in the database, lifetime about 7 days;
  - **rotated on every use**;
  - reuse of a rotated token revokes the whole token family (theft detection).
- The signing key is never committed. It comes from user secrets in Development and a secret store elsewhere.
- The login endpoint is **rate limited** against brute force.
- MFA (TOTP) is an optional exercise.

### Users vs customers
- A `User` is a login identity, and a `Customer` is a banking party (Customers module).
- A user may be linked to one `CustomerId`. Staff users (Teller, Admin) have none.

### Authorization
1. **Role-based:** roles `Customer`, `Teller`, `Admin`. For example, only `Admin` approves KYC.
2. **Policy-based:** rules beyond roles, e.g. "only KYC-verified customers may transfer".
3. **Resource-based:** customers may only access resources they own. Every query and command
   that takes a resource id checks ownership, which prevents **IDOR** (Insecure Direct Object Reference).
   Unauthorized access to another customer's resource returns `NotFound`, not `Forbidden`, so it doesn't reveal that the resource exists.

### Module independence
- All modules authenticate requests by **validating the JWT** (JwtBearer). No module calls Identity to check a token.
- `MiniBanking.BuildingBlocks` exposes `ICurrentUser` (`UserId`, `CustomerId`, `Roles`), read from claims.
- Replacing token issuance with an external provider (e.g. Keycloak) therefore changes only the Identity module.
  This is a Phase 8 exercise.

## Consequences
- Deep hands-on coverage of .NET authentication and authorization, a frequent interview topic.
- Security-sensitive code must be written carefully and covered by integration tests: token expiry, refresh rotation and reuse, lockout, cross-customer access attempts.
- Other modules stay decoupled from how tokens are issued.
