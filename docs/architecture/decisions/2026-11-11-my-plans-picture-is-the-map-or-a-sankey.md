---
type: adr
title: My plan's picture is the map or a Sankey, the viewer's choice, until one of them wins
description: A switch in the map's tools swaps the drawing for a Sankey of the same plan (SankeyView from MapLinks.Of, money conserved at every node, SankeyLayout one ordering rule); the choice is kept in the browser, the map the default; cards, choice and effects are shared.
status: draft
decision: proposed
decided: 2026-11-11
tags: [site]
sources:
  - resource: https://app.notion.com/p/415b6b39bd1281c98825e3523e9d3add
  - resource: https://claude.ai/artifact/LxwVWoynXrEmVXA8pFskz4
generated: {by: claude-fable-5-2/architecture-views, at: 2026-11-11T19:00:00+01:00}
---

# Context

The plan is drawn as a map by owner ([the plan is the map](/2026-10-27-the-plan-is-the-map.md), [the map drawn by owner](/2026-10-29-the-map-drawn-by-owner.md)). A head-to-head on the plan with every lever ([Map or Sankey](https://claude.ai/artifact/LxwVWoynXrEmVXA8pFskz4)) recommended a Sankey on conditions: one tested ordering rule, small flows readable, flows going back marked, what moves no money kept out of the bands. The operator asked to judge both on real plans before choosing ([story](https://app.notion.com/p/415b6b39bd1281c98825e3523e9d3add)). Which drawing wins is the decision of [the facts pane story](https://app.notion.com/p/415b6b39bd128129b80bf165e5c724de); this one only puts both on screen.

# Decision

- **A switch swaps the drawing, nothing else**: « Map · Sankey », first in the map's tools, two pressed-or-not buttons. The chosen card, what it receives, the listed effects, the page's choice (`MapChoice`) and the band are shared; a node or a bucket of the Sankey is chosen as on the map.
- **The choice is the viewer's**, kept in the browser (`netto.plan.picture`, `Tabs/PicturePreference`), the map when nothing is kept. It is no part of a saved plan.
- **The Sankey reads the map's lines**: `SankeyView` gathers the money links of `MapLinks.Of` into bands between nodes, each band the sum of the links it gathers (`MapBundle.Sum`); it computes no figure of its own and holds no tax rule. The company's line collapses into Company year; a lever's share runs from Company year to the lever; earlier profits are a source into Dividend, read from the Dividend card's view.
- **Money is conserved at every node**, held by a test over every composition of the plan: what enters a node leaves it, and each bucket receives what the band says to the cent.
- **One ordering rule** (`SankeyLayout`): fixed stages from Company year to the five buckets, nodes and bands in the table's order, thickness the amount at one scale. A band under the minimum is drawn at the minimum and said as such in the key, never as if to scale.
- **A flow going back is marked**: the car's contribution is the one dashed loop, from the pocket to Company year; money that came back into a module is netted on its band and said « net ».
- **What moves no money is no band**: tax bases, conditions and the household stay in the list beside the chosen card; modules without a band stay reachable from a row of buttons under the drawing.
- **Where no honest picture exists** (a refusal, a loss), the Sankey says so in one line; the map is one press away.

# Alternatives rejected

- Replacing the map now: the head-to-head was on one made-up plan; the operator wants both on their own figures first.
- Feeding the Sankey from `MapView`: its lines are already rewritten for the map's drawing and nothing in it checks a node's money in against its money out.
- A read-only Sankey: the card beside it would still show, chosen only from the band.
- The switch in My plan's head: a wider surface for something meant to go.
- A charting library: the map is plain SVG in Razor, tested as a layout; a library's layout could not be held by the same tests.

# Consequences

- Two drawings read the same lines until the choice is made; a new module is also its stage in `SankeyLayout`.
- [The map drawn by owner](/2026-10-29-the-map-drawn-by-owner.md) stands. The drawing that loses is removed by the story that decides, with this ADR superseded then.
