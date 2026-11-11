---
type: adr
title: The moves retune only the levers in the plan, measured from the plan on screen
description: Each finder names its lever (LeverKind); MoveSearch runs only the finders of levers in the plan; MovesSource measures from the river on screen, searching after paint and a quiet period; Today feeds only the band.
status: stable
accepted_by: human:Johnny Amadeus
decided: 2026-11-11
tags: [search, site]
sources:
  - resource: https://app.notion.com/p/411b6b39bd1281389895d66c562a99e9
  - resource: https://claude.ai/artifact/N6CmKXboSbPuGucQSmcdQm
generated: {by: claude-fable-5-2/architecture-views, at: 2026-11-11T14:00:00+01:00}
---

# Context

The moves were measured from a baseline, Today or the plan as opened or saved, and searched once per baseline, never on an edit ([moves searched once per baseline](/2026-10-23-moves-searched-once-per-baseline-after-paint.md)). On 2026-11-06, with a 25 % royalty unsaved, the tab said « this plan: 61.975,74 € » while the band said 62.624,32 €. The search also proposed voucher moves while vouchers were out of the plan: a lever that is out is its `None` in the scenario, which Core cannot tell from a lever in the plan at zero. The story « I see my plan, what Netto can improve with what I gave, and what I could add, each in its own view » ([story](https://app.notion.com/p/411b6b39bd1281389895d66c562a99e9)) asks that Optimise only retunes, from the plan on screen. Once accepted, this decision replaces [moves searched once per baseline](/2026-10-23-moves-searched-once-per-baseline-after-paint.md) and amends two consequences: Apply no longer adds a module ([optional levers](/2026-10-21-optional-levers-neither-shown-nor-counted.md)), and the search runs a subset of the finders ([moves found greedily](/2026-10-22-moves-found-greedily-from-a-baseline.md)).

# Decision

- **Each finder names its lever**: `IMoveFinder.Lever`, a `LeverKind` (Core), null for what every plan holds (the pay, both advance payments). A finder whose lever is in the plan retunes; one whose lever is out would add it.
- **The search is told which levers are in the plan**: `MoveSearch.Run(scenario, date, inPlan)` runs only the retuning finders. The client only names the levers in (`RiverInputs.LeversInPlan`, from `PlanComposition`); it selects finders and decides no tax outcome.
- **The moves are measured from the river on screen**, the scenario the band runs. Today, the starred plan, feeds only the band's gain (`BaselineGain`), the Plans rows and the comparison.
- **When it searches**: after the browser has painted, at once on opening the page and on the Optimise view, and on the other views once edits have paused (a quiet period); one search per plan and date. While the plan on screen is not the plan searched, the view is searching: the badge reads « … » and no move is listed, so a stale move is never applied.
- **Apply one or all**: `MovesView.Apply` and `ApplyAll`, the latter landing on the plan's optimised pocket; applying searches again at once. Nothing is applied unasked.

# Alternatives rejected

- Core guessing « in the plan » from the scenario's figures: a lever in the plan at zero is its `None`.
- Searching on every edit: the search runs the split at every round, about a second in the browser, on its one thread.
- Searching only while Optimise is shown: My plan's badge and nudge would say « … » after any edit.
- Keeping Today as the Optimiser's baseline: two « this plan » figures on one screen.

# Consequences

- `MovesBaseline`, the « river changed since » note and `RiverDifferences` go: there is no other plan to compare with.
- The finders of a lever that is out are what an idea builds on ([an idea's gain](/2026-11-11-an-ideas-gain-is-optimised-against-optimised.md)).
- A new kind of move is a `Move`, its finder with its `LeverKind`, its words, its reason and its link.
