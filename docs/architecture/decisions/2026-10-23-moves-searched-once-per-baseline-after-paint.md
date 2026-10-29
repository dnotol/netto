---
type: adr
title: The moves are searched once per baseline and date, after the page has painted, never on an edit
description: MovesBaseline is Today, else the plan as opened or saved; MovesSource searches once per baseline and date, after plan-paint.js.
status: stable
decided: 2026-10-23
tags: [search, site]
sources:
  - resource: https://claude.ai/artifact/C2gkexviNGUUMzDbpaqaEX
  - resource: https://app.notion.com/p/402b6b39bd12815c9a13db118e4ab3a9
  - resource: https://app.notion.com/p/402b6b39bd1281bda930db0a7e146a17
  - resource: "git:f36e7d3 Measure the moves from a given plan, Today or the plan in the river"
  - resource: "git:d33b99c Search the moves once the browser has painted the plan page"
generated: {by: claude-opus-5-5/architecture-views-0.7.0, at: 2026-10-29T14:00:00+01:00}
---

# Context

The moves were measured from Today, the plan the user stars as their current setup, so without a Today Optimisation had nothing to measure from. The search runs the split at every move, the year a thousand times or more, on the browser's one thread.

# Decision

The moves are measured from a baseline (`River/MovesBaseline`): Today when a plan is starred, else the plan in the river as it stood when the page opened, when a plan was opened from Plans, or when the river was saved. `River/MovesSource` searches once per baseline and date, never on an edit of the river; a note says when the river has changed since. From 24.10.2026 the search waits until the browser has painted the page (`plan-paint.js`, `nettoPaint.after`), the Optimisation badge reading « … » meanwhile ([work item](https://app.notion.com/p/402b6b39bd1281bda930db0a7e146a17)).

# Alternatives rejected

None recorded at the time.

# Consequences

- `Plan.OnInputsChanged` never touches the moves; only opening and saving a plan move the baseline. Saving re-baselines, and stars the plan when none is starred.
- A new day or a new star triggers one new search on the next render.
- No search runs when the plan starts at the pay.
- The search itself is [greedy, each gain the difference of two runs](/2026-10-22-moves-found-greedily-from-a-baseline.md); the flow is drawn on the [Optimisation moves page](../optimisation-moves.html).
