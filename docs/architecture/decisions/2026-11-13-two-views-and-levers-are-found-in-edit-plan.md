---
type: adr
title: The plan page has two views, and a lever is found in « Edit plan »
description: My plan and Optimise are the PlanTab values; the Ideas view, its trial and PisteSearch go; a lever not in the plan is found and added in the panel over the map, its card says its gain once added, and Optimise retunes; My plan shows no line of nudges.
status: stable
accepted_by: human:Johnny Amadeus
decided: 2026-11-13
tags: [site, search]
sources:
  - resource: https://app.notion.com/p/417b6b39bd12815e8d5fd1ed759106a7
generated: {by: claude-fable-5-2/architecture-views, at: 2026-11-13T18:00:00+01:00}
---

# Context

[Three views, each answering one question](/2026-11-11-three-views-one-question-each.md) gave the levers not in the plan a view of their own, Ideas (« Pistes »), measured as [an idea's gain is optimised against optimised](/2026-11-11-an-ideas-gain-is-optimised-against-optimised.md) says. Two days later the operator, reading the app, found the view redundant ([story](https://app.notion.com/p/417b6b39bd12815e8d5fd1ed759106a7)): it covers three levers where the panel over the map lists every optional module; since [a lever's card leads with what it adds to the pocket](/2026-11-12-a-levers-card-leads-with-what-it-adds-to-the-pocket.md), adding a lever shows its gain at once, and taking it out keeps its values and offers Undo, so « Try » and « Keep » repeat what adding does; and its line on what Netto does not model advertises what the product lacks. My plan also repeated, in a line of nudges under its title, what the band's badges already say. Once accepted, this decision replaces [an idea's gain is optimised against optimised](/2026-11-11-an-ideas-gain-is-optimised-against-optimised.md) and amends [three views](/2026-11-11-three-views-one-question-each.md), whose first two views, their order and their questions stand.

# Decision

- **Two views**: My plan (« Mon plan », where do I stand) and Optimise (« Optimiser », how to do better with what I have), the `PlanTab` values `Plan` and `Optimisation`. `/plan/pistes` replaces itself with `/plan`, where levers are now added.
- **The Ideas view and its machinery go**: `PistesTab`, `PisteCard`, `PistesView`, `PistesSource`, `PisteTrial` and Core's `PisteSearch`, with the tab's badge. No gain is measured for a lever before it is in the plan.
- **A lever is found in « Edit plan »** (« Modifier le plan », `CompositionMenu`), the panel over the map that adds and removes every optional module; the button is named for both. Added, the lever's card opens beside the map and leads with its gain; Optimise then retunes the plan with it, as [the moves retune the plan on screen](/2026-11-11-moves-retune-the-plan-on-screen.md) says.
- **My plan suggests nothing and repeats nothing**: Optimise appears in it only as its tab's badges. `PlanNudges` goes.
- What Netto does not model is said nowhere as a promise or a teaser; the limits that change how a figure reads stay in « Not counted yet » at the bottom of My plan.

# Alternatives rejected

- The view kept as it was: three levers in a tab of their own beside a panel that lists them all.
- The measured gain shown in the panel beside a lever not in the plan: My plan would suggest something, the search and its one-idea-per-paint schedule would stay for a figure the card gives a click later, and a lever that asks a question first would still show none.

# Consequences

- A lever's gain is no longer plan against plan before it is added: the viewer adds it, reads its card, and visits Optimise for the retunes. `MoveSearch` keeps running only the finders of levers in the plan (`IMoveFinder.Lever`, `MoveSearch.KindOf`); the finders of a lever that is out are no longer built on.
- A new lever needs no idea, question or trial: its `OptionalModule` and its texts in the panel are how it is found.
- One search fewer per lever out of the plan after each edit.
