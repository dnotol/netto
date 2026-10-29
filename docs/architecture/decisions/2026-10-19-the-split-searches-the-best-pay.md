---
type: adr
title: The split finds the best pay by running the year at every pay
description: SplitSearch draws net in pocket on a grid of pays, adds the reduced-rate jump, and refines the best pay on each side to the euro; no formula of its own.
status: stable
decided: 2026-10-19
tags: [search, engine]
sources:
  - resource: https://app.notion.com/p/3feb6b39bd12819eba87eff6b9ff6c98
  - resource: https://claude.ai/artifact/C2gkexviNGUUMzDbpaqaEX
  - resource: git:33f4330 Find the director pay that leaves the most in the pocket
  - resource: git:64f915b Draw the net in pocket at every pay on the split and offer the best pay
generated: {by: claude-opus-5-5/architecture-views-0.7.0, at: 2026-10-29T14:00:00+01:00}
---
# Context

The question Netto answers first is how to split the profit between pay and dividend. The River Map's rules of 19 October 2026 (« The split finds the best pay to the euro », « A pay that cannot keep your reserve is never the best », « The split weighs the reduced corporate rate ») and « Engine architecture » ([Notion](https://app.notion.com/p/3feb6b39bd12819eba87eff6b9ff6c98)), which puts `SplitSearch` in the engine, set how.

# Decision

`Netto.Core/Simulation/SplitSearch` runs the year at every pay with everything else held, through `YearSimulation.Level`, and does no tax arithmetic of its own. Net in pocket is piecewise linear in the pay with one jump where the reduced corporate rate is kept: a grid of round amounts draws it, the jump is added a cent either side, and on each side the best pay is refined to the euro (`BestBetween`), narrowing by thirds then trying every euro. Only pays whose profit after tax covers the reserve count. A tie goes to the lower pay. The split weighs the best pay keeping the reduced rate against the best giving it up, and says which it chose.

# Alternatives rejected

None recorded at the time.

# Consequences

- The search runs the year a hundred times and more, on the browser's one thread; the page reruns it only when an input other than the pay changes (`Plan.Show`, `SameBesidesPay`).
- The moves reuse it as their pay move ([moves](/2026-10-22-moves-found-greedily-from-a-baseline.md)).
- A royalty that follows the pay made the pays run up to `YearSimulation.PayCeiling`.
- `Simulation/SplitSearchTests` holds it.
