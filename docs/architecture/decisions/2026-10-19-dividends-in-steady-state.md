---
type: adr
title: Dividends are modelled in steady state, each year paying out its own profit
description: The year's dividend comes out of its own profit after tax, at the rate of its payment the following year; the whole distribution takes the declared rate.
status: stable
decided: 2026-10-19
tags: [engine, law-reading]
sources:
  - resource: https://app.notion.com/p/3fdb6b39bd1281bc81bfce270c576635
  - resource: https://claude.ai/artifact/C2gkexviNGUUMzDbpaqaEX
  - resource: git:eeeeb26 Withhold tax on the dividend and refund it on the exempt 833
generated: {by: claude-opus-5-5/architecture-views-0.7.0, at: 2026-10-29T14:00:00+01:00}
---
# Context

A dividend is decided after the financial year closes and paid the next year, so a one-year planner has to say which profit a dividend pays out. « Goal & scope » ([Notion](https://app.notion.com/p/3fdb6b39bd1281bc81bfce270c576635)) and the River Map's rule of 19 October 2026 (« Dividends in steady state ») settled it.

# Decision

Netto models a steady state: each year pays out its own profit, at the rate of its payment the following year. `YearSimulation` takes the dividend out of the profit after tax and the profits kept from earlier years, less the reserve the user keeps, never below zero. Since the law withholds on the distribution whatever year its profits come from (CIR 92 art. 269 §2), the whole dividend takes the declared rate (`WithholdingCategory`) and the exempt amount once (`DividendCalculator`).

# Alternatives rejected

None recorded at the time.

# Consequences

- The dividend's withholding paid in the following year is not on the payment calendar (River Map, « not drawn and not decided »).
- A two-year horizon is in scope since the pivot of 24 October 2026 ([a reference tool](/2026-10-24-a-reference-tool-not-a-product.md)) and would revisit this.
- The reserve is the input that sets the dividend ([one reserve](/2026-10-19-one-reserve-for-the-company.md)).
