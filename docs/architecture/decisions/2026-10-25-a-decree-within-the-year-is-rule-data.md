---
type: adr
title: What a decree changes within the year is rule data, and each month withholds by the rules in force on its first day
description: WithholdingRules.Later holds the rules from a date; InMonth picks a month's; November 2026 is the first case.
status: stable
decided: 2026-10-25
acceptance_mode: unattended
operator_review: confirmed
verified:
  - {by: "human:johnny-amadeus", at: "2026-11-17T15:16:22+01:00"}
tags: [rules, law-reading]
sources:
  - resource: https://claude.ai/artifact/C2gkexviNGUUMzDbpaqaEX
  - resource: "git:1d737ca Withhold November and December 2026 by the annex III amounts in force from 1 November"
generated: {by: claude-opus-5-5/architecture-views-0.7.0, at: 2026-10-29T14:00:00+01:00}
---

# Context

The royal decree of 27.09.2026 (MB 02.10.2026) changes, from 1 November 2026, the tax on the tax-free amount in the monthly withholding on a single director's pay (annex III n° 32: 2,987.98 to 3,089.63). The withholding rule set had one set of amounts per year. It was decided unattended in the Selected batch of 24.10.2026 (« Withholding from 1 November 2026 follows the adapted key formula »).

# Decision

What a decree changes within the year is rule data in the same rule set: `WithholdingRules.IncomeYear2026` holds January's rules and, in `Later`, those from 1 November. `InMonth(month)` gives the rules in force on the month's first day, and `Periods()` lists them (a method, or the rule-set reflection loops). The year's derived withholding is the sum of its twelve months, each by its own rules; the pay statement shows the formula's lines once per period.

# Alternatives rejected

None recorded at the time.

# Consequences

- No key formula is published for 1 November, so the method stays January's; the November values are derived from the official amounts and await the SPF's November simulator.
- On the example the year's withholding became €10,674.34 (from €10,691.28) and its balance €1,453.00; the pocket did not move.
- The same mechanism carries the quotient method's and the dependants' amounts from November (28.10.2026).
- It follows [withholding by the key formula](/2026-10-20-withholding-by-the-key-formula.md) and keeps [one rule set per layer and income year](/2026-10-19-one-rule-set-per-layer-and-income-year.md).
