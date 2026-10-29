---
type: adr
title: The pay slip withholds by the administration's key formula unless the user types what was withheld
description: Twelve equal months, each withheld by the key formula with the rules in force when paid; the year's withholding is their sum unless given.
status: stable
decided: 2026-10-20
tags: [engine, law-reading]
sources:
  - resource: https://app.notion.com/p/3fdb6b39bd1281bc81bfce270c576635
  - resource: https://claude.ai/artifact/C2gkexviNGUUMzDbpaqaEX
  - resource: git:790c6d8 Add the 2026 withholding rules for a director's monthly pay
generated: {by: claude-opus-5-5/architecture-views-0.7.0, at: 2026-10-29T14:00:00+01:00}
---
# Context

The surcharge on the director's income tax depends on how much was withheld during the year, which most users do not know offhand. « Goal & scope » ([Notion](https://app.notion.com/p/3fdb6b39bd1281bc81bfce270c576635)) and the River Map's rule of 20 October 2026 (« Your pay slip withholds by the key formula ») chose to derive it.

# Decision

The pay is paid in twelve equal months. Each month is withheld on as the administration's key formula does for a director under the self-employed status (`WithholdingCalculator`, with `WithholdingRules` per income year): the gross less a monthly reduction in place of social deductions, times 12, less 3% of flat-rate costs, the formula's own scale with the 7% municipal surcharge built in, less the tax on the tax-free amount, divided by 12, never below zero, each step rounded to the cent. The year's withholding is the sum of the twelve months, unless the user types the amount actually withheld, which replaces it. The pay slip is Director pay's detail view.

# Alternatives rejected

None recorded at the time.

# Consequences

- The withholding (Tax-Calc input 1407) is checked against the key formula and the SPF simulator's formula chain, as CLAUDE.md says.
- A decree changing the amounts within the year became rule data ([a decree within the year](/2026-10-25-a-decree-within-the-year-is-rule-data.md)).
- The household brought the quotient method and the reductions for dependants ([household](/2026-10-28-household-taxed-jointly.md)).
