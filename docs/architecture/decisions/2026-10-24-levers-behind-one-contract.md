---
type: adr
title: The year folds over its levers in the law's order, each behind one contract
description: Lever and LeverYear, ordered by LeverOrder.For(year); what each hands the pay is one record, TaxedWithPay.
status: stable
decided: 2026-10-24
tags: [engine]
sources:
  - resource: https://app.notion.com/p/403b6b39bd1281b988c7e240f8369c20
  - resource: https://claude.ai/artifact/C2gkexviNGUUMzDbpaqaEX
  - resource: "git:2546b7e Fold the year over the levers behind one contract"
  - resource: "git:705c92e Hand the pay what is taxed with it as one record"
generated: {by: claude-opus-5-5/architecture-views-0.7.0, at: 2026-10-29T14:00:00+01:00}
---

# Context

By 23.10.2026 `YearSimulation` composed five levers (vouchers, expense allowances, home office rent, company car, copyright royalty) by hand, and `GrossToNet` took one more argument for each income taxed with the pay. Two review findings of 22.10.2026 named the cost: catch-all switch arms let a new lever act silently as the car or the split, and GrossToNet's argument list grew by one decimal per income. « The engine takes a new lever without touching the year's composition » ([story](https://app.notion.com/p/403b6b39bd1281b988c7e240f8369c20)) was built attended in ten slices.

# Decision

The year folds over its levers in the law's order, `Simulation/Levers/LeverOrder.For(year)`: vouchers, expense allowances, home office rent, company car, copyright royalty. Each is a `Lever` with one contract: `FromProfit` (what it takes off what the company earns, as given, the one walk `Run`, `ProfitBeforePay` and `PayCeiling` share), `FollowsPay` and `AtPay`, and `Compute(scenario, left)` giving its `LeverYear` or a refusal. A `LeverYear` gives `ToPocket`, `Disallowed`, `TaxedWithPay` and its steps before the company's and after the pay's. What the levers hand the pay is one record, `DirectorPay/TaxedWithPay` (`OtherIncome`, `BenefitInKind`, `Movable`), summed through `Plus`.

# Alternatives rejected

- Composing each lever by hand in `YearSimulation`, as before 24.10.2026.

# Consequences

- A new lever is its facts on `Scenario`, a `Lever` in `Simulation/Levers/`, its place in `LeverOrder.For(year)`, a `YearResult` field if its card reads one, and an argument of the test fixtures.
- `YearSimulation` reads a lever's result by its type only where `YearResult` or `PayLevel` hands a card one.
- `TaxedWithPay.Plus` throws when two levers pay movable income, since the pay takes one.
- The household is not a lever ([household taxed jointly](/2026-10-28-household-taxed-jointly.md)).
- It builds on [car and royalty behind plain contracts](/2026-10-22-car-and-royalty-behind-plain-contracts.md) and [levers come off the profit before the split](/2026-10-20-levers-come-off-the-profit-before-the-split.md).
