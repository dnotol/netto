---
type: adr
title: The map's lines are drawn as a river, on arcs, the pocket line one stepped shape at full ink
description: Routes stay orthogonal points; the drawing turns every corner on an arc (MapPath), a line into the pocket line curves down into it with no halo where they meet, and the pocket line is one stepped shape, never faded per stretch; lines into one bucket arrive spread across its top.
status: draft
decision: proposed
decided: 2026-11-06
tags: [site]
sources:
  - resource: https://app.notion.com/p/410b6b39bd1281db98f6fc1fcbba5857
generated: {by: claude-opus-5-5/architecture-views, at: 2026-11-06T12:00:00+01:00}
---

# Context

On the map drawn by owner ([decision](/2026-10-29-the-map-drawn-by-owner.md)) the operator found that where lines meet, the map reads as crossings and circuit traces rather than money merging ([work item](https://app.notion.com/p/410b6b39bd1281db98f6fc1fcbba5857)). Three causes in the drawing: a line ending on the pocket line keeps the halo drawn for crossings, which cuts a gap in the gold line; the pocket line is one link per stretch, each faded on its own when a module is chosen, so it changes ink halfway down; every route is a polyline with square corners. Lines into one bucket also bunch at its left corner.

# Decision

- **Routes stay what they are.** `MapLayout` keeps orthogonal points, ports and crossings, so `MapLayoutTests` and `MapCrossings.txt` hold. The rounding is drawing only: `Tabs/MapPath` turns a route's drawn points into SVG path data, each corner on an arc of 8 px at the narrowest map (the scale the layout is worked out at; the SVG stretches, so wider maps draw it larger), never more than half the shorter adjacent side, nor under an arrowhead. A line's halo follows the same path, so crossings still read as over and under.
- **A join curves into the flow.** A line into the pocket line ends on a quarter arc turning down along it; its halo stops where the arc begins, so nothing cuts the gold. A line leaving the pocket line curves out of it the same way.
- **The pocket line is one shape.** Its stretches are drawn as one group under every other line, each step centred on the line with its own stretch's width (`MapLineWidths.Money` of the running total), starting where its join's arc ends, square-ended. It is never faded per stretch: full ink when a module or the pocket is chosen, faded whole when another bucket is.
- **A running total sits under its join**, right of the pocket line, clear of the arc.
- **Lines into one bucket arrive at ports spread across its top edge**, in their nested order, adding no crossing; the Expenses bucket's two lines keep their lane (operator, 2026-11-06).

# Alternatives rejected

- Rounded points in `MapLayout`: every route and crossing test would move for a drawing change.
- An exact pixel radius at every width: needs a script measuring the map and redrawing on resize, for a few pixels.
- Spreading Expenses' lines: only by a new route along the band, with real crossing risk.

# Consequences

- `<polyline>` becomes `<path>` in `MapTab.razor`; tests reading the drawn lines read paths.
- The pocket line leaves `MapView.Drawn` for a drawing of its own; its links, totals and `Touches` are unchanged, so `MapCardsTests` holds.
- The drawing step is shown on the [map lines page](../map-lines.html).
