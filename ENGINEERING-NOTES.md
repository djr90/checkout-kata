# Engineering notes

`README.md` is the **original kata brief**, preserved verbatim (problem statement, pricing
table, the suggested `void Scan` interface, and instructions). This document is the solution's
design record and change log — it is the authoritative description of *what was built and why*.

## Solution design

- **`ICheckout.Scan` returns a `Result`, not `void`.** The brief suggests `void Scan(string)`,
  but a `void` signature can't tell the caller a scan failed. Following
  [ADR-001 (Result pattern)](https://xero.atlassian.net/wiki/spaces/PHOMO/pages/271135835180),
  `Scan` returns `Success`, `NotFound` (no rule for that SKU) or `Invalid` (null/empty/whitespace).
  **Callers must inspect the result** — an ignored failure silently drops the item from the basket.
- **Exceptions vs Result.** Predictable runtime outcomes (unknown/blank SKU) are `Result`s.
  Construction-time programmer errors (null rule set, duplicate SKU, negative price, blank SKU)
  throw — they're precondition faults, guarded with `Ardalis.GuardClauses`.
- **Offers are a per-SKU strategy (`IOffer`).** New per-SKU offer types (e.g. %-off) can be added
  without touching `Checkout`. Cross-SKU promotions ("one X free per ten Y") deliberately aren't
  modelled — they need a basket-level discount pipeline, which is out of scope here.
- **Offers are applied as authored ("trust the data").** `MultiBuyOffer` charges the special price
  whenever the threshold is met; it does not check the offer is cheaper than buying individually.
  A pinning test documents this.
- **Money is `int` whole units.** No fractional pricing or currency; a `Money` value object would
  be the first refactor if rounding/currency were introduced.
- **`Checkout` is single-transaction state.** Stateful, not thread-safe, no reset — one instance
  per transaction. `GetTotalPrice()` is a repeatable, side-effect-free read.

## Test strategy

Built test-first (TDD). Two complementary layers:

- **Unit** (xUnit + AwesomeAssertions) — pricing scenarios with hardcoded expected totals
  (130/180/260/95, B 30/45/75), scan result paths, case-sensitivity, and construction guards.
- **Property** (CsCheck) — `total ≥ 0` and order-independence across arbitrary valid rule sets and
  scan sequences. Monotonicity is intentionally *not* a property: a generous offer can make more
  items cost less, which is the deliberate "trust the data" stance.

## Review-driven changes

### 1. Removed unused test dependencies (AutoFixture)

`AutoFixture` and `AutoFixture.Xunit2` were referenced by the unit-test project and pinned in
`Directory.Packages.props`, but **nothing used them** — zero occurrences across the test code.
Removed from `Directory.Packages.props` and
`tests/CheckoutKata.UnitTests/CheckoutKata.UnitTests.csproj`.

Rationale: unused packages are needless dependency surface and contradict the project's
dependency-light stance. The inputs here (single-letter SKUs, small integers) read more clearly
as hand-written `[InlineData]`, and the generative/randomised angle is already covered by CsCheck.

### 2. Removed the Reqnroll (BDD) acceptance layer

The `tests/CheckoutKata.AcceptanceTests` project (Reqnroll feature + step bindings) has been
deleted, `Reqnroll.xUnit` removed from `Directory.Packages.props`, and the project removed from
`CheckoutKata.slnx`.

Rationale: BDD/Gherkin earns its keep when a **non-technical stakeholder** reads or writes the
feature files. On a solo kata there is no such audience, so the layer was read and written only by
the same engineer who wrote the unit tests — and it **duplicated** cases already covered by the
unit theory (`AAA=130`, `AAAA=180`, `AAAAAA=260`, `AAABBCD=210`, `BAB=95`, and the unknown-SKU
result path). It was the least load-bearing layer in the repo, so it was cut.

## Decisions made explicit

These were already correct and tested; recorded here so the reasoning is on the record.

- **SKU lookup is case-sensitive.** SKUs are exact machine identifiers (barcodes / product codes),
  not user-typed free text, so `a` and `A` are legitimately different keys. Case-folding would risk
  silently merging distinct products. Pinned by the `Scan_WhenSkuWrongCase_ReturnsNotFound` test.
  If SKUs were ever user-entered, switching the backing dictionary to
  `StringComparer.OrdinalIgnoreCase` would be the one-line change.

- **Totals accumulate in `int`.** Realistic baskets are nowhere near `int.MaxValue`, so overflow is
  deliberately not guarded. A `Money` / `long` value object would be the home for that if fractional
  pricing, currency, or absurd quantities ever entered scope.

- **Proportionality.** The property tests, central package management, and ADR references are
  intentionally heavier than a four-SKU kata strictly needs — they exist to show how a real service
  would be set up. For a genuinely small internal tool I would keep the `IOffer` seam, the guard
  clauses, and the unit tests, and add the heavier layers only once complexity justified them.
  (Removing the BDD layer above is a first step in that direction.)
