---
type: adr
title: The plan page has three views, each answering one question, and time is a lens of the first
description: My plan, Optimise and Ideas (Mon plan, Optimiser, Pistes) are the PlanTab values; When to pay is the « by date » lens of My plan at /plan?by=date; Compare plans is a mode of the Plans panel; the old addresses replace themselves.
status: stable
accepted_by: human:Johnny Amadeus
decided: 2026-11-11
tags: [site]
sources:
  - resource: https://app.notion.com/p/411b6b39bd1281389895d66c562a99e9
  - resource: https://claude.ai/artifact/N6CmKXboSbPuGucQSmcdQm
generated: {by: claude-fable-5-2/architecture-views, at: 2026-11-11T14:00:00+01:00}
---

# Context

The plan page had four tabs by purpose ([tabs by purpose](/2026-10-23-tabs-by-purpose.md)): Plan, Optimisation, When to pay, Compare plans. On 2026-11-06 the Optimisation tab ranked in one list a certain gain of 2.233,03 € beside 45 € resting on a condition Netto cannot see. The operator's position « Three views, each answering one question » and the story « I see my plan, what Netto can improve with what I gave, and what I could add, each in its own view » ([story](https://app.notion.com/p/411b6b39bd1281389895d66c562a99e9)) redraw the page. Once accepted, this decision replaces [tabs by purpose](/2026-10-23-tabs-by-purpose.md).

# Decision

- **Three views, no more**, in this order: My plan (« Mon plan », where do I stand), Optimise (« Optimiser », how to do better with what I have) and Ideas (« Pistes », what could I add). They are the `PlanTab` values `Plan`, `Optimisation` and `Pistes`, at `/plan`, `/plan/optimisation` (kept, so no bookmark moves) and `/plan/pistes`.
- **The line between the second and the third is certain against conditional**: Optimise uses only facts the viewer gave; an idea rests on something Netto cannot see and asks it first.
- **My plan suggests nothing inside the plan.** The other two views appear in it only as counts: their tab badges and one line of nudges (`PlanNudges`).
- **Time is a lens of My plan** ([time is a lens](/2026-10-19-time-is-a-lens.md)): « by bucket · by date » (`PlanLens`), the lens a query, `/plan?by=date`, drawn by two links so Back, a bookmark and a move's link work. « By date » shows what the When to pay tab showed. The next deadline stays in the band, as the badge of the My plan tab.
- **Comparing two saved plans is a mode of the Plans panel**, not a view of this plan.
- **Old addresses replace themselves**: `/plan/when-to-pay` with `/plan?by=date`, `/plan/compare` with `/plan?plans=compare`, which opens the Plans panel on its compare mode.
- A future multi-year plan is a year selector, not a fourth view.

# Alternatives rejected

- Four tabs kept, the moves split inside Optimisation: the certain and the conditional would still be ranked together.
- The lens as a segment (`/plan/by-date`): a second tab-like route for what is a state of My plan.
- The lens as page state only: no bookmark, and the advance-payment moves could not link to it.
- The next deadline as a text line in the band: a new row whose presence depends on the plan, against the bar's measured height.

# Consequences

- A new view is still a `PlanTab` value and segment, its texts, a component in `Tabs/` opening on `TabHead`, its case in `Plan.razor` and its badge; My plan's head is drawn by the page, above the lens, so a lens switch keeps the focus where it is.
- Whatever tested « the Plan tab is shown » to draw the map tests « My plan, by bucket ».
- My plan keeps the map and the chosen card beside it ([the plan is the map](/2026-10-27-the-plan-is-the-map.md)); its facts pane, read-only picture and explanation drawer are a story of their own.
