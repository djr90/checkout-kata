# Engineering notes

`README.md` is the kata brief plus a quickstart. **This file is the design record** — what was
built, why, the trade-offs accepted, and the full reference for running and testing the solution.

## Run & test

Everything below runs from the repo root on .NET 10. The full run/test matrix lives here; the
README keeps only a one-command quickstart.

**Run the app.** The Aspire AppHost starts the dashboard and the API together:

```bash
dotnet run --project src/CheckoutKata.AppHost      # Aspire dashboard + checkout-api
dotnet run --project src/CheckoutKata.Api          # API only, on http://localhost:5080
```

To exercise the endpoint, open [`CheckoutKata.Api.http`](src/CheckoutKata.Api/CheckoutKata.Api.http)
in VS / Rider or the VS Code REST Client and click **Send Request** — it carries both happy and
unhappy paths (a valid basket returns `{"total":95}`; bad input returns a problem document).

**Run the tests.** The whole suite, then each project on its own:

```bash
dotnet test                                                  # all four suites

dotnet test tests/CheckoutKata.Domain.UnitTests              # pure domain: Sku, rules, offers
dotnet test tests/CheckoutKata.Application.UnitTests         # Checkout + PricingService + property tests
dotnet test tests/CheckoutKata.ArchitectureTests             # layering & naming/sealing rules
dotnet test tests/CheckoutKata.Api.IntegrationTests          # HTTP contract (200/400/500, no leaks)
```

**Mutation testing** runs one source project at a time (Stryker mutates a single assembly per run):

```bash
dotnet stryker -f stryker-config.Domain.json                 # last measured ~90%
dotnet stryker -f stryker-config.Application.json            # last measured ~81%
```

Both sit above the **80% break threshold**. **Formatting:**

```bash
dotnet csharpier check .
```

**CI** (`.github/workflows/ci.yml`) gates on formatting, the Release build, and `dotnet test`
(the architecture tests are part of that gate). The two Stryker runs are **non-blocking** — they
upload an HTML report as an artifact; promote them to a required gate once the score is stable.

## Architecture (plain English)

Dependencies point **inwards**: the domain knows nothing about the outer layers, and each outer
layer depends only on the ones beneath it — never the reverse.

| Project | Responsibility | Depends on |
| --- | --- | --- |
| `CheckoutKata.Domain` | `Sku`, `PricingRule`, `IOffer` + offers | `Ardalis.GuardClauses` only |
| `CheckoutKata.Application` | `Checkout` (scan state), `PricingService` (totals) | Domain, `Ardalis.Result` |
| `CheckoutKata.ServiceDefaults` | Aspire defaults: OTel, health, resilience, error contract | — |
| `CheckoutKata.Api` | minimal API (`POST /checkout/total`) | Domain, Application, ServiceDefaults |
| `CheckoutKata.AppHost` | Aspire orchestrator | Api |

- **Two core layers, not four.** Domain holds the pure model; Application holds orchestration.
  There is no Data layer because nothing is persisted, and the API is the only presentation —
  adding empty layers would be ceremony, not clarity.
- **State vs. pricing are split.** `Checkout` owns the per-transaction scan *counts*;
  `PricingService` (injected via `IPricingService`) turns a SKU→quantity map into a *total*. The
  pricing engine is stateless and thread-safe, so it is independently testable and the same engine
  prices a whole basket for the API. `Checkout` keeps a convenience constructor that wires the
  default engine, so simple call sites stay simple.
- **The architecture tests make the boundary executable.** `NetArchTest.eNhancedEdition` (the
  maintained fork) asserts the domain depends on neither the application nor the Result pattern,
  and that offers/services are sealed and conventionally named. The compiler already forbids the
  reverse *project* reference, so the real teeth are the naming/sealing/type-leak rules. They run
  in `dotnet test`, so they gate CI.
- **The error contract lives in ServiceDefaults.** Turning failures into HTTP responses is a
  hosting concern, so RFC 9457 `ProblemDetails` is wired once in `AddServiceDefaults` /
  `MapDefaultEndpoints` and every service inherits it. Bad input (unknown/blank SKU, negative
  quantity) is a **400** with a per-SKU `errors` map. Anything that throws becomes a **generic
  500** — and **no exception detail leaks in any environment** (the explicit handler wins over the
  development exception page); the error is logged server-side and correlated by a `traceId`. The
  integration tests pin that the 500 body contains no stack trace even when forced into Development.

## Key decisions — why, and the trade-off

- **`Scan` returns a `Result`, not `void`.** The brief suggests `void Scan(string)`, but `void`
  can't tell the caller a scan failed and silently dropped the item. `Scan` returns `Success`,
  `NotFound` (no rule for that SKU) or `Invalid` (blank SKU) — callers are expected to inspect it.
- **`Result` for runtime outcomes, exceptions for programmer errors.** Predictable outcomes
  (unknown/blank SKU) are `Result`s. Construction faults (null rule set, duplicate SKU, negative
  price) throw via `Ardalis.GuardClauses` — they are precondition bugs, not user input.
- **Offers are a per-SKU strategy (`IOffer`).** New per-SKU offer types drop in without touching
  `Checkout` or `PricingService` — proven by a second offer, `BuyXGetYFreeOffer`, alongside
  `MultiBuyOffer`. Cross-SKU promotions ("one X free per ten Y") are deliberately out of scope —
  they need a basket-level pipeline (see the future section).
- **Offers never overcharge.** `MultiBuyOffer` caps each line at `quantity × unitPrice` via
  `Math.Min`, so a misconfigured "3 for 200" on a £50 item still charges the cheaper 150. A pinning
  test documents this; correctly-priced discounts are unaffected.
- **`MultiBuyOffer` requires `quantity ≥ 2`.** A "multi-buy of one" is just a unit price and almost
  always a config mistake, so it is rejected at construction rather than silently repricing.
- **Money is `checked int`.** No currency or fractional pricing yet. Totals and offer arithmetic
  are `checked`, so an absurd basket throws `OverflowException` instead of silently wrapping to a
  negative total. This is robustness insurance, not a realistic path.
- **`Sku` is a normalising value object — case-insensitive.** It trims and upper-cases on
  construction, so `" a "` matches a rule keyed on `"A"`. **Trade-off, accepted:** case-folding can
  in theory collapse two genuinely distinct raw SKUs (`"aB"` vs `"Ab"`); for this catalog that is
  the right call, and the policy lives in exactly one place if it ever needs revisiting.
- **`Checkout` is single-transaction state.** Stateful, not thread-safe, no reset — one instance
  per transaction. `GetTotalPrice()` is a repeatable, side-effect-free read.

## How review shaped the design

The process matters as much as the result, so the iteration is on the record:

- **Mutation testing drove the hardening.** Stryker's first run scored **75%** and flagged that two
  of three `checked` sites were unpinned; targeted overflow tests took it to **87.5%**. After the
  Domain/Application split it runs per project (last measured ~90% / ~81%), and a test pins the new
  `Checkout(IPricingService)` null guard.
- **The never-overcharge cap came from a property test.** Writing "total is never negative" exposed
  that the only scenario which could violate it was signed overflow — which prompted both the
  `checked` arithmetic and the `Math.Min` cap (the pinning test now asserts 150, not 200).
- **A metamorphic test was attempted and rejected.** "Adding an item never lowers the total" is
  false by design: a generous special (e.g. "2 for 0") makes the item that *completes* a group
  cheaper than the partial group. The reasoning is recorded in the property-test file.
- **Dead weight was removed, not kept for show.** `AutoFixture` (referenced, never used) and the
  Reqnroll BDD acceptance layer (no non-technical audience to read the feature files; it only
  duplicated the unit theory) were both deleted.

## Test strategy

Built test-first. The unit tests mirror the production layering — Domain tests reference the domain
only, so isolation stays honest, and each project's Stryker run executes just its own tests.

- **Unit** (xUnit + AwesomeAssertions) — pricing scenarios with hardcoded totals, scan result
  paths, case-insensitive matching, and construction guards.
- **Property** (CsCheck) — `total ≥ 0` and order-independence over arbitrary valid rule sets.
  Generators stay clear of `int.MaxValue`; the overflow edge is pinned by dedicated unit tests.
- **Mutation** (Stryker.NET) — one config per source project, both above the 80% break threshold.
  The survivors are benign (a redundant null guard, two error-message strings we don't pin).

Monotonicity is intentionally **not** a property, in either direction — a generous offer can make
more items cost less, so neither "more quantity costs more" nor "adding an item raises the total"
holds.

## Proportionality

The layering, architecture tests, Aspire host, property and mutation tests are all heavier than a
four-SKU kata strictly needs — they exist to show how a real service is set up, not to claim a kata
requires them. The restraint is visible too: **two** core projects rather than four, no Data layer,
no cross-SKU offer pipeline, and the BDD layer removed once it earned nothing. For a genuinely small
tool I would keep the `IOffer` seam, the guard clauses, and the unit tests, and add the rest only as
complexity justified it.

## When new requirements arrive

The seams are placed so the likely next requirements land in one obvious place:

- **A new per-SKU offer** (e.g. percentage-off) → another `IOffer`; nothing else changes.
- **A cross-SKU promotion** ("one X free per ten Y") → a basket-level discount pipeline sitting
  *above* `IOffer`, applied after per-SKU pricing. This is the one change the current model
  deliberately can't absorb in place.
- **Currency or fractional pricing** → a `Money` (`long`/decimal) value object replacing the raw
  `int`, which is also the natural home for rounding rules.
- **Persistence** (saved baskets, a real catalog) → a Data layer beneath Application, which is why
  the layering exists even though it is empty today.
- **Mutation testing as a gate** → promote the non-blocking Stryker runs to required once the score
  holds steady.
