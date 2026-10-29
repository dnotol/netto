---
type: adr
title: The user sets what the company keeps, and the dividend is what is left
description: One reserve for the whole company at year end; the dividend is profit after tax plus earlier profits less that reserve, never below zero, never refused.
status: stable
decided: 2026-10-19
tags: [engine, product]
sources:
  - resource: https://app.notion.com/p/3fdb6b39bd1281bc81bfce270c576635
  - resource: https://claude.ai/artifact/C2gkexviNGUUMzDbpaqaEX
  - resource: git:b568560 Keep a reserve in the company and derive the dividend from it
  - resource: git:d03737d Pay out profits kept from earlier years with the year's dividend
generated: {by: claude-opus-5-5/architecture-views-0.7.0, at: 2026-10-29T14:00:00+01:00}
---
# Context

A dividend typed as an amount could exceed what the year allows and had to be refused. The River Map's rules of 19 October 2026 (« The engine takes what you keep, not a dividend », « One reserve for the whole company ») and « Goal & scope » ([Notion](https://app.notion.com/p/3fdb6b39bd1281bc81bfce270c576635)) changed the input.

# Decision

The `Scenario` holds the `Reserve` kept in the company at year end, for the whole company: this year's profit after tax and the profits kept from earlier years (`EarlierProfits`). Everything above it is paid out. A loss eats into the earlier profits first. A reserve above what the company holds leaves no dividend. The rule « the reserve is kept » is one method, `YearResult.ReserveKept`, which the year, the split's search and the moves all read.

# Alternatives rejected

- A dividend amount as input: it could be refused for exceeding the profit; the reserve replaces it (River Map, 19 October 2026).

# Consequences

- The dividend card is a slider over the profit after tax, so it never exceeds what is there.
- A pay that cannot keep the reserve is never the best pay ([the split's search](/2026-10-19-the-split-searches-the-best-pay.md)), and no move leaving less than the reserve is taken ([moves](/2026-10-22-moves-found-greedily-from-a-baseline.md)).
- Whether the reserve should also be expressible in months of running costs is still open (River Map, « Still open »).
