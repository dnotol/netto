---
type: adr
title: Optimise is three sub-tabs under one head line, each with its address
description: The moves, the pay curve and When to pay are sub-tabs of the Optimise view at /plan/optimisation, ?show=pay and ?show=when, links in a nav, only the chosen one drawn, under a head line common to the three; a move is a compact row with its why behind a toggle.
status: stable
accepted_by: human:Johnny Amadeus
decided: 2026-11-13
tags: [site]
sources:
  - resource: https://app.notion.com/p/417b6b39bd12815e8d5fd1ed759106a7
generated: {by: claude-fable-5-2/architecture-views, at: 2026-11-13T18:00:00+01:00}
---

# Context

[When to pay is a section of Optimise](/2026-11-11-when-to-pay-is-a-section-of-optimise.md) stacked three things on one page: the moves, the pay curve and the whole payment calendar, each with a note setting it against the others. Reading it in the app, the operator found the view a mess and asked for sub-tabs, there being more than one way to optimise ([story](https://app.notion.com/p/417b6b39bd12815e8d5fd1ed759106a7)). This amends how that decision lays the view out; the calendar stays in Optimise, at the address it has, and the deadline stays the tab's badge.

# Decision

- **Three sub-tabs**, in this order: « Ajustements » (the moves), « Rémunération » (the pay curve, `PayCurve`) and « Quand payer » (`WhenToPaySection`). Only the chosen one is drawn.
- **Each has an address through the `show` query**: `/plan/optimisation` the moves, `?show=pay` the curve, `?show=when` the calendar, kept, so the deadline badge, a payment move's link and the old addresses still land on it. An unknown value shows the moves. The address holds the choice and the page reads it (`OptimiseSections`): no state of its own, and Back and Forward walk the sub-tabs while the page stays alive.
- **The sub-tabs are links in a `nav`, the chosen one `aria-current="page"`**, as the band's views are ([tabs by purpose](/2026-10-23-tabs-by-purpose.md) chose the same for them): each is an address and a history entry, which an ARIA tab is not.
- **One head line is common to the three**: what the plan leaves in the pocket, what it leaves with every move, the gain and « Apply all », then the state line and, in one muted line in sight, what the gain is measured by. A bubble never holds it.
- **A move is a compact row**: what it changes, its gain, « Apply ». Why it helps and its link to the card are behind a toggle on the row, « Pourquoi »; the pocket after each move is no longer said, the head saying the total.
- **Focus**: a click on a sub-tab leaves the focus on it; any other change of sub-tab (Back, the badge, a move's link, an arrival by address) moves it to the start of the panel. Nothing is announced.

# Alternatives rejected

- « Quand payer » as a view of its own in the band: the operator kept it with the moves it belongs with.
- An ARIA tablist with arrow keys: a tab with an address mixes selecting with navigating, and arrows that navigate push a history entry per key.
- The sub-tab kept as the tab's own state: no bookmark, no Back, and the badge and the old addresses could not name the calendar.
- The why of a move always in sight: it is what made the list long.

# Consequences

- The pay curve and the calendar are no longer under the moves, so each says once that it is the plan on screen's own, without setting itself against « the moves above ».
- « Apply all » can be pressed from any sub-tab; the state line it moves the focus to is in the head.
- `SectionArrival` becomes an arrival for any sub-tab; `MovesView.InPocketAfter` goes.
