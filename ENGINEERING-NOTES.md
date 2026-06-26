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
- **Offers never overcharge.** `MultiBuyOffer` applies the special price per completed group, then
  caps the line at `quantity × unitPrice` (`Math.Min`). A misconfigured offer that is dearer than
  buying individually (e.g. "3 for 200" on a £50 item) charges the cheaper unit total (150), never
  the offer price. A pinning test documents this. Correctly-configured discounts are unaffected —
  the cap only bites when the offer is a bad deal.
- **`MultiBuyOffer` requires `quantity >= 2`.** A "multi-buy of one" is just a unit price and almost
  always a configuration mistake, so it is rejected at construction (`Guard.Against.OutOfRange`)
  rather than silently repricing every unit.
- **Money is `int` whole units, with `checked` arithmetic.** No fractional pricing or currency; a
  `Money` value object would be the first refactor if rounding/currency were introduced. Totals and
  offer arithmetic are `checked`, so an absurd basket throws `OverflowException` rather than silently
  wrapping to a negative total. This is robustness insurance, not a realistic path — see the
  overflow note below.
- **SKUs are a normalising `Sku` value object.** Null/blank is rejected; surrounding whitespace is
  trimmed and the value is upper-cased, all in one place (`Sku`), so `" a "` matches a rule keyed
  on `"A"` (see the case-insensitivity note below).
- **`Checkout` is single-transaction state.** Stateful, not thread-safe, no reset — one instance
  per transaction. `GetTotalPrice()` is a repeatable, side-effect-free read.

## Test strategy

Built test-first (TDD). Two complementary layers:

- **Unit** (xUnit + AwesomeAssertions) — pricing scenarios with hardcoded expected totals
  (130/180/260/95, B 30/45/75), scan result paths, case-insensitive matching, and construction guards.
- **Property** (CsCheck) — `total ≥ 0` and order-independence across arbitrary valid rule sets and
  scan sequences. Generators are bounded well clear of `int.MaxValue` so they never overflow; the
  `checked`-overflow edge is pinned by dedicated unit tests instead.
- **Mutation** (Stryker.NET) — Stryker mutates one project per run, so after the Domain/Application
  split there is one config each (`stryker-config.Domain.json`, `stryker-config.Application.json`).
  Current scores: **Domain 90%**, **Application 81%**, both above the **80%** break threshold. The
  surviving mutants are the same benign categories as before — a redundant null guard (LINQ's
  `ToDictionary` already throws `ArgumentNullException`) and two human-readable error-message strings
  we deliberately don't pin. Wired into CI as a **non-blocking** report (artifact upload); promote to
  a required gate once the
  score is stable.

Monotonicity is intentionally *not* a property — in **either** direction:

- *Monotonic in quantity* fails because a generous offer can make more items cost less.
- *Monotonic when adding an item* fails for the same reason: the "never overcharge" cap floors each
  line at `quantity × unitPrice`, but a generous special (e.g. "2 for 0") makes the item that
  *completes* a group cheaper than the partial group, so the total can drop. An early version of
  the metamorphic test asserted this and rightly failed; it was removed and the reasoning recorded
  in the property-test file.

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

### 3. QE review follow-ups

A senior-QE pass against the brief drove the following changes (no functional pricing bug was found;
these harden behaviour and tighten the tests):

- **Offers can no longer overcharge** — replaced the "trust the data" stance with a `Math.Min` cap
  at the unit-price total. The pinning test now asserts `150`, not `200`.
- **`checked` arithmetic** on totals and offer lines, so overflow throws instead of wrapping. This
  was prompted by noticing the never-negative property test could not actually reach the only
  scenario (signed overflow) that would violate it.
- **`MultiBuyOffer` rejects `quantity < 2`**; **SKUs are trimmed** consistently at construction and
  scan time.
- **Property generators widened** (prices/counts) to traverse realistic magnitudes, kept clear of
  the overflow edge.
- **Mutation testing (Stryker.NET) added.** The first run scored 75% and flagged that two of three
  `checked` sites were unpinned; targeted overflow tests took the score to 87.5%. After the
  Domain/Application split it runs per project (Domain 90%, Application 81%); a targeted test pins
  the new `Checkout(IPricingService)` null guard. Remaining survivors are benign (redundant null
  guard, unpinned message strings).
- **Metamorphic "adding an item never lowers the total" was attempted and rejected** — it is false
  by design under generous offers (documented above and in the property-test file).

## Decisions made explicit

These were already correct and tested; recorded here so the reasoning is on the record.

- **SKU lookup is case-insensitive (reversed).** This was previously case-sensitive, on the
  argument that SKUs are exact machine identifiers. That has been **deliberately reversed**: the
  `Sku` value object now upper-cases (`ToUpperInvariant`) on construction, so SKUs are treated as
  case-insensitive identifiers and entry is forgiving of how a code was typed (`a` matches `A`).
  Pinned by `Scan_WhenSkuDiffersOnlyByCase_MatchesRule`. **Trade-off, accepted:** case-folding can
  in theory collapse two genuinely distinct raw SKUs (e.g. `"aB"` vs `"Ab"`) into one key; for this
  catalog domain that is the right call. Normalisation is centralised in `Sku`, so the policy lives
  in exactly one place if it ever needs revisiting.

- **Totals accumulate in `checked int`.** Realistic baskets are nowhere near `int.MaxValue`, so
  overflow is not a live risk — but the arithmetic is `checked` so the impossible-in-practice case
  fails loudly (`OverflowException`) instead of wrapping to a negative total. Pinned by unit tests
  covering a single oversized line, the sum of multiple lines, and both `MultiBuyOffer` sites. A
  `Money` / `long` value object would be the home for true large-magnitude or fractional pricing.

- **Deferred: `Money` value object and cross-SKU promotions.** Neither is built. The kata is
  whole-pound, single-SKU offers, so a `Money` type (currency, rounding) and a basket-level discount
  pipeline ("one X free per ten Y") would be speculative. **Revisit when** currency, fractional
  pricing, or a genuine cross-SKU offer enters scope; the `IOffer` seam is where a per-SKU extension
  lands, and a basket-level pipeline would sit above it.

- **Proportionality.** The property tests, central package management, and ADR references are
  intentionally heavier than a four-SKU kata strictly needs — they exist to show how a real service
  would be set up. For a genuinely small internal tool I would keep the `IOffer` seam, the guard
  clauses, and the unit tests, and add the heavier layers only once complexity justified them.
  (Removing the BDD layer above is a first step in that direction.)
