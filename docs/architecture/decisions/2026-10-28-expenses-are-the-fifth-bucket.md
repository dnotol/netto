---
type: adr
title: The company's expenses are the bar's fifth bucket, Expenses, last
description: INASTI's company contribution and a car's costs land in Expenses; the bar starts before them and its reach leaves them out.
status: stable
decided: 2026-10-28
tags: [site, product]
sources:
  - resource: https://app.notion.com/p/407b6b39bd1281f48511c1401baf00a0
  - resource: https://app.notion.com/p/407b6b39bd1281d29c92f04374511109
  - resource: https://claude.ai/artifact/C2gkexviNGUUMzDbpaqaEX
  - resource: "git:1f5ff95 Hold INASTI's contribution and a car's costs in a fifth bucket, Expenses"
generated: {by: claude-opus-5-5/architecture-views-0.7.0, at: 2026-10-29T14:00:00+01:00}
---

# Context

Since 23.10.2026 the bar had four buckets whatever the plan held, and a car's costs and INASTI's company contribution were left out of the river's start, so on the map the car's costs ran out of the river into empty space. The operator asked for « I see what the company spends on its car and on INASTI in an Expenses bucket » ([story](https://app.notion.com/p/407b6b39bd1281f48511c1401baf00a0)).

# Decision

The bar has five buckets whatever the plan holds (`BucketsView.Keys`: pocket, taxes_on_pay, taxes_on_profit, kept_in_company, expenses), Expenses last. It holds the costs that fall in the middle of the river, INASTI's company contribution and a car's costs (`BucketsView.ExpensesOf`), and nothing else; running costs stay before the bar. The bar starts from the profit before them (`BucketsView.StartOf`) and the five buckets add up to it to the cent; the reach, the bar's and the split's, leaves the expenses out, so it reads as before. Expenses is a neutral slate (`--netto-expenses`), never a shade of the pocket, €0.00 when there are none.

# Alternatives rejected

- The four buckets of 23.10.2026: reversed by the operator ([gains are in cash](/2026-10-23-gains-are-in-cash.md) still stands).

# Consequences

- On the example, Expenses holds €409.30, or €8,409.30 with a car costing €8,000.
- On the map, the car's costs run down the right edge into Expenses and INASTI's contribution runs there from Company year; a drain to a cost of the company ends in Expenses, never in empty space.
- From 1,201 px the bar's amounts are a step smaller, so five columns hold a six-figure pocket.
