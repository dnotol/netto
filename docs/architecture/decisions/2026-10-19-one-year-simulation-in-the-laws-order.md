---
type: adr
title: One year simulation composes the layers in the law's order, and the cards only read its steps
description: YearSimulation.Run composes static layer calculators into one ledger of tagged steps; no tax rule lives outside Netto.Core.
status: stable
decided: 2026-10-19
tags: [engine]
sources:
  - resource: https://app.notion.com/p/3feb6b39bd12819eba87eff6b9ff6c98
  - resource: git:20c478c Record the engine architecture rule in CLAUDE.md
  - resource: git:67eac0a Tag each step with its layer and taxable event
  - resource: git:6080190 Simulate the year from the company's profit to the director's pocket
generated: {by: claude-opus-5-5/architecture-views-0.7.0, at: 2026-10-29T14:00:00+01:00}
---
# Context

After the gross-to-net slice, the engine needed a shape that every later layer and card could follow. « Engine architecture » ([Notion](https://app.notion.com/p/3feb6b39bd12819eba87eff6b9ff6c98)) recorded it on 19 October 2026.

# Decision

- The engine follows the order the law computes in. Each layer is a static calculator returning figures and steps (`GrossToNet`, `CorporateTaxCalculator`, `DividendCalculator`, …); `YearSimulation.Run(Scenario)` composes them and returns one `YearResult` with its ledger.
- A `Step` carries its `RuleKey`, `Layer` (Corporate, Social, Personal), `TaxableEvent`, `Track` (PaidOut, TaxBaseOnly, InPocket), amount, `Source` and the amounts it used, so the buckets add up by construction.
- The river's modules are views of that ledger, not calculators: a card selects, orders and scales the engine's figures and never decides a tax outcome. No tax rule lives outside `Netto.Core`; Core returns data and refusals, never display text.

# Alternatives rejected

- A strategy per layer chosen by dependency injection: rejected (« Why not Strategy everywhere »): the law's order is fixed, Tax-Calc comparability needs one composition, and an interface goes only where the algorithm differs.

# Consequences

- A card growing an `if` about tax means the logic belongs in Core, checked at review.
- View models live in `Netto.Web.Client` and are tested under `WebClient/`.
- The levers later joined this composition behind one contract ([levers behind one contract](/2026-10-24-levers-behind-one-contract.md)), and the conditions between parts of the year became the engine's ([the year's conditions](/2026-10-25-the-engine-names-the-years-conditions.md)).
