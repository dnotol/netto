---
type: adr
title: No part of a copyright royalty counts toward the minimum director pay of the reduced corporate rate
description: Law reading - art. 215 tests the taxable pay without a royalty's part taxed as pay (TaxablePayWithoutRequalified).
status: stable
decided: 2026-10-22
acceptance_mode: unattended
operator_review: confirmed
verified:
  - {by: "human:johnny-amadeus", at: "2026-11-17T15:16:22+01:00"}
tags: [law-reading, engine]
sources:
  - resource: https://app.notion.com/p/401b6b39bd128164b84ec739e9ee4124
  - resource: https://claude.ai/artifact/H78okHrfkQ1Fjs3FtCYGCY
  - resource: "git:d30da32 Run the year with a copyright royalty, taxed apart within its limits and as pay beyond them"
generated: {by: claude-opus-5-5/architecture-views-0.7.0, at: 2026-10-29T14:00:00+01:00}
---

# Context

The part of a royalty beyond its limits (30% of royalty plus payslip gross, and the year's cap) is taxed as director pay under art. 32 CIR 92. Art. 215 grants the reduced corporate rate only when the company pays a director at least the minimum pay, €51,000 for 2026. No text Netto has filed settles whether that requalified part counts toward the minimum. It was decided unattended while « I can weigh a copyright royalty in my plan » ([story](https://app.notion.com/p/401b6b39bd128164b84ec739e9ee4124)) was built.

# Decision

Art. 215 tests the taxable pay as it would be without the royalty's part taxed as pay: `GrossToNetResult.TaxablePayWithoutRequalified`, which `YearSimulation` hands `CorporateTaxCalculator`. The reasoning, kept in `YearSimulation`'s comment: art. 215 asks for pay to an art. 32 director, and leaving the royalty out never overstates the gain ([the reading that never overstates the gain](/2026-10-22-the-reading-that-never-overstates-the-gain.md)).

# Alternatives rejected

None recorded at the time.

# Consequences

- With €49,000 of pay beside €30,000 of royalty, €6,300 is taxed as pay but art. 215 still tests €49,000 (`YearSimulationCopyrightTests`).
- The requalified part is in `TaxablePay`, INASTI's contributions, the scale and the net, never in the company's cost of the pay, the pay statement or the key formula.
- A royalty that follows the pay lowers the company's taxable income, so the smallest pay keeping the reduced rate drops; the engine names that effect as a condition since 25.10.2026 ([the year's conditions](/2026-10-25-the-engine-names-the-years-conditions.md)).
- How art. 215 treats contributions the company pays on the requalified part is still an open point on the royalty story.
