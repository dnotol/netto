---
type: adr
title: The engine names the conditions one part of the year sets on another
description: YearConditions.Of(year) returns sealed YearCondition records, each saying whether it holds; the map and cards only word them.
status: stable
decided: 2026-10-25
tags: [engine]
sources:
  - resource: https://app.notion.com/p/404b6b39bd128141985adff511a771c9
  - resource: https://claude.ai/artifact/C2gkexviNGUUMzDbpaqaEX
  - resource: "git:2fda476 Name the conditions one part of the year sets on another"
generated: {by: claude-opus-5-5/architecture-views-0.7.0, at: 2026-10-29T14:00:00+01:00}
---

# Context

The river is not one-way: the pay decides the corporate rate, a benefit in kind can cancel it, a royalty moves the pay that keeps it ([a river of modules](/2026-10-19-a-river-of-modules.md)). Until 25.10.2026 each card showed such a condition in its own words. « I see on one map how my modules act on each other » ([story](https://app.notion.com/p/404b6b39bd128141985adff511a771c9)) needed them as data to draw.

# Decision

The conditions are the engine's: `Simulation/YearConditions.Of(year)` returns sealed `YearCondition` records, declared only in Core, each saying whether it holds. Five kinds: `PayKeepsReducedRate` and `BenefitsWithinShareOfPay` (art. 215's two tests, which Corporate tax now reports apart as `PayTest` and `BenefitsWithinShare`), `NoOfficeAllowanceBesideRent`, `RoyaltyLowersPayKeepingReducedRate` (from the split's own search, with the royalty and without it) and `PayOutCoversReserve`. They are computed beside the year, never inside `Run`.

# Alternatives rejected

None recorded at the time.

# Consequences

- A new kind is its record, its case in `YearConditions.Of`, its arm in `Tabs/MapConditions` (which throws on a kind no case names; `MapConditionsTests` holds every kind) and its words.
- The map draws each as a dashed condition line with a badge ([map lines read from the cards](/2026-10-25-map-lines-read-from-the-cards.md)); since 28.10.2026 the split's and the dividend's cards state theirs from the same records.
- `YearConditions.Of` runs `YearSimulation.SmallestPayKeepingReducedRate` twice, so each new run of the map costs more Core runs.
