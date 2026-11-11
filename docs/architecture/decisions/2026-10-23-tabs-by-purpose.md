---
type: adr
title: The plan page splits into tabs by purpose, each with its own address
description: Plan, Optimisation, When to pay and Compare plans are PlanTab values under /plan/{Tab}, one page keeping the plan across them.
status: deprecated
superseded_by: /2026-11-11-three-views-one-question-each.md
decided: 2026-10-23
tags: [site]
sources:
  - resource: https://app.notion.com/p/402b6b39bd1281aba11ce39997cc260e
  - resource: https://claude.ai/artifact/C2gkexviNGUUMzDbpaqaEX
  - resource: "git:8d390d1 Name the plan's tabs and the URL of each"
generated: {by: claude-opus-5-5/architecture-views-0.7.0, at: 2026-10-29T14:00:00+01:00}
---

# Context

The river had grown a moves card at its top and a calendar card at its end. « I find my plan, my moves and when to pay each under its own tab » ([story](https://app.notion.com/p/402b6b39bd1281aba11ce39997cc260e)) followed the prototype « Netto River or Tabs ».

# Decision

The plan page splits into tabs by purpose: Plan, the river; Optimisation, the moves; When to pay, the calendar; Compare plans, two saved plans side by side. Each tab is a `PlanTab` value with its own address (`PlanTabRoutes`: `/plan`, `/plan/optimisation`, `/plan/when-to-pay`, `/plan/compare`) and a badge (`TabBadges`). One component, `Pages/Plan.razor`, keeps the inputs, the run, the searches and the chosen module across tabs; the bar of buckets stays on every tab.

# Alternatives rejected

- One long river with the moves and the calendar as cards in it, as built until 23.10.2026.

# Consequences

- A new tab is a `PlanTab` value and segment, its texts, a component in `Tabs/` opening on `TabHead`, its case in `Plan.razor` and its badge.
- Between `/plan` and `/plan/{Tab}` the page stays alive, so focus and scroll are the page's own (`TabHead` focuses its h1).
- A Map tab joined on 25.10.2026 and was folded into Plan on 27.10.2026 ([the plan is the map](/2026-10-27-the-plan-is-the-map.md)).
- Decided in « Goal & scope » on 24.10.2026 and applied on 25.10.2026 (`d79a176`): `/` replaces itself with `/plan`, and the front page moves to `/about`.
