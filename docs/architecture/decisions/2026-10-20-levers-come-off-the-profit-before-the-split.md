---
type: adr
title: What the company pays besides pay comes off its profit before the split, and is never pay
description: Vouchers, allowances, rent, a car and a royalty come off what the company earns in the law's order; the split shares what they leave; what is undeductible is added back.
status: stable
decided: 2026-10-20
tags: [engine, law-reading]
sources:
  - resource: https://app.notion.com/p/3feb6b39bd12819eba87eff6b9ff6c98
  - resource: https://app.notion.com/p/3ffb6b39bd1281b79c64c75a3421b860
  - resource: https://claude.ai/artifact/C2gkexviNGUUMzDbpaqaEX
  - resource: git:bbe3b2d Run the year with the vouchers: off the profit before pay, into the pocket
generated: {by: claude-opus-5-5/architecture-views-0.7.0, at: 2026-10-29T14:00:00+01:00}
---
# Context

The first levers, vouchers then expense allowances and the home office rent, raised where their money sits in the year. « Engine architecture » ([Notion](https://app.notion.com/p/3feb6b39bd12819eba87eff6b9ff6c98)) and the River Map's rules of 20 October 2026 (« What the company pays you besides pay comes before the split », « The lever modules run one under the other ») settled it.

# Decision

What the company pays the director besides pay comes off what the company earns, one lever after the other, so the split shares what they leave (`YearSimulation.ProfitBeforePay`). What a lever brings reaches the pocket; what the company cannot deduct of it is added back to its taxable income (`Disallowed`) and, never spent or already spent, comes out of the profit after tax again. These amounts are never pay: they never count toward art. 215's minimum director pay. The bar of buckets starts from what the company earns before it pays the director anything.

# Alternatives rejected

- A lever interface at that point: not introduced, since a seam arrives with its second case (« Engine architecture », « When each piece arrives »). It came once five levers existed ([levers behind one contract](/2026-10-24-levers-behind-one-contract.md)).

# Consequences

- `Left` in `YearSimulation` is the one walk the year, the profit before pay and the pay ceiling subtract the levers by.
- What a lever hands the pay to be taxed with it (a rent's net, a car's benefit, a royalty) became one record, `TaxedWithPay`.
- The office allowance and the rent exclude each other, a condition the engine names ([the year's conditions](/2026-10-25-the-engine-names-the-years-conditions.md)).
