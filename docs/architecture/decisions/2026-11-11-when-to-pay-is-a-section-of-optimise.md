---
type: adr
title: When to pay is a section of Optimise, not a lens of My plan
description: The whole payment calendar is a section of the Optimise view at /plan/optimisation?show=when, under the moves and the pay curve; My plan has no lens; the next deadline is a badge on the Optimise tab; the old addresses lead to the section.
status: stable
accepted_by: human:Johnny Amadeus
decided: 2026-11-11
tags: [site]
sources:
  - resource: https://app.notion.com/p/415b6b39bd1281c98825e3523e9d3add
generated: {by: claude-fable-5-2/architecture-views, at: 2026-11-11T19:00:00+01:00}
---

# Context

[Three views, one question each](/2026-11-11-three-views-one-question-each.md) made the payment calendar the « by date » lens of My plan, at `/plan?by=date`, with the next deadline as a badge on the My plan tab. Seeing it in the app the same day, the operator asked for My plan to be the plan alone and for the calendar to stand with the moves it belongs with ([story](https://app.notion.com/p/415b6b39bd1281c98825e3523e9d3add)). This amends where that decision puts time; its three views, their order and their questions stand, and time stays a lens over the whole year, not a module ([time is a lens](/2026-10-19-time-is-a-lens.md)).

# Decision

- **My plan has no lens**: it is the picture of the plan and the chosen card, nothing else. `PlanLens` and its bar go.
- **The whole calendar is a section of Optimise**, « When to pay » (« Quand payer »), under the moves and the pay curve: the timeline, the advance payments, the monthly pay, the quarterly contributions with what the fund bills, and the balances on the assessments. It applies nothing; its one field is an edit like any other.
- **Its address is a query**, `/plan/optimisation?show=when`, read once per arrival as `plan?card=` is; arriving moves the focus to the section's heading. A move about a payment links to it.
- **The next deadline stays in the band**, as a badge on the Optimise tab beside its gain, a link of its own to the section. A refused « what the fund bills » shows on the Optimise tab, where its field is.
- **The old addresses lead to the section**: `/plan/when-to-pay` and `/plan?by=date` replace themselves with it.

# Alternatives rejected

- Only the top-ups in Optimise, the rest of the calendar dropped: the monthly pay, the quarters and the balances would have no place on the site.
- A fragment (`#when`): Blazor treats it as a navigation and does not scroll to it reliably while the page stays alive between the plan's routes.
- The deadline badge kept on My plan, jumping to another view: a tab's badge speaks of its own view.

# Consequences

- Optimise shows payments that are facts beside moves that are gains; the section says what falls when and suggests nothing beyond the moves above it.
- The page builds the calendar once per draw, for the badge and the section.
- `WhenToPayTab` is `WhenToPaySection`, a part of `OptimisationTab`.
