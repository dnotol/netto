---
type: adr
title: The region is a parameter of every rule lookup, and a year or region without rules gives null
description: Scenario.Region, Wallonia unless given; only Wallonia has rules, others are refused; no lookup throws.
status: stable
decided: 2026-10-24
tags: [rules, engine]
sources:
  - resource: https://app.notion.com/p/403b6b39bd1281b988c7e240f8369c20
  - resource: https://app.notion.com/p/403b6b39bd12819a8124d77df082edd7
  - resource: https://app.notion.com/p/3fdb6b39bd128156899cc15f3b51d47e
  - resource: "git:0562f90 Make the region a parameter of the rule lookups, and no rules a null"
generated: {by: claude-opus-5-5/architecture-views-0.7.0, at: 2026-10-29T14:00:00+01:00}
---

# Context

The personal tax and the municipal surcharge differ by region; Netto only had Walloon rules. « Engine architecture » (19.10.2026) named the regional tax as the one candidate for a strategy, its seam to arrive with its second case (Notion, « Personal tax for residents of Flanders and Brussels »). It was decided in the engine-lever story of 24.10.2026.

# Decision

The region is a parameter of every rule lookup that depends on it (`PersonalTaxRules.For(year, region)`, `MunicipalRates.For(year, region)`), carried by `Scenario.Region`, Wallonia unless given. Only Wallonia has rules; another region gives null and is refused as a year without rules is (`income_year_not_supported`), never guessed. A year or a region without rules gives a null rule set, a null calendar or null contribution quarters, never an exception.

# Alternatives rejected

None recorded at the time.

# Consequences

- Flanders and Brussels were dropped from the scope by the pivot the same day ([a reference tool](/2026-10-24-a-reference-tool-not-a-product.md)); the seam stays.
- Saved plans store no region, so every plan reads as Walloon.
- A new year is a new rule instance, never an edit ([one rule set per layer and income year](/2026-10-19-one-rule-set-per-layer-and-income-year.md)).
