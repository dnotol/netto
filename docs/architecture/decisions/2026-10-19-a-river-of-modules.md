---
type: adr
title: Netto's shape is a river of modules, each taking its part and passing the rest on
description: One page at /plan follows the money through module cards to five buckets, the bar of buckets always in view as the summary.
status: stable
decided: 2026-10-19
tags: [product, site]
sources:
  - resource: https://app.notion.com/p/3fdb6b39bd1281bc81bfce270c576635
  - resource: https://claude.ai/artifact/C2gkexviNGUUMzDbpaqaEX
  - resource: git:611702e Hold the river's inputs in one state that runs the year for every card
  - resource: git:107868b Make /plan the river and retire /overview
generated: {by: claude-opus-5-5/architecture-views-0.7.0, at: 2026-10-29T14:00:00+01:00}
---
# Context

The first screens were separate windows: gross to net on `/plan`, the current setup on `/overview`. After a prototype, the operator chose one shape for the whole product on 19 October 2026 (River Map, « The river is Netto's shape »; « Goal & scope », [Notion](https://app.notion.com/p/3fdb6b39bd1281bc81bfce270c576635)).

# Decision

Netto follows the money from what the company earns to what reaches the director, through modules that each take a part and pass the rest on. One component holds the river's inputs (`RiverInputs`) and runs the year once for every card. A bar of buckets stays in view as the summary; there is no flow diagram on top. The river is not one-way: where one module sets a condition on another (the pay decides the corporate rate), the module shows that condition rather than an arrow going back up.

# Alternatives rejected

- Two pages, `/plan` and `/overview`: merged into one page of module cards, `/overview` retired (River Map, 19 October 2026).

# Consequences

- The modules, their order and the buckets became the vocabulary of the code (`RiverModule`, `BucketsView.Keys`).
- The page later split into tabs ([tabs by purpose](/2026-10-23-tabs-by-purpose.md)) and the Plan tab became a map of the river ([the plan is the map](/2026-10-27-the-plan-is-the-map.md)); the conditions became arrows drawn from the engine ([the year's conditions](/2026-10-25-the-engine-names-the-years-conditions.md)).
- The bar grew a fifth bucket ([Expenses](/2026-10-28-expenses-are-the-fifth-bucket.md)).
