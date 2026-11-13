---
type: adr
title: An idea is a lever not in the plan, its gain the optimised plan with it less the optimised plan without it
description: PisteSearch runs MoveSearch once with the lever and sets it against the Optimiser's plan; ideas are the levers whose cash gain the engine measures (vouchers, rent, royalty); each asks first what Netto cannot see; a trial lives in the Ideas view until kept.
status: deprecated
superseded_by: /2026-11-13-two-views-and-levers-are-found-in-edit-plan.md
accepted_by: human:Johnny Amadeus
decided: 2026-11-11
tags: [search, site]
sources:
  - resource: https://app.notion.com/p/411b6b39bd1281389895d66c562a99e9
  - resource: https://claude.ai/artifact/N6CmKXboSbPuGucQSmcdQm
generated: {by: claude-fable-5-2/architecture-views, at: 2026-11-11T14:00:00+01:00}
---

# Context

A move that switches on a lever rests on conditions Netto cannot see ([the user declares what Netto cannot see](/2026-10-19-the-user-declares-what-netto-cannot-see.md)), so it leaves Optimise ([moves retune the plan on screen](/2026-11-11-moves-retune-the-plan-on-screen.md)). The story « I see my plan, what Netto can improve with what I gave, and what I could add, each in its own view » ([story](https://app.notion.com/p/411b6b39bd1281389895d66c562a99e9)) gives such levers a view of their own, Ideas (« Pistes »).

# Decision

- **An idea's gain is plan against plan**: the pocket of the optimised plan with the lever less the pocket of the optimised plan without it. `Core/Simulation/PisteSearch` runs `MoveSearch` with the retuning finders plus that lever's own; the « without » side is the Optimiser's own search, shared. Every figure is the difference of two runs of the year; the search does no arithmetic of its own. The gain is not the lever's move line: the pay retunes around a rent.
- **Which levers are ideas**: those whose gain in cash the engine measures from the viewer's facts: vouchers (eco, sport/culture), the home-office rent, the copyright royalty. A company car always takes cash from the pocket ([gains are in cash](/2026-10-23-gains-are-in-cash.md)) and the household is a fact, not a lever ([household taxed jointly](/2026-10-28-household-taxed-jointly.md)): both stay in « Add to plan ». No allowance is an idea ([the reading that never overstates the gain](/2026-10-22-the-reading-that-never-overstates-the-gain.md)).
- **Questions before gains**: an idea first asks the facts its card already holds (a status, or an amount with no default); Netto never decides eligibility. Vouchers, whose only question is a yes, show « if yes » beside their tuned gain. An untuned gain is never shown.
- **A trial lives in the Ideas view**: « Try » shows the optimised plan with the idea and its moves; the page's river does not change. « Keep » adds the lever with its facts to the river, an edit like any other, and points to Optimise for the retunes; « Drop » discards it. Ideas never retune, as Optimise never adds.
- **When it searches**: after the Optimiser's search, one idea per paint, once per plan, date and answers; an idea whose answers are being typed is searched on « Try ».
- The answers are the levers' existing inputs: no new stored input. What Netto does not model (pension) is one muted line, « not in Netto yet », with no promise.

# Alternatives rejected

- Showing each lever's move gain: it ranks an untuned figure, and overstates or understates what the plan gains.
- A page-wide trial plan under a banner: the band would show a plan that is not the viewer's, and every view's unsaved-changes logic would learn of it.
- « Keep » applying the retunes too: Ideas would change amounts of levers the viewer already had.
- The car as a priced idea: its worth needs the private cost the engine never sees; weighing it in the client would be a tax rule outside Core.

# Consequences

- The count on My plan is the ideas with a gain and the questions still open.
- One search more per idea, about a search's cost each, spread over paints.
- A new idea is a lever with a finder naming its `LeverKind`, its questions and its words.
