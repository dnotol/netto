---
type: adr
title: Moves are found greedily, each gain the difference of two runs of the year
description: MoveSearch takes the finders' best move until none adds a euro; no move is refused or leaves less than the reserve; Apply is the one place a move is applied.
status: stable
decided: 2026-10-22
tags: [search, engine]
sources:
  - resource: https://app.notion.com/p/3fdb6b39bd1281babbb9d47babbc23e6
  - resource: https://claude.ai/artifact/C2gkexviNGUUMzDbpaqaEX
  - resource: git:8616c57 Find the moves from Today greedily, each measured on top of the ones before it
generated: {by: claude-opus-5-5/architecture-views-0.7.0, at: 2026-10-29T14:00:00+01:00}
---
# Context

« I see what each recommended move gains me » ([Notion](https://app.notion.com/p/3fdb6b39bd1281babbb9d47babbc23e6)) asked Netto to say what it would change in the user's current plan and what each change adds. The River Map's rules of 22 October 2026 (« Your moves, from Today », « One move, one button ») set the method.

# Decision

`Netto.Core/Simulation/MoveSearch` finds moves greedily. Each kind of move has a finder (`IMoveFinder`) proposing its best from where the plan stands; the move adding the most is taken and the others are measured again on top of it, until none adds a euro (`SmallestGain`), within `MaximumRounds`. Every gain is the pocket of one `YearSimulation.Run` less another's: the searches do no arithmetic of their own. A move the rules refuse, or one leaving less than the reserve, is never taken. A move of a kind already taken replaces it in its line, adding both gains. « Apply » on the Optimisation tab is the one place a move is applied (`MovesView.Apply`).

# Alternatives rejected

None recorded at the time. The split's own « Move to the best pay » button was removed that day as one action said twice, and came back on 25 October 2026 for the river's own best pay (River Map).

# Consequences

- Together the moves add up to the whole plan's gain, the bar's own figure once all are applied.
- A new kind of move is a `Move`, its finder, its words, its reason, its link and what it sets; their switches throw on a kind no case names (CLAUDE.md).
- The baseline and when the search runs were refined the next day ([moves searched once per baseline](/2026-10-23-moves-searched-once-per-baseline-after-paint.md)).
- `Simulation/MoveSearchTests` holds it.
