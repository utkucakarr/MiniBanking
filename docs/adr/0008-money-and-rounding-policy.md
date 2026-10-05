# ADR 0008 — Money and rounding policy

- **Status:** Accepted
- **Date:** 2026-09-30

## Context
Monetary amounts are at the core of every module. Rounding errors, silently altered amounts and currency mix-ups
are among the most expensive defects in banking software:
- binary floating point (`double`) cannot represent values such as 0.1 exactly;
- .NET's `Math.Round` / `decimal.Round` default to banker's rounding (`MidpointRounding.ToEven`), which surprises most developers;
- splitting an amount (e.g. 100.00 TRY into 3 installments) naively loses or creates minor units.

## Decision
Money is modeled in `MiniBanking.SharedKernel.Monetary`:
- `Currency` — ISO 4217 code plus minor units (TRY/USD/EUR/GBP: 2, JPY: 0). Only supported currencies can exist.
- `Money` — a `decimal` amount plus a `Currency`. It is an always-valid, immutable value object.

### Rules
1. **`decimal` only.** Never `double` or `float` for amounts, rates or factors.
2. **Precision is validated, not rounded.** `Money.Create` rejects amounts with more decimal places than the
   currency's minor units (`Money.InvalidPrecision`, HTTP 400). Input such as `100.555 TRY` is an error to be surfaced, not silently fixed.
3. **No implicit rounding anywhere.**
   - Addition and subtraction of valid amounts are exact and need no rounding.
   - Operations that produce fractions (interest, fees, FX) use `Multiply(factor, MidpointRounding rounding)`.
     The rounding rule is a **required parameter with no default**.
4. **Which rule applies is a business decision per use case**, recorded when that use case is built
   (e.g. interest accrual in Phase 7, fees in Phase 4, FX in Phase 6). Reference points:
   - `MidpointRounding.AwayFromZero` (commercial, "half up"): customer-facing amounts, most fees.
   - `MidpointRounding.ToEven` (banker's rounding): high-volume calculations such as accruals, to avoid systematic bias.
5. **Splitting uses `Allocate(parts)`.** Shares differ by at most one minor unit and always sum to the original amount.
6. **No implicit currency mixing.** Combining or comparing different currencies throws `InvalidOperationException`,
   because it is a programming error. Conversion is an explicit FX operation (Phase 6).
7. **Culture-independent text.** `Money.ToString()` uses `CultureInfo.InvariantCulture` (`"1234.50 TRY"`).
   Codes are normalized with `ToUpperInvariant` (avoids the Turkish-I problem).
   Localized display belongs to the frontend.

### Persistence (see ADR 0004)
`amount numeric(19,4)` + `currency char(3)`, mapped as an EF Core complex type.

## Consequences
- An existing `Money` instance is always valid; no code needs to re-check precision.
- Every rounding decision is visible at the call site and can be reviewed.
- Clients must send amounts at the currency's precision. Excess precision gets a clear 400 error.
- Allocation guarantees ledger balances are never off by a minor unit.
