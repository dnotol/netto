---
type: adr
title: INASTI's yearly company contribution is the year's first step, a cost of the company
description: CompanyContributionRules by balance-sheet band; it comes off what the company earns before the levers, and is no lever.
status: stable
decided: 2026-10-25
tags: [engine, rules]
sources:
  - resource: https://app.notion.com/p/3fdb6b39bd1281bc81bfce270c576635
  - resource: https://claude.ai/artifact/C2gkexviNGUUMzDbpaqaEX
  - resource: "git:665948e Add INASTI's yearly company contribution as a rule set of income year 2026"
generated: {by: claude-opus-5-5/architecture-views-0.7.0, at: 2026-10-29T14:00:00+01:00}
---

# Context

Every company owes INASTI a flat yearly contribution whatever it pays its director (law of 30.12.1992, art. 91). Until 25.10.2026 the example company's running costs held it without saying so. It was decided unattended in the Selected batch of 24.10.2026 (« The company's yearly INASTI contribution is a step of the year »), its documents filed on the operator's go of 25.10.2026.

# Decision

The contribution is the year's first step, `company_contribution`, off what the company earns before the levers (`YearSimulation.Left`, `YearResult.CompanyContribution`, `ProfitBeforeLevers`): a cost the company deducts like its running costs (CIR 92 art. 49, the law's art. 100). It is a rule set of its own, `CompanyContributionRules`, by the band the user declares on Company year: €409.30 for 2026 while the balance-sheet total of the penultimate closed year is at most €879,163.44, €1,022.38 above it, none when the law exempts the company (a société de personnes in its first three years, art. 94 9°; a company with no activity, art. 92bis). It is no lever.

# Alternatives rejected

- Leaving it inside the running costs, as the example did before 25.10.2026.

# Consequences

- The example's €20,000 of costs read as €19,590.70 of running costs plus €409.30, so its figures stand; a plan saved before counts the contribution twice until its running costs are lowered, which the field's bubble says.
- Saved-plan format 7 holds the band. Netto applies the law in force although a bill pending in the Chamber (DOC 56 1733/001) would change the 2026 amounts.
- Since 28.10.2026 it lands in the Expenses bucket ([Expenses is the fifth bucket](/2026-10-28-expenses-are-the-fifth-bucket.md)).
