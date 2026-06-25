# checkout-kata

A supermarket checkout that prices items by SKU, supporting per-unit prices and multi-buy
special offers (buy _n_ for _y_). Pricing rules are supplied per transaction, items are
scanned in any order, and offers apply as many times as they qualify.

## The kata

Products are identified by single-letter SKUs and priced individually; some are multipriced
(e.g. A is 50 each, or 3 for 130). Scanning is order-independent — a B, an A and another B
recognise the two-B offer for a total of 95 — and offers can apply repeatedly (six As = 260).
Because pricing changes frequently, rules are passed in when a checkout starts.

| SKU | Unit Price | Special Price |
| --- | ---------- | ------------- |
| A   | 50         | 3 for 130     |
| B   | 30         | 2 for 45      |
| C   | 20         |               |
| D   | 15         |               |

The suggested interface:

```cs
interface ICheckout
{
    void Scan(string item);
    int GetTotalPrice();
}
```

## Build, test, format

Requires the **.NET 10 SDK**.

```bash
dotnet tool restore      # restores CSharpier (pinned in .config/dotnet-tools.json)
dotnet restore
dotnet build -warnaserror
dotnet test              # unit + property + acceptance tests
dotnet csharpier check . # formatting gate (CI fails on drift)
```

CI (`.github/workflows/ci.yml`) runs the same restore → format-check → build → test (with
coverage) sequence on every push and pull request.

## Project layout

```
src/CheckoutKata/                 # the library (no test or web dependencies)
  ICheckout.cs                    # the contract
  Checkout.cs                     # aggregates scans, totals via pricing rules
  PricingRule.cs                  # immutable per-SKU pricing (unit price + optional offer)
  Offers/IOffer.cs                # per-SKU offer seam
  Offers/MultiBuyOffer.cs         # "n for y" offer
tests/CheckoutKata.UnitTests/     # xUnit + AwesomeAssertions + AutoFixture + CsCheck
tests/CheckoutKata.AcceptanceTests/  # Reqnroll (BDD) feature + steps
```

Build configuration is centralised at the root: `Directory.Build.props` (shared TFM, nullable,
analyzers-as-errors), `Directory.Packages.props` (central package versions), and `NuGet.config`
(public nuget.org only). Read top-down: contract → implementation → data → offer strategy.

## Design notes

- **`ICheckout.Scan` returns a `Result`.** The suggested `void` signature can't tell the caller
  that a scan failed. Following [ADR-001 (Result pattern)](https://xero.atlassian.net/wiki/spaces/PHOMO/pages/271135835180),
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
- **`Checkout` is single-transaction state.** Stateful, not thread-safe, no reset — one instance per
  transaction. `GetTotalPrice()` is a repeatable, side-effect-free read.

## Tests

Built test-first (TDD). Three layers, kept complementary rather than overlapping:

- **Unit** — pricing scenarios with hardcoded expected totals (130/180/260/95, B 30/45/75), scan
  result paths, case-sensitivity, and construction guards.
- **Property** (CsCheck) — `total ≥ 0` and order-independence across arbitrary valid rule sets and
  scan sequences.
- **Acceptance** (Reqnroll) — the README scenarios as Gherkin, including a caller that inspects the
  `Result` of an unknown-SKU scan.
